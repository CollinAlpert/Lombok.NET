using System.Text;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generates serialization methods for a class or a struct.
/// </summary>
[Generator]
internal sealed class SerializationGenerator : IIncrementalGenerator 
{
	private static readonly string AttributeName = typeof(SerializationAttribute).FullName;

	private static readonly ISet<SpecialType> AllowedTypes = new HashSet<SpecialType>
	{
		SpecialType.System_Int16,
		SpecialType.System_Int32,
		SpecialType.System_Int64,
		SpecialType.System_UInt16,
		SpecialType.System_UInt32,
		SpecialType.System_UInt64,
		SpecialType.System_Byte,
		SpecialType.System_SByte,
		SpecialType.System_Single,
		SpecialType.System_Double,
		SpecialType.System_Decimal,
		SpecialType.System_String,
		SpecialType.System_Char,
		SpecialType.System_Boolean
	};
    
    private static readonly LocalDeclarationStatementSyntax BinaryWriterDeclaration = LocalDeclarationStatement(
            VariableDeclaration(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierName(
                                Token(SyntaxKind.GlobalKeyword)
                            ),
                            IdentifierName("System")
                        ),
                        IdentifierName("IO")
                    ),
                    IdentifierName("BinaryWriter")
                )
            ).WithVariables(
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier("binaryWriter")
                    ).WithInitializer(
                        EqualsValueClause(
                            ObjectCreationExpression(
                                QualifiedName(
                                    QualifiedName(
                                        AliasQualifiedName(
                                            IdentifierName(
                                                Token(SyntaxKind.GlobalKeyword)
                                            ),
                                            IdentifierName("System")
                                        ),
                                        IdentifierName("IO")
                                    ),
                                    IdentifierName("BinaryWriter")
                                )
                            ).WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            IdentifierName("fileStream")
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        ).WithUsingKeyword(
            Token(SyntaxKind.UsingKeyword)
        );

    private static readonly LocalDeclarationStatementSyntax BinaryReaderDeclaration = LocalDeclarationStatement(
        VariableDeclaration(
            QualifiedName(
                QualifiedName(
                    AliasQualifiedName(
                        IdentifierName(
                            Token(SyntaxKind.GlobalKeyword)
                        ),
                        IdentifierName("System")
                    ),
                    IdentifierName("IO")
                ),
                IdentifierName("BinaryReader")
            )
        ).WithVariables(
            SingletonSeparatedList(
                VariableDeclarator(
                    Identifier("binaryReader")
                ).WithInitializer(
                    EqualsValueClause(
                        ObjectCreationExpression(
                            QualifiedName(
                                QualifiedName(
                                    AliasQualifiedName(
                                        IdentifierName(
                                            Token(SyntaxKind.GlobalKeyword)
                                        ),
                                        IdentifierName("System")
                                    ),
                                    IdentifierName("IO")
                                ),
                                IdentifierName("BinaryReader")
                            )
                        ).WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        IdentifierName("fileStream")
                                    )
                                )
                            )
                        )
                    )
                )
            )
        )
    ).WithUsingKeyword(
        Token(SyntaxKind.UsingKeyword)
    );
	
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var sources = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeName, IsCandidate, Transform);
		context.AddSources(sources);
	}
	
	private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node is ClassDeclarationSyntax or StructDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
        var namedType = (INamedTypeSymbol)context.TargetSymbol;
		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}
		
		cancellationToken.ThrowIfCancellationRequested();
		
		var memberTypeArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(SerializationAttribute.MemberType));
		var includeDeserializationArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(SerializationAttribute.IncludeDeserialization));
		var includeDeserialization = includeDeserializationArgument.Value.Value as bool? ?? true;
		var memberType = (MemberType?)(memberTypeArgument.Value.Value as int?) ?? MemberType.Field;

        MemberAccessWithType[] memberAccesses = (memberType switch
        {
            MemberType.Property => GetMembers<IPropertySymbol>(namedType, IsCandidate, p => p.Type),
            MemberType.Field => GetMembers<IFieldSymbol>(namedType, IsCandidate, f => f.Type),
            _ => throw new ArgumentOutOfRangeException(nameof(memberType))
        }).ToArray();
        if (memberAccesses.Length == 0)
        {
            return GeneratorResult.Empty;
        }

        SourceText partialTypeSourceText = CreatePartialType(@namespace, typeDeclaration, memberAccesses, includeDeserialization);

		return new GeneratorResult(typeDeclaration.GetHintName(@namespace), partialTypeSourceText);
	}

    private static SourceText CreatePartialType(
        NameSyntax @namespace,
        TypeDeclarationSyntax typeDeclaration,
        MemberAccessWithType[] memberAccesses,
        bool includeDeserialization)
    {
        SyntaxKind accessibilityModifier = typeDeclaration.GetAccessibilityModifier();
        List<MemberDeclarationSyntax> methods = new(includeDeserialization ? 4 : 2)
        {
            CreateSerializeMethod(memberAccesses, accessibilityModifier),
            CreateSerializeAsyncMethod(memberAccesses, accessibilityModifier)
        };
        if (includeDeserialization)
        {
            methods.Add(CreateDeserializeMethod(memberAccesses, accessibilityModifier));
            methods.Add(CreateDeserializeAsyncMethod(memberAccesses, accessibilityModifier));
        }

        return @namespace.CreateNewNamespace(typeDeclaration.GetUsings(),
                typeDeclaration.CreateNewPartialType()
                    .WithMembers(
                        List(methods)
                    )
            ).NormalizeWhitespace()
            .GetText(Encoding.UTF8);
    }

    private static MemberDeclarationSyntax CreateSerializeMethod(MemberAccessWithType[] memberAccesses, SyntaxKind accessibilityModifier)
    {
        List<StatementSyntax> statements = new(memberAccesses.Length + 2)
        {
            CreateFileStreamDeclaration("OpenWrite", false),
            BinaryWriterDeclaration
        };
        statements.AddRange(Array.ConvertAll(memberAccesses, CreateBinaryWriterWriteStatement));

        return MethodDeclaration(
            PredefinedType(
                Token(SyntaxKind.VoidKeyword)
            ),
            Identifier("Serialize")
        ).WithParameterList(
            ParameterList(
                SingletonSeparatedList(
                    Parameter(
                        Identifier("path")
                    ).WithType(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)
                        )
                    )
                )
            )
        ).WithBody(
            Block(
                statements
            )
        ).WithModifiers(
            TokenList(
                Token(accessibilityModifier)
            )
        );
    }

	private static MemberDeclarationSyntax CreateSerializeAsyncMethod(MemberAccessWithType[] memberAccesses, SyntaxKind accessibilityModifier)
    {
        List<StatementSyntax> statements = new(memberAccesses.Length + 2)
        {
            CreateFileStreamDeclaration("OpenWrite", true),
            BinaryWriterDeclaration.WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword))
        };
        statements.AddRange(Array.ConvertAll(memberAccesses, CreateBinaryWriterWriteStatement));

        return MethodDeclaration(
            QualifiedName(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierName(
                                Token(SyntaxKind.GlobalKeyword)
                            ),
                            IdentifierName("System")
                        ),
                        IdentifierName("Threading")
                    ),
                    IdentifierName("Tasks")
                ),
                IdentifierName("Task")
            ),
            Identifier("SerializeAsync")
        ).WithModifiers(
            TokenList(
                Token(accessibilityModifier),
                Token(SyntaxKind.AsyncKeyword)
            )
        ).WithParameterList(
            ParameterList(
                SeparatedList<ParameterSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        Parameter(
                            Identifier("path")
                        ).WithType(
                            PredefinedType(
                                Token(SyntaxKind.StringKeyword)
                            )
                        ),
                        Token(SyntaxKind.CommaToken),
                        Parameter(
                            Identifier("cancellationToken")
                        ).WithType(
                            QualifiedName(
                                QualifiedName(
                                    AliasQualifiedName(
                                        IdentifierName(
                                            Token(SyntaxKind.GlobalKeyword)
                                        ),
                                        IdentifierName("System")
                                    ),
                                    IdentifierName("Threading")
                                ),
                                IdentifierName("CancellationToken")
                            )
                        ).WithDefault(
                            EqualsValueClause(
                                LiteralExpression(
                                    SyntaxKind.DefaultLiteralExpression,
                                    Token(SyntaxKind.DefaultKeyword)
                                )
                            )
                        )
                    }
                )
            )
        ).WithBody(
            Block(statements)
        );
    }

	private static MemberDeclarationSyntax CreateDeserializeMethod(MemberAccessWithType[] memberAccesses, SyntaxKind accessibilityModifier)
    {
        List<StatementSyntax> statements = new(memberAccesses.Length + 2)
        {
            CreateFileStreamDeclaration("OpenRead", false),
            BinaryReaderDeclaration
        };
        statements.AddRange(Array.ConvertAll(memberAccesses, CreateBinaryReaderReadStatement));

        return MethodDeclaration(
            PredefinedType(
                Token(SyntaxKind.VoidKeyword)
            ),
            Identifier("Deserialize")
        ).WithParameterList(
            ParameterList(
                SingletonSeparatedList(
                    Parameter(
                        Identifier("path")
                    ).WithType(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)
                        )
                    )
                )
            )
        ).WithBody(
            Block(statements)
        ).WithModifiers(
            TokenList(
                Token(accessibilityModifier)
            )
        );
    }

	private static MemberDeclarationSyntax CreateDeserializeAsyncMethod(MemberAccessWithType[] memberAccesses, SyntaxKind accessibilityModifier)
    {
        List<StatementSyntax> statements = new(memberAccesses.Length + 2)
        {
            CreateFileStreamDeclaration("OpenRead", true),
            BinaryReaderDeclaration
        };
        statements.AddRange(Array.ConvertAll(memberAccesses, CreateBinaryReaderReadStatement));

        return MethodDeclaration(
            QualifiedName(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierName(
                                Token(SyntaxKind.GlobalKeyword)
                            ),
                            IdentifierName("System")
                        ),
                        IdentifierName("Threading")
                    ),
                    IdentifierName("Tasks")
                ),
                IdentifierName("Task")
            ),
            Identifier("DeserializeAsync")
        ).WithModifiers(
            TokenList(
                Token(accessibilityModifier),
                Token(SyntaxKind.AsyncKeyword)
            )
        ).WithParameterList(
            ParameterList(
                SingletonSeparatedList(
                    Parameter(
                        Identifier("path")
                    ).WithType(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)
                        )
                    )
                )
            )
        ).WithBody(
            Block(statements)
        );
    }

    private static ExpressionStatementSyntax CreateBinaryWriterWriteStatement(MemberAccessWithType memberAccess)
    {
        return ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("binaryWriter"),
                    IdentifierName("Write")
                )
            ).WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(memberAccess.MemberAccess)
                    )
                )
            )
        );
    }

    private static ExpressionStatementSyntax CreateBinaryReaderReadStatement(MemberAccessWithType memberAccess)
    {
        return ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                memberAccess.MemberAccess,
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("binaryReader"),
                        IdentifierName($"Read{memberAccess.TypeName}")
                    )
                )
            )
        );
    }

    private static LocalDeclarationStatementSyntax CreateFileStreamDeclaration(string method, bool awaitUsing)
    {
        var declaration = LocalDeclarationStatement(
            VariableDeclaration(
                QualifiedName(
                    QualifiedName(
                        AliasQualifiedName(
                            IdentifierName(
                                Token(SyntaxKind.GlobalKeyword)
                            ),
                            IdentifierName("System")
                        ),
                        IdentifierName("IO")
                    ),
                    IdentifierName("FileStream")
                )
            ).WithVariables(
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier("fileStream")
                    ).WithInitializer(
                        EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            AliasQualifiedName(
                                                IdentifierName(
                                                    Token(SyntaxKind.GlobalKeyword)
                                                ),
                                                IdentifierName("System")
                                            ),
                                            IdentifierName("IO")
                                        ),
                                        IdentifierName("File")
                                    ),
                                    IdentifierName(method)
                                )
                            ).WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            IdentifierName("path")
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        ).WithUsingKeyword(
            Token(SyntaxKind.UsingKeyword)
        );

        if (awaitUsing)
        {
            return declaration.WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword));
        }

        return declaration;
    }

    private static IEnumerable<MemberAccessWithType> GetMembers<TSymbol>(INamedTypeSymbol namedType, Func<TSymbol, bool> predicate, Func<TSymbol, ITypeSymbol> typeTransformer)
        where TSymbol : ISymbol
    {
        bool isMemberInBaseClass = false;
        INamedTypeSymbol? type = namedType;
        while (type is not null)
        {
            foreach (var member in type.GetMembers().OfType<TSymbol>().Where(predicate))
            {
                var memberAccess = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    isMemberInBaseClass ? BaseExpression() : ThisExpression(),
                    IdentifierName(member.Name));
                yield return new MemberAccessWithType(memberAccess, typeTransformer(member));
            }

            type = type.BaseType;
            isMemberInBaseClass = true;
        }
    }
    
    private bool IsCandidate(IFieldSymbol field) =>
        !field.IsReadOnly
        && !field.IsStatic
        && field.AssociatedSymbol is null
        && AllowedTypes.Contains(field.Type.SpecialType);

    private bool IsCandidate(IPropertySymbol property) =>
        property.SetMethod is not null
        && !property.IsStatic
        && AllowedTypes.Contains(property.Type.SpecialType);

    private sealed class MemberAccessWithType(MemberAccessExpressionSyntax memberAccess, ITypeSymbol type)
    {
        public MemberAccessExpressionSyntax MemberAccess { get; } = memberAccess;
        
        public string TypeName { get; } = type.Name;
    }
}
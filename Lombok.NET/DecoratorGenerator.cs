using System.Text;
using Lombok.NET.Analyzers;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET;

/// <summary>
/// Generator which generates the decorator subclasses for abstract classes or interfaces.
/// </summary>
[Generator]
internal sealed class DecoratorGenerator : IIncrementalGenerator
{
	private const string AttributeName = "Lombok.NET.DecoratorAttribute";

	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var sources = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeName, IsCandidate, Transform);
		context.AddSources(sources);
	}

	private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node is ClassDeclarationSyntax or InterfaceDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
		INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
		var @namespace = typeDeclaration.GetNamespace();
		if (@namespace is null)
		{
			return new GeneratorResult(Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, typeDeclaration.Identifier.GetLocation()));
		}

		cancellationToken.ThrowIfCancellationRequested();

		return CreateSubclass(@namespace, typeDeclaration, typeSymbol);
	}

	private static GeneratorResult CreateSubclass(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol typeSymbol)
	{
		if (!typeSymbol.IsAbstract)
		{
			return GeneratorResult.Empty;
		}

		var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>().Where(s => s.IsAbstract);

		var decoratorSourceText = CreateDecoratorCode(@namespace, typeDeclaration, methods);

		return new GeneratorResult(typeDeclaration.GetHintName(@namespace), decoratorSourceText);
	}

	private static SourceText CreateDecoratorCode(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<IMethodSymbol> methodSymbols)
	{
		bool isClass = typeDeclaration is ClassDeclarationSyntax;
		string typeName = typeDeclaration.Identifier.Text;
		typeName = !isClass && typeDeclaration.Identifier.Text.StartsWith("I")
			? typeName.Substring(1)
			: typeName;

		var variableName = char.ToLower(typeName[0]) + typeName.Substring(1);

		var memberVariableName = "_" + variableName;
		IEnumerable<MethodDeclarationSyntax> methods = methodSymbols
			.Select(s => s.DeclaringSyntaxReferences[0].GetSyntax())
			.OfType<MethodDeclarationSyntax>()
			.Select(m =>
			{
				// Indicates if the type is a class or not.
				int indexOfAbstractKeyword = m.Modifiers.IndexOf(SyntaxKind.AbstractKeyword);
				if (indexOfAbstractKeyword == -1)
				{
					m = m.WithModifiers(m.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword)).Insert(1, Token(SyntaxKind.VirtualKeyword)));
				}
				else
				{
					m = m.WithModifiers(m.Modifiers.Replace(m.Modifiers[indexOfAbstractKeyword], Token(SyntaxKind.OverrideKeyword)));
				}
				
				m = m.WithSemicolonToken(Token(SyntaxKind.None));
				var methodInvocation = InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(memberVariableName), IdentifierName(m.Identifier)),
					ArgumentList(
						SeparatedList(
							m.ParameterList.Parameters.Select(p => Argument(IdentifierName(p.Identifier)))
						)
					)
				);
				if (m.ReturnType.IsVoid())
				{
					return m.WithBody(
						Block(
							SingletonList<StatementSyntax>(
								ExpressionStatement(methodInvocation)
							)
						)
					);
				}

				return m.WithBody(
					Block(
						SingletonList<StatementSyntax>(
							ReturnStatement(methodInvocation)
						)
					)
				);
			});

		var nullabilityTrivia = SyntaxTriviaList.Empty;
		if (typeDeclaration.ShouldEmitNrtTrivia())
		{
			nullabilityTrivia = Extensions.SyntaxNodeExtensions.NullableTrivia;
		}

		return @namespace.CreateNewNamespace(typeDeclaration.GetUsings(),
				ClassDeclaration($"{typeName}Decorator")
					.WithModifiers(
						TokenList(
							Token(SyntaxKind.PublicKeyword).WithLeadingTrivia(nullabilityTrivia)
						)
					).WithBaseList(
						BaseList(
							SingletonSeparatedList<BaseTypeSyntax>(
								SimpleBaseType(
									IdentifierName(typeDeclaration.Identifier.Text)
								)
							)
						)
					).WithMembers(
						List(
							new MemberDeclarationSyntax[]
							{
								FieldDeclaration(
									VariableDeclaration(IdentifierName(typeDeclaration.Identifier))
										.WithVariables(
											SingletonSeparatedList(
												VariableDeclarator(
													Identifier(memberVariableName)
												)
											)
										)
								).WithModifiers(
									TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
								),
								ConstructorDeclaration(
										Identifier($"{typeName}Decorator"))
									.WithModifiers(
										TokenList(
											typeDeclaration.Modifiers.Where(IsAccessModifier).Cast<SyntaxToken?>().FirstOrDefault() ??
											Token(SyntaxKind.InternalKeyword)
										)
									).WithParameterList(
										ParameterList(
											SingletonSeparatedList(
												Parameter(
														Identifier(variableName))
													.WithType(
														IdentifierName(typeDeclaration.Identifier)
													)
											)
										)
									).WithBody(
										Block(
											SingletonList<StatementSyntax>(
												ExpressionStatement(
													AssignmentExpression(
														SyntaxKind.SimpleAssignmentExpression,
														IdentifierName(memberVariableName),
														IdentifierName(variableName)
													)
												)
											)
										)
									)
							}.Concat(methods)
						)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}

	private static bool IsAccessModifier(SyntaxToken token)
	{
		return token.IsKind(SyntaxKind.PublicKeyword)
		       || token.IsKind(SyntaxKind.ProtectedKeyword)
		       || token.IsKind(SyntaxKind.PrivateKeyword)
		       || token.IsKind(SyntaxKind.InternalKeyword);
	}
}
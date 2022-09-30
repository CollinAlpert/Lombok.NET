using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Analyzers;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET;

/// <summary>
/// Generator which generates the decorator subclasses for abstract classes or interfaces.
/// </summary>
[Generator]
public class DecoratorGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG
        SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
		context.AddSources(sources);
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		TypeDeclarationSyntax? typeDeclaration = node as InterfaceDeclarationSyntax;
		typeDeclaration ??= node as ClassDeclarationSyntax;
		if (typeDeclaration is null)
		{
			return false;
		}

		return typeDeclaration.AttributeLists
			.SelectMany(l => l.Attributes)
			.Any(a => a.IsNamed("Decorator"));
	}

	private static GeneratorResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		SymbolCache.DecoratorAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<DecoratorAttribute>();

		var typeDeclaration = (TypeDeclarationSyntax)context.Node;
		var @namespace = typeDeclaration.GetNamespace();
		if (!typeDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.DecoratorAttributeSymbol))
		{
			return GeneratorResult.Empty;
		}

		if (@namespace is null)
		{
			return new GeneratorResult(Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, typeDeclaration.Identifier.GetLocation()));
		}

		cancellationToken.ThrowIfCancellationRequested();

		return typeDeclaration switch
		{
			ClassDeclarationSyntax classDeclaration => CreateSubclass(@namespace, classDeclaration),
			InterfaceDeclarationSyntax interfaceDeclaration => CreateSubclass(@namespace, interfaceDeclaration),
			_ => GeneratorResult.Empty
		};
	}

	private static GeneratorResult CreateSubclass(string @namespace, ClassDeclarationSyntax classDeclaration)
	{
		var methods = classDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
			.Select(m => m.WithModifiers(m.Modifiers.Replace(m.Modifiers[m.Modifiers.IndexOf(SyntaxKind.AbstractKeyword)],
				Token(SyntaxKind.OverrideKeyword))));

		var decoratorSourceText = CreateDecoratorCode(@namespace, classDeclaration, methods);

		return new GeneratorResult(classDeclaration.Identifier.Text, decoratorSourceText);
	}

	private static GeneratorResult CreateSubclass(string @namespace, InterfaceDeclarationSyntax interfaceDeclaration)
	{
		var methods = interfaceDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.Where(m => m.Body is null)
			.Select(m => m.WithModifiers(m.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword)).Insert(1, Token(SyntaxKind.VirtualKeyword))));

		var decoratorSourceText = CreateDecoratorCode(@namespace, interfaceDeclaration, methods);

		return new GeneratorResult(interfaceDeclaration.Identifier.Text, decoratorSourceText);
	}

	private static SourceText CreateDecoratorCode(string @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<MethodDeclarationSyntax> methods)
	{
		var typeName = typeDeclaration switch
		{
			InterfaceDeclarationSyntax _ when typeDeclaration.Identifier.Text.StartsWith("I") => typeDeclaration.Identifier.Text.Substring(1),
			_ => typeDeclaration.Identifier.Text
		};

		var variableName = char.ToLower(typeName[0]) + typeName.Substring(1);

		var memberVariableName = "_" + variableName;
		methods = methods.Select(m =>
		{
			m = m.WithSemicolonToken(Token(SyntaxKind.None));
			if (m.ReturnType.IsVoid())
			{
				return m.WithBody(Block(
						SingletonList<StatementSyntax>(
							ExpressionStatement(
								InvocationExpression(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(memberVariableName), IdentifierName(m.Identifier))
								)
							)
						)
					)
				);
			}

			return m.WithBody(Block(
					SingletonList<StatementSyntax>(
						ReturnStatement(
							InvocationExpression(
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(memberVariableName), IdentifierName(m.Identifier))
							)
						)
					)
				)
			);
		});

		return CompilationUnit()
			.WithUsings(typeDeclaration.GetUsings())
			.WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					NamespaceDeclaration(
						IdentifierName(@namespace)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							ClassDeclaration($"{typeName}Decorator")
								.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
								.WithBaseList(
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
						)
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
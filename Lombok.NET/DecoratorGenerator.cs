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

		var methods = typeSymbol.GetAllMembersIncludingInherited().OfType<IMethodSymbol>().Where(s => s.IsAbstract);

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
			.Select(ms => GenerateMethodDeclaration(ms, memberVariableName));

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

	/// <summary>
	/// This is mostly reimplementing SyntaxGenerator.MethodDeclaration (which I don't believe can be used in a source generator).
	/// On top of that, it tweaks the modifiers to be an implementation delegates the call to the <paramref name="delegationTargetIdentifier"/>.
	/// </summary>
	public static MethodDeclarationSyntax GenerateMethodDeclaration(IMethodSymbol symbol, string delegationTargetIdentifier)
	{
		return MethodDeclaration(symbol.ReturnType.ToTypeSyntax(), symbol.Name)
			.WithModifiers(GenerateImplementationModifiers(symbol))
			.WithParameterList(symbol.Parameters.GenerateParameterList())
			.WithBody(Block(GenerateDelegatingCall(symbol, delegationTargetIdentifier)));
	}

	private static StatementSyntax GenerateDelegatingCall(IMethodSymbol symbol, string delegationTargetIdentifier)
	{
		var invocation = InvocationExpression(
			MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				IdentifierName(delegationTargetIdentifier),
				IdentifierName(symbol.Name)
			)
		).WithArgumentList(ArgumentList(SeparatedList(
			symbol.Parameters.Select(p => Argument(IdentifierName(p.Name))
				.WithRefKindKeyword(p.RefKind.GenerateRefKindToken()))
		)));

		return symbol.ReturnsVoid ?
			ExpressionStatement(invocation) :
			ReturnStatement(invocation);
	}

	private static SyntaxTokenList GenerateImplementationModifiers(IMethodSymbol symbol) => TokenList(
		new[]
		{
			symbol.GenerateAccessibilityToken(),
			symbol.IsStatic ? Token(SyntaxKind.StaticKeyword) : Token(SyntaxKind.None),
			symbol.IsOverride || symbol.IsVirtual || symbol.IsAbstract ?
				Token(SyntaxKind.OverrideKeyword) : Token(SyntaxKind.None)
		}.Where(t => !t.IsKind(SyntaxKind.None)
		));
}
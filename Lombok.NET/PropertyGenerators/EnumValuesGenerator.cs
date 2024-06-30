using System.Text;
using Lombok.NET.Analyzers;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.PropertyGenerators;

/// <summary>
/// Generator which generates a property containing an enum's values.
/// </summary>
[Generator]
internal sealed class EnumValuesGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(EnumValuesAttribute).FullName;

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
		return node is EnumDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var enumDeclaration = (EnumDeclarationSyntax)context.TargetNode;
		var @namespace = enumDeclaration.GetNamespace();
		if (@namespace is null)
		{
			var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, enumDeclaration.GetLocation(), enumDeclaration.Identifier.Text);

			return new GeneratorResult(diagnostic);
		}

		if (enumDeclaration.IsNestedType())
		{
			var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBeNonNested, enumDeclaration.GetLocation(), enumDeclaration.Identifier.Text);

			return new GeneratorResult(diagnostic);
		}

		var enumIdentifier = enumDeclaration.Identifier;
		var valuesClassName = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(EnumValuesAttribute.TypeName)).Value.Value as string
		                      ?? enumIdentifier.Text + "Values";

		var enumMembers = GetEnumMemberAccessExpressions(enumIdentifier, enumDeclaration.Members);

		cancellationToken.ThrowIfCancellationRequested();

		var sourceText = @namespace.CreateNewNamespace(
				default,
				ClassDeclaration(valuesClassName)
					.WithModifiers(
						TokenList(
							Token(enumDeclaration.GetAccessibilityModifier()),
							Token(SyntaxKind.StaticKeyword)
						)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							PropertyDeclaration(
								ArrayType(
										IdentifierName(enumIdentifier)
									)
									.WithRankSpecifiers(
										SingletonList(
											ArrayRankSpecifier(
												SingletonSeparatedList<ExpressionSyntax>(
													OmittedArraySizeExpression()
												)
											)
										)
									),
								Identifier("Values")
							).WithModifiers(
								TokenList(
									Token(SyntaxKind.PublicKeyword),
									Token(SyntaxKind.StaticKeyword))
							).WithAccessorList(
								AccessorList(
									SingletonList(
										AccessorDeclaration(
												SyntaxKind.GetAccessorDeclaration
											)
											.WithSemicolonToken(
												Token(SyntaxKind.SemicolonToken)
											)
									)
								)
							).WithInitializer(
								EqualsValueClause(
									InitializerExpression(
										SyntaxKind.ArrayInitializerExpression,
										SeparatedList<ExpressionSyntax>(enumMembers)
									)
								)
							).WithSemicolonToken(
								Token(SyntaxKind.SemicolonToken)
							)
						)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
		var hintName = enumDeclaration.GetHintName(@namespace);

		return new GeneratorResult(hintName, sourceText);
	}

	private static IEnumerable<SyntaxNodeOrToken> GetEnumMemberAccessExpressions(SyntaxToken enumIdentifier, SeparatedSyntaxList<EnumMemberDeclarationSyntax> enumMembers)
	{
		foreach (var enumMember in enumMembers)
		{
			yield return MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				IdentifierName(enumIdentifier),
				IdentifierName(enumMember.Identifier)
			);
			yield return Token(SyntaxKind.CommaToken);
		}
	}
}
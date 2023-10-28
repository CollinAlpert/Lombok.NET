using System.Text;
using System.Threading;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.PropertyGenerators;

/// <summary>
/// Generator which generates a <see cref="System.Lazy{T}"/> property for a class.
/// </summary>
[Generator]
public sealed class LazyGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(LazyAttribute).FullName;

	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG
        SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		var sources = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeName, IsCandidate, Transform);
		context.AddSources(sources);
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node is ClassDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		if (!classDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var lazyPropertyClassSourceText = CreateClassWithLazyProperty(@namespace, classDeclaration);

		return new GeneratorResult(classDeclaration.GetHintName(@namespace), lazyPropertyClassSourceText);
	}

	private static SourceText CreateClassWithLazyProperty(NameSyntax @namespace, ClassDeclarationSyntax classDeclaration)
	{
		var className = classDeclaration.Identifier.Text;

		return @namespace.CreateNewNamespace(
				classDeclaration.CreateNewPartialClass()
					.WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							FieldDeclaration(
								VariableDeclaration(
									GenericName(
										Identifier("global::System.Lazy")
									).WithTypeArgumentList(
										TypeArgumentList(
											SingletonSeparatedList<TypeSyntax>(
												IdentifierName(className)
											)
										)
									)
								).WithVariables(
									SingletonSeparatedList(
										VariableDeclarator(
											Identifier("Lazy")
										).WithInitializer(
											EqualsValueClause(
												ImplicitObjectCreationExpression()
													.WithArgumentList(
														ArgumentList(
															SingletonSeparatedList(
																Argument(
																	ParenthesizedLambdaExpression()
																		.WithExpressionBody(
																			ObjectCreationExpression(
																				IdentifierName(className)
																			).WithArgumentList(
																				ArgumentList()
																			)
																		)
																)
															)
														)
													)
											)
										)
									)
								)
							).WithModifiers(
								TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
							)
						)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
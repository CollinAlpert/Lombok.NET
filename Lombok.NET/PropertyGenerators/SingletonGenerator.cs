using System.Linq;
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
/// Generator which generates the singleton pattern for a class.
/// </summary>
[Generator]
public sealed class SingletonGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(SingletonAttribute).FullName;

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

		var singletonSourceText = CreateSingletonClass(@namespace, classDeclaration);

		return new GeneratorResult(classDeclaration.GetHintName(@namespace), singletonSourceText);
	}

	private static SourceText CreateSingletonClass(NameSyntax @namespace, ClassDeclarationSyntax classDeclaration)
	{
		var className = classDeclaration.Identifier.Text;

		return @namespace.CreateNewNamespace(
				classDeclaration.CreateNewPartialClass()
					.WithMembers(
						List(
							new MemberDeclarationSyntax[]
							{
								PropertyDeclaration(
									IdentifierName(className),
									Identifier("Instance")
								).WithModifiers(
									TokenList(
										Token(SyntaxKind.PublicKeyword),
										Token(SyntaxKind.StaticKeyword)
									)
								).WithExpressionBody(
									ArrowExpressionClause(
										MemberAccessExpression(
											SyntaxKind.SimpleMemberAccessExpression,
											IdentifierName("Nested"),
											IdentifierName("Instance")
										)
									)
								).WithSemicolonToken(
									Token(SyntaxKind.SemicolonToken)
								),
								ClassDeclaration("Nested")
									.WithModifiers(
										TokenList(
											Token(SyntaxKind.PrivateKeyword)
										)
									).WithMembers(
										List(
											new MemberDeclarationSyntax[]
											{
												ConstructorDeclaration(
													Identifier("Nested")
												).WithModifiers(
													TokenList(
														Token(SyntaxKind.StaticKeyword)
													)
												).WithBody(
													Block()
												),
												FieldDeclaration(
														VariableDeclaration(
															IdentifierName(className)
														).WithVariables(
															SingletonSeparatedList(
																VariableDeclarator(
																	Identifier("Instance")
																).WithInitializer(
																	EqualsValueClause(
																		ObjectCreationExpression(
																				IdentifierName(className)
																			)
																			.WithArgumentList(
																				ArgumentList()
																			)
																	)
																)
															)
														)
													)
													.WithModifiers(
														TokenList(
															Token(SyntaxKind.InternalKeyword),
															Token(SyntaxKind.StaticKeyword),
															Token(SyntaxKind.ReadOnlyKeyword)
														)
													)
											}
										)
									)
							}
						)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
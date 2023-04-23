using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Analyzers;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generates a ToString implementation for an enum. The method is called ToText, as the ToString method cannot be overriden through code generation.
/// </summary>
[Generator]
public sealed class ToTextGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(ToStringAttribute).FullName;

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
		return node is EnumDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var enumDeclaration = (EnumDeclarationSyntax)context.TargetNode;
		cancellationToken.ThrowIfCancellationRequested();

		return CreateToStringExtension(enumDeclaration.GetNamespace(), enumDeclaration);
	}

	private static SwitchExpressionArmSyntax CreateSwitchArm(string enumName, string enumMemberName)
	{
		return SwitchExpressionArm(
			ConstantPattern(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					IdentifierName(enumName),
					IdentifierName(enumMemberName)
				)
			),
			InvocationExpression(
				IdentifierName(
					Identifier(
						TriviaList(),
						SyntaxKind.NameOfKeyword,
						"nameof",
						"nameof",
						TriviaList()
					)
				)
			).WithArgumentList(
				ArgumentList(
					SingletonSeparatedList(
						Argument(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName(enumName),
								IdentifierName(enumMemberName)
							)
						)
					)
				)
			)
		);
	}

	private static GeneratorResult CreateToStringExtension(NameSyntax? @namespace, EnumDeclarationSyntax enumDeclaration)
	{
		var enumName = enumDeclaration.Identifier.Text;
		var extensionClassName = $"{enumName}Extensions";
		var switchArms = enumDeclaration.Members.Select(member => CreateSwitchArm(enumName, member.Identifier.Text)).ToArray();
		if (switchArms.Length == 0)
		{
			return GeneratorResult.Empty;
		}

		if (@namespace is null)
		{
			var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, enumDeclaration.GetLocation());

			return new GeneratorResult(diagnostic);
		}

		var switchArmList = new SyntaxNodeOrToken[enumDeclaration.Members.Count * 2 + 1];
		for (int i = 0; i < switchArms.Length; i++)
		{
			switchArmList[i * 2] = switchArms[i];
			switchArmList[i * 2 + 1] = Token(SyntaxKind.CommaToken);
		}

		SwitchExpressionArmSyntax CreateDiscardArm()
		{
			return SwitchExpressionArm(
				DiscardPattern(),
				ThrowExpression(
					ObjectCreationExpression(
						IdentifierName("global::System.ArgumentOutOfRangeException")
					).WithArgumentList(
						ArgumentList(
							SeparatedList<ArgumentSyntax>(
								new SyntaxNodeOrToken[]
								{
									Argument(
										InvocationExpression(
												IdentifierName(
													Identifier(
														TriviaList(),
														SyntaxKind.NameOfKeyword,
														"nameof",
														"nameof",
														TriviaList()
													)
												)
											)
											.WithArgumentList(
												ArgumentList(
													SingletonSeparatedList(
														Argument(
															IdentifierName(enumName.Decapitalize()!)
														)
													)
												)
											)
									),
									Token(SyntaxKind.CommaToken),
									Argument(
										IdentifierName(enumName.Decapitalize()!)
									),
									Token(SyntaxKind.CommaToken),
									Argument(
										LiteralExpression(
											SyntaxKind.NullLiteralExpression
										)
									)
								}
							)
						)
					)
				)
			);
		}

		switchArmList[switchArmList.Length - 1] = CreateDiscardArm();

		var nullabilityTrivia = SyntaxTriviaList.Empty;
		if (enumDeclaration.ShouldEmitNrtTrivia())
		{
			nullabilityTrivia = Extensions.SyntaxNodeExtensions.NullableTrivia;
		}

		var source = @namespace.CreateNewNamespace(
				ClassDeclaration(extensionClassName)
					.WithModifiers(
						TokenList(
							Token(SyntaxKind.PublicKeyword).WithLeadingTrivia(nullabilityTrivia),
							Token(SyntaxKind.StaticKeyword)
						)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							MethodDeclaration(
								PredefinedType(
									Token(SyntaxKind.StringKeyword)
								),
								Identifier("ToText")
							).WithModifiers(
								TokenList(
									Token(enumDeclaration.GetAccessibilityModifier()),
									Token(SyntaxKind.StaticKeyword)
								)
							).WithParameterList(
								ParameterList(
									SingletonSeparatedList(
										Parameter(
											Identifier(enumName.Decapitalize()!)
										).WithModifiers(
											TokenList(
												Token(SyntaxKind.ThisKeyword)
											)
										).WithType(
											IdentifierName(enumName)
										)
									)
								)
							).WithBody(
								Block(
									SingletonList<StatementSyntax>(
										ReturnStatement(
											SwitchExpression(
												IdentifierName(enumName.Decapitalize()!)
											).WithArms(
												SeparatedList<SwitchExpressionArmSyntax>(
													switchArmList
												)
											)
										)
									)
								)
							)
						)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
		var hintName = string.Concat(@namespace.ToString().Replace('.', '_'), '_', extensionClassName);

		return new GeneratorResult(hintName, source);
	}
}
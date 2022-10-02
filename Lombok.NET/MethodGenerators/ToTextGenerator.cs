using System;
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

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generates a ToString implementation for an enum. The method is called ToText, as the ToString method cannot be overriden through code generation.
/// </summary>
[Generator]
public class ToTextGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG
        SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(static s => s != null);
		context.RegisterSourceOutput(sources, static (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node.TryConvertToEnum(out var enumDeclaration) &&
		       enumDeclaration.AttributeLists
			       .SelectMany(static l => l.Attributes)
			       .Any(static a => a.IsNamed("ToString"));
	}

	private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		SymbolCache.ToStringAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<ToStringAttribute>();

		var enumDeclaration = (EnumDeclarationSyntax)context.Node;
		if (!enumDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.ToStringAttributeSymbol))
		{
			return null;
		}

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

	private static SourceText? CreateToStringExtension(string? @namespace, EnumDeclarationSyntax enumDeclaration)
	{
		var enumName = enumDeclaration.Identifier.Text;
		var switchArms = enumDeclaration.Members.Select(member => CreateSwitchArm(enumName, member.Identifier.Text)).ToArray();
		// Caught by LOM003
		if (switchArms.Length == 0 || @namespace is null)
		{
			return null;
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
						IdentifierName("ArgumentOutOfRangeException")
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


		return CompilationUnit()
			.WithUsings(
				SingletonList(
					UsingDirective(
						IdentifierName("System")
					)
				)
			).WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					FileScopedNamespaceDeclaration(
						IdentifierName(@namespace)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							ClassDeclaration($"{enumName}Extensions")
								.WithModifiers(
									TokenList(
										Token(SyntaxKind.PublicKeyword),
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
						)
					)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
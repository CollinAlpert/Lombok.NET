using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Lombok.NET.MethodGenerators
{
	[Generator]
	public class ToStringGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new ToStringSyntaxReceiver());
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!(context.SyntaxContextReceiver is ToStringSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var classDeclaration in syntaxReceiver.ClassCandidates)
			{
				classDeclaration.EnsurePartial();
				classDeclaration.EnsureNamespace(out var @namespace);
				var toStringMethod = CreateToStringMethod(classDeclaration);
				if (toStringMethod is null)
				{
					continue;
				}

				context.AddSource(classDeclaration.Identifier.Text, CreateType(@namespace, classDeclaration.CreateNewPartialType(), toStringMethod));
			}

			foreach (var structDeclaration in syntaxReceiver.StructCandidates)
			{
				structDeclaration.EnsurePartial();
				structDeclaration.EnsureNamespace(out var @namespace);
				var toStringMethod = CreateToStringMethod(structDeclaration);
				if (toStringMethod is null)
				{
					continue;
				}

				context.AddSource(structDeclaration.Identifier.Text, CreateType(@namespace, structDeclaration.CreateNewPartialType(), toStringMethod));
			}

			foreach (var enumDeclaration in syntaxReceiver.EnumCandidates)
			{
				enumDeclaration.EnsureNamespace(out var @namespace);
				var toStringExtension = CreateToStringExtension(@namespace, enumDeclaration);
				if (toStringExtension is null)
				{
					continue;
				}

				context.AddSource(enumDeclaration.Identifier.Text, toStringExtension);
			}
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

		private static SourceText CreateToStringExtension(string @namespace, EnumDeclarationSyntax enumDeclaration)
		{
			var enumName = enumDeclaration.Identifier.Text;
			var switchArms = enumDeclaration.Members.Select(member => CreateSwitchArm(enumName, member.Identifier.Text)).ToArray();
			if (switchArms.Length == 0)
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
																IdentifierName(enumName.Decapitalize())
															)
														)
													)
												)
										),
										Token(SyntaxKind.CommaToken),
										Argument(
											IdentifierName(enumName.Decapitalize())
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

			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(
					SingletonList(
						UsingDirective(
							IdentifierName("System")
						)
					)
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
													Identifier(enumName.Decapitalize())
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
														IdentifierName(enumName.Decapitalize())
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
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}

		private static MethodDeclarationSyntax CreateToStringMethod(TypeDeclarationSyntax typeDeclaration)
		{
			var memberType = typeDeclaration.GetAttributeArgument<MemberType>("ToString") ?? MemberType.Field;
			var accessType = typeDeclaration.GetAttributeArgument<AccessTypes>("ToString") ?? AccessTypes.Private;

			string[] identifiers;
			switch (memberType)
			{
				case MemberType.Property:
					identifiers = typeDeclaration.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(accessType)
						.Select(p => p.Identifier.Text)
						.ToArray();

					break;
				case MemberType.Field:
					identifiers = typeDeclaration.Members
						.OfType<FieldDeclarationSyntax>()
						.Where(accessType)
						.SelectMany(f => f.Declaration.Variables.Select(v => v.Identifier.Text))
						.ToArray();

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(memberType));
			}

			if (identifiers.Length == 0)
			{
				return null;
			}

			var stringInterpolationContent = new List<InterpolatedStringContentSyntax>
			{
				CreateStringInterpolationContent(typeDeclaration.Identifier.Text + ": "),
				CreateStringInterpolationContent(identifiers[0] + "="),
				Interpolation(
					IdentifierName(identifiers[0])
				)
			};

			for (int i = 1; i < identifiers.Length; i++)
			{
				stringInterpolationContent.Add(CreateStringInterpolationContent("; " + identifiers[i] + "="));
				stringInterpolationContent.Add(Interpolation(IdentifierName(identifiers[i])));
			}

			return MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.StringKeyword)
				),
				"ToString"
			).WithModifiers(
				TokenList(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.OverrideKeyword)
				)
			).WithBody(
				Block(
					ReturnStatement(
						InterpolatedStringExpression(
							Token(SyntaxKind.InterpolatedStringStartToken)
						).WithContents(List(stringInterpolationContent))
					)
				)
			);
		}

		private static InterpolatedStringContentSyntax CreateStringInterpolationContent(string s)
		{
			return InterpolatedStringText(
				Token(
					TriviaList(),
					SyntaxKind.InterpolatedStringTextToken,
					s,
					s,
					TriviaList()
				)
			);
		}

		private static SourceText CreateType(string @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax toStringMethod)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.WithMembers(
							new SyntaxList<MemberDeclarationSyntax>(toStringMethod)
						)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
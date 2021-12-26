using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Lombok.NET
{
	[Generator]
	public class SingletonGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SingletonSyntaxReceiver());
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!(context.SyntaxContextReceiver is SingletonSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var typeDeclaration in syntaxReceiver.Candidates)
			{
				if (!(typeDeclaration is ClassDeclarationSyntax classDeclaration))
				{
					throw new NotSupportedException("Only classes are supported for the 'Singleton' attribute.");
				}

				if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					throw new NotSupportedException("Class must be partial.");
				}

				var @namespace = classDeclaration.GetNamespace();
				if (@namespace is null)
				{
					throw new Exception($"Namespace could not be found for {typeDeclaration.Identifier.Text}.");
				}
				
				context.AddSource(classDeclaration.Identifier.Text, CreateSingletonClass(@namespace, classDeclaration));
			}
		}

		private static SourceText CreateSingletonClass(string @namespace, ClassDeclarationSyntax classDeclaration)
		{
			var className = classDeclaration.Identifier.Text;
			
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						ClassDeclaration(className)
							.WithModifiers(
								TokenList(
									Token(classDeclaration.GetAccessibilityModifier()),
									Token(SyntaxKind.PartialKeyword)
								)
							).WithMembers(
								List(
									new MemberDeclarationSyntax[]
									{
										ConstructorDeclaration(
											Identifier(className)
										).WithModifiers(
											TokenList(
												Token(SyntaxKind.PrivateKeyword)
											)
										).WithBody(
											Block()
										),
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
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
using System.Collections.Generic;
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

namespace Lombok.NET.PropertyGenerators
{
	public abstract class BasePropertyChangeGenerator : ISourceGenerator
	{
		protected abstract BaseAttributeSyntaxReceiver SyntaxReceiver { get; }
		
		protected abstract string ImplementingInterfaceName { get; }

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => SyntaxReceiver);
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxContextReceiver == null || context.SyntaxContextReceiver.GetType() != SyntaxReceiver.GetType())
			{
				return;
			}

			foreach (var typeDeclaration in SyntaxReceiver.Candidates)
			{
				typeDeclaration.EnsureClass("The notify pattern can only be generated for classes.", out var classDeclaration);
				classDeclaration.EnsurePartial();
				classDeclaration.EnsureNamespace(out var @namespace);

				context.AddSource(classDeclaration.Identifier.Text, CreateImplementationClass(@namespace, classDeclaration));
			}
		}

		protected abstract IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment);
		
		protected abstract EventFieldDeclarationSyntax CreateEventField();

		protected abstract MethodDeclarationSyntax CreateSetFieldMethod();

		private SourceText CreateImplementationClass(string @namespace, ClassDeclarationSyntax classDeclaration)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(
					List(
						new []
						{
							UsingDirective(
								QualifiedName(
									IdentifierName("System"),
									IdentifierName("ComponentModel")
								)
							),
							UsingDirective(
								QualifiedName(
									QualifiedName(
										IdentifierName("System"),
										IdentifierName("Runtime")
									),
									IdentifierName("CompilerServices")
								)
							)
						}
					)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						ClassDeclaration(classDeclaration.Identifier.Text)
							.WithModifiers(
								TokenList(Token(classDeclaration.GetAccessibilityModifier()), Token(SyntaxKind.PartialKeyword))
							).WithBaseList(
								BaseList(
									SingletonSeparatedList<BaseTypeSyntax>(
										SimpleBaseType(
											IdentifierName(ImplementingInterfaceName)
										)
									)
								)
							).WithMembers(
								List(
									new MemberDeclarationSyntax[]
									{
										CreateEventField(),
										CreateSetFieldMethod().WithModifiers(
											TokenList(
												Token(SyntaxKind.PrivateKeyword)
											)
										).WithTypeParameterList(
											TypeParameterList(
												SingletonSeparatedList(
													TypeParameter(
														Identifier("T")
													)
												)
											)
										).WithParameterList(
											ParameterList(
												SeparatedList<ParameterSyntax>(
													new SyntaxNodeOrToken[]
													{
														Parameter(
															Identifier(
																TriviaList(),
																SyntaxKind.FieldKeyword,
																"field",
																"field",
																TriviaList()
															)
														).WithModifiers(
															TokenList(
																Token(SyntaxKind.OutKeyword)
															)
														).WithType(
															IdentifierName("T")
														),
														Token(SyntaxKind.CommaToken), Parameter(
															Identifier("newValue")
														).WithType(
															IdentifierName("T")
														),
														Token(SyntaxKind.CommaToken), Parameter(
															Identifier("propertyName")
														).WithAttributeLists(
															SingletonList(
																AttributeList(
																	SingletonSeparatedList(
																		Attribute(
																			IdentifierName("CallerMemberName")
																		)
																	)
																)
															)
														).WithType(
															PredefinedType(
																Token(SyntaxKind.StringKeyword)
															)
														).WithDefault(
															EqualsValueClause(
																LiteralExpression(
																	SyntaxKind.NullLiteralExpression
																)
															)
														)
													}
												)
											)
										).WithBody(
											Block(CreateAssignmentWithPropertyChangeMethod(CreateNewValueAssignmentExpression()))
										)
									}
								)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}

		private static ExpressionStatementSyntax CreateNewValueAssignmentExpression()
		{
			return ExpressionStatement(
				AssignmentExpression(
					SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(
						Identifier(
							TriviaList(),
							SyntaxKind.FieldKeyword,
							"field",
							"field",
							TriviaList()
						)
					),
					IdentifierName("newValue")
				)
			);
		}
	}
}
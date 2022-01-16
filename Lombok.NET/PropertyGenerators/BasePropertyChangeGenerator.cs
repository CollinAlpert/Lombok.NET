using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

				var statements = CreateAssignmentWithPropertyChangeMethod(CreateNewValueAssignmentExpression(), CreatePropertyChangeExpression());
				context.AddSource(classDeclaration.Identifier.Text, CreateImplementationClass(@namespace, classDeclaration, statements));
			}
		}
		
		protected abstract IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment, ExpressionStatementSyntax propertyChangeCall);

		private static SourceText CreateImplementationClass(string @namespace, ClassDeclarationSyntax classDeclaration, IEnumerable<StatementSyntax> setFieldBody)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(
					SingletonList(
						UsingDirective(
							QualifiedName(
								IdentifierName("System"),
								IdentifierName("ComponentModel")
							)
						)
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
											IdentifierName("INotifyPropertyChanged")
										)
									)
								)
							).WithMembers(
								List(
									new MemberDeclarationSyntax[]
									{
										EventFieldDeclaration(
												VariableDeclaration(
														IdentifierName("PropertyChangedEventHandler")
													).WithVariables(
														SingletonSeparatedList(
															VariableDeclarator(
																Identifier("PropertyChanged")
															)
														)
													)
											).WithModifiers(
												TokenList(
													Token(SyntaxKind.PublicKeyword)
												)
											),
										MethodDeclaration(
												PredefinedType(
													Token(SyntaxKind.VoidKeyword)
												),
												Identifier("SetField")
											).WithModifiers(
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
												Block(setFieldBody)
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

		private static ExpressionStatementSyntax CreatePropertyChangeExpression()
		{
			return ExpressionStatement(
				ConditionalAccessExpression(
					IdentifierName("PropertyChanged"),
					InvocationExpression(
						MemberBindingExpression(
							IdentifierName("Invoke")
						)
					).WithArgumentList(
						ArgumentList(
							SeparatedList<ArgumentSyntax>(
								new SyntaxNodeOrToken[]
								{
									Argument(
										ThisExpression()
									),
									Token(SyntaxKind.CommaToken), Argument(
										ObjectCreationExpression(
											IdentifierName("PropertyChangedEventArgs")
										).WithArgumentList(
											ArgumentList(
												SingletonSeparatedList(
													Argument(
														IdentifierName("propertyName")
													)
												)
											)
										)
									)
								}
							)
						)
					)
				)
			);
		}
	}

	partial class Test : INotifyPropertyChanged
	{
		private int _age;

		public int Age
		{
			get => _age;
			set => SetField(out _age, value);
		}
	}

	partial class Test : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private void SetField<T>(out T field, T newValue, [CallerMemberName] string propertyName = null)
		{
			field = newValue;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
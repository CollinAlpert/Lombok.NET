using System;
using System.Collections.Generic;
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

namespace Lombok.NET.PropertyGenerators
{
	public abstract class BasePropertyChangeGenerator : IIncrementalGenerator
	{
		protected abstract string ImplementingInterfaceName { get; }

		protected abstract string AttributeName { get; }

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
		}

		private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			return node is ClassDeclarationSyntax classDeclaration
			       && classDeclaration.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == AttributeName);
		}

		private SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			if (cancellationToken.IsCancellationRequested
			    || !classDeclaration.ContainsAttribute(context.SemanticModel, GetAttributeSymbol(context.SemanticModel))
			    // Caught by LOM001, LOM002 and LOM003 
			    || !classDeclaration.CanGenerateCodeForType(out var @namespace))
			{
				return null;
			}

			return CreateImplementationClass(@namespace, classDeclaration);
		}

		protected abstract IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment);

		protected abstract EventFieldDeclarationSyntax CreateEventField();

		protected abstract MethodDeclarationSyntax CreateSetFieldMethod();

		protected abstract INamedTypeSymbol GetAttributeSymbol(SemanticModel semanticModel);

		private SourceText CreateImplementationClass(string @namespace, ClassDeclarationSyntax classDeclaration)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(
					List(
						new[]
						{
							"System.ComponentModel".CreateUsingDirective(),
							"System.Runtime.CompilerServices".CreateUsingDirective(),
						}
					)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						classDeclaration.CreateNewPartialClass()
							.WithBaseList(
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
														Token(SyntaxKind.CommaToken),
														Parameter(
															Identifier("newValue")
														).WithType(
															IdentifierName("T")
														),
														Token(SyntaxKind.CommaToken),
														Parameter(
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
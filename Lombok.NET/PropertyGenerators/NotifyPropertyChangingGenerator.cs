using System.Collections.Generic;
using System.ComponentModel;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class NotifyPropertyChangingGenerator : BasePropertyChangeGenerator
	{
		public const string SetFieldMethodName = "SetFieldAndRaisePropertyChanging";

		protected override string ImplementingInterfaceName { get; } = nameof(INotifyPropertyChanging);
		
		protected override string AttributeName { get; } = "NotifyPropertyChanging";

		protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment)
		{
			return new[] { CreatePropertyChangeInvocation(), newValueAssignment };
		}

		protected override INamedTypeSymbol GetAttributeSymbol(SemanticModel semanticModel)
		{
			return SymbolCache.NotifyPropertyChangingAttributeSymbol ??= semanticModel.Compilation.GetSymbolByType<NotifyPropertyChangingAttribute>();
		}

		protected override EventFieldDeclarationSyntax CreateEventField()
		{
			return EventFieldDeclaration(
				VariableDeclaration(
					IdentifierName("PropertyChangingEventHandler")
				).WithVariables(
					SingletonSeparatedList(
						VariableDeclarator(
							Identifier("PropertyChanging")
						)
					)
				)
			).WithModifiers(
				TokenList(
					Token(SyntaxKind.PublicKeyword)
				)
			);
		}

		protected override MethodDeclarationSyntax CreateSetFieldMethod()
		{
			return MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.VoidKeyword)
				),
				Identifier(SetFieldMethodName)
			);
		}

		private static ExpressionStatementSyntax CreatePropertyChangeInvocation()
		{
			return ExpressionStatement(
				ConditionalAccessExpression(
					IdentifierName("PropertyChanging"),
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
											IdentifierName("PropertyChangingEventArgs")
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
}
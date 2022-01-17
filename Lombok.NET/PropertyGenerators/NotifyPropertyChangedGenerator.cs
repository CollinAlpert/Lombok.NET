using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class NotifyPropertyChangedGenerator : BasePropertyChangeGenerator
	{
		public const string SetFieldMethodName = "SetFieldAndRaisePropertyChanged";

		protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new NotifyPropertyChangedSyntaxReceiver();
		
		protected override string ImplementingInterfaceName { get; } = nameof(INotifyPropertyChanged);

		protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment)
		{
			return new[] { newValueAssignment, CreatePropertyChangeInvocation() };
		}

		protected override EventFieldDeclarationSyntax CreateEventField()
		{
			return EventFieldDeclaration(
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
}
using System.Collections.Generic;
using System.ComponentModel;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.PropertyGenerators;

/// <summary>
/// Generator which implements the INotifyPropertyChanged interface for a class.
/// </summary>
[Generator]
public sealed class NotifyPropertyChangingGenerator : BasePropertyChangeGenerator
{
	/// <summary>
	/// The name of the method which will be available for setting a field and raising the event.
	/// </summary>
	public const string SetFieldMethodName = "SetFieldAndRaisePropertyChanging";

	/// <summary>
	/// The name of the attribute the generator targets.
	/// </summary>
	protected override string ImplementingInterfaceName { get; } = nameof(INotifyPropertyChanging);

	/// <summary>
	/// The name of the attribute the generator targets.
	/// </summary>
	protected override string AttributeName { get; } = typeof(NotifyPropertyChangingAttribute).FullName;

	/// <summary>
	/// Creates the body of the method which sets a field and raises the event.
	/// This is important for the order in which these two statements can happen
	/// </summary>
	/// <param name="newValueAssignment"></param>
	/// <returns>A list of statements which the method executes.</returns>
	protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment)
	{
		return new[] { CreatePropertyChangeInvocation(), newValueAssignment };
	}

	/// <summary>
	/// Creates the event field.
	/// </summary>
	/// <returns>The event field.</returns>
	protected override EventFieldDeclarationSyntax CreateEventField()
	{
		return EventFieldDeclaration(
			VariableDeclaration(
				NullableType(
					IdentifierName("global::System.ComponentModel.PropertyChangingEventHandler")
				)
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

	/// <summary>
	/// Creates the method which contains the event invocation plus allows the setting of a field.
	/// </summary>
	/// <returns>The method definition.</returns>
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
								Token(SyntaxKind.CommaToken),
								Argument(
									ObjectCreationExpression(
										IdentifierName("global::System.ComponentModel.PropertyChangingEventArgs")
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
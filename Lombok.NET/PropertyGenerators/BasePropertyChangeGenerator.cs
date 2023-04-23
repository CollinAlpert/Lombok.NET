using System.Collections.Generic;
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

namespace Lombok.NET.PropertyGenerators;

/// <summary>
/// Base class for generators which generate property change functionality.
/// </summary>
public abstract class BasePropertyChangeGenerator : IIncrementalGenerator
{
	/// <summary>
	/// The name of the interface which dictates the property change contracts and which will be implemented.
	/// </summary>
	protected abstract string ImplementingInterfaceName { get; }

	/// <summary>
	/// The name of the attribute the generator targets.
	/// </summary>
	protected abstract string AttributeName { get; }

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
		return node is ClassDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		if (!classDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var implementationSourceText = CreateImplementationClass(@namespace, classDeclaration);

		return new GeneratorResult(classDeclaration.GetHintName(@namespace), implementationSourceText);
	}

	/// <summary>
	/// Creates the body of the method which sets a field and raises the event.
	/// This is important for the order in which these two statements can happen
	/// </summary>
	/// <param name="newValueAssignment"></param>
	/// <returns>A list of statements which the method executes.</returns>
	protected abstract IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment);

	/// <summary>
	/// Creates the event field.
	/// </summary>
	/// <returns>The event field.</returns>
	protected abstract EventFieldDeclarationSyntax CreateEventField();

	/// <summary>
	/// Creates the method which contains the event invocation plus allows the setting of a field.
	/// </summary>
	/// <returns>The method definition.</returns>
	protected abstract MethodDeclarationSyntax CreateSetFieldMethod();

	private SourceText CreateImplementationClass(NameSyntax @namespace, ClassDeclarationSyntax classDeclaration)
	{
		return @namespace.CreateNewNamespace(
				classDeclaration.CreateNewPartialClass()
					.WithBaseList(
						BaseList(
							SingletonSeparatedList<BaseTypeSyntax>(
								SimpleBaseType(
									IdentifierName("global::System.ComponentModel." + ImplementingInterfaceName)
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
										Token(SyntaxKind.ProtectedKeyword)
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
																	IdentifierName("global::System.Runtime.CompilerServices.CallerMemberName")
																)
															)
														)
													)
												).WithType(
													NullableType(
														PredefinedType(
															Token(SyntaxKind.StringKeyword)
														)
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
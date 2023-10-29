using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generates the freezable pattern for a class or struct.
/// </summary>
[Generator]
public sealed class FreezablePatternGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(FreezableAttribute).FullName;

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
		return node is ClassDeclarationSyntax or StructDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
		var freezableProperties = typeSymbol.GetMembers()
			.OfType<IFieldSymbol>()
			.Where(f => !f.IsReadOnly && f.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeName))
			.Select(CreateFreezableProperty);

		var typeName = typeDeclaration.Identifier.Text;
		var freezerMembers = CreateFreezeMembers(typeName);
		
		var generateUnfreezeMethods = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(FreezableAttribute.IsUnfreezable)).Value.Value as bool? ?? true;
		var unfreezeMethods = generateUnfreezeMethods
			? CreateUnfreezeMethods(typeName)
			: Enumerable.Empty<MemberDeclarationSyntax>();
		var sourceText = @namespace.CreateNewNamespace(
				typeDeclaration.GetUsings(),
				typeDeclaration.CreateNewPartialType()
					.WithMembers(
						List(freezableProperties.Concat(freezerMembers).Concat(unfreezeMethods))
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
		var hint = typeDeclaration.GetHintName(@namespace);

		return new GeneratorResult(hint, sourceText);
	}

	private static MemberDeclarationSyntax CreateFreezableProperty(IFieldSymbol field)
	{
		return PropertyDeclaration(
			IdentifierName(field.Type.ToDisplayString()),
			Identifier(field.Name.ToPascalCaseIdentifier())
		).WithModifiers(
			TokenList(
				Token(SyntaxKind.PublicKeyword)
			)
		).WithAccessorList(
			AccessorList(
				List(
					new[]
					{
						AccessorDeclaration(
							SyntaxKind.GetAccessorDeclaration
						).WithExpressionBody(
							ArrowExpressionClause(
								IdentifierName(field.Name)
							)
						).WithSemicolonToken(
							Token(SyntaxKind.SemicolonToken)
						),
						AccessorDeclaration(
							SyntaxKind.SetAccessorDeclaration
						).WithBody(
							Block(
								IfStatement(
									IdentifierName("IsFrozen"),
									Block(
										SingletonList<StatementSyntax>(
											ThrowStatement(
												ObjectCreationExpression(
													IdentifierName("global::System.InvalidOperationException")
												).WithArgumentList(
													ArgumentList(
														SingletonSeparatedList(
															Argument(
																LiteralExpression(
																	SyntaxKind.StringLiteralExpression,
																	Literal($"'{field.ContainingType.Name}' is frozen and cannot be modified.")
																)
															)
														)
													)
												)
											)
										)
									)
								),
								ExpressionStatement(
									AssignmentExpression(
										SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(field.Name),
										IdentifierName("value")
									)
								)
							)
						)
					}
				)
			)
		);
	}

	private static MemberDeclarationSyntax[] CreateFreezeMembers(string typeName)
	{
		return new MemberDeclarationSyntax[]
		{
			PropertyDeclaration(
				PredefinedType(
					Token(SyntaxKind.BoolKeyword)
				),
				Identifier("IsFrozen")
			).WithModifiers(
				TokenList(
					Token(SyntaxKind.PublicKeyword)
				)
			).WithAccessorList(
				AccessorList(
					List(
						new[]
						{
							AccessorDeclaration(
								SyntaxKind.GetAccessorDeclaration
							).WithSemicolonToken(
								Token(SyntaxKind.SemicolonToken)
							),
							AccessorDeclaration(
								SyntaxKind.SetAccessorDeclaration
							).WithModifiers(
								TokenList(
									Token(SyntaxKind.PrivateKeyword)
								)
							).WithSemicolonToken(
								Token(SyntaxKind.SemicolonToken)
							)
						}
					)
				)
			),
			MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.VoidKeyword)
				),
				Identifier("Freeze")
			).WithModifiers(
				TokenList(
					Token(
						TriviaList(
							Trivia(
								DocumentationCommentTrivia(
									SyntaxKind.SingleLineDocumentationCommentTrivia,
									List(
										new XmlNodeSyntax[]
										{
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	$" Freezes this '{typeName}' instance.",
																	$" Freezes this '{typeName}' instance.",
																	TriviaList()
																), XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	" ",
																	" ",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("summary")
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("summary")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														), XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("\t///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(
																XmlTextLiteral(
																	TriviaList(),
																	"When this instance has already been frozen.",
																	"When this instance has already been frozen.",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("exception")
													)
												).WithAttributes(
													SingletonList<XmlAttributeSyntax>(
														XmlCrefAttribute(
															NameMemberCref(
																IdentifierName("InvalidOperationException")
															)
														)
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("exception")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														)
													)
												)
										}
									)
								)
							)
						),
						SyntaxKind.PublicKeyword,
						TriviaList()
					)
				)
			).WithBody(
				Block(
					IfStatement(
						IdentifierName("IsFrozen"),
						Block(
							SingletonList<StatementSyntax>(
								ThrowStatement(
									ObjectCreationExpression(
										IdentifierName("InvalidOperationException")
									).WithArgumentList(
										ArgumentList(
											SingletonSeparatedList(
												Argument(
													LiteralExpression(
														SyntaxKind.StringLiteralExpression,
														Literal($"'{typeName}' is already frozen.")
													)
												)
											)
										)
									)
								)
							)
						)
					),
					ExpressionStatement(
						AssignmentExpression(
							SyntaxKind.SimpleAssignmentExpression,
							IdentifierName("IsFrozen"),
							LiteralExpression(
								SyntaxKind.TrueLiteralExpression
							)
						)
					)
				)
			),
			MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.BoolKeyword)
				),
				Identifier("TryFreeze")
			).WithModifiers(
				TokenList(
					Token(
						TriviaList(
							Trivia(
								DocumentationCommentTrivia(
									SyntaxKind.SingleLineDocumentationCommentTrivia,
									List(
										new XmlNodeSyntax[]
										{
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	$" Tries to freeze this '{typeName}' instance.",
																	$" Tries to freeze this '{typeName}' instance.",
																	TriviaList()
																), XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	" ",
																	" ",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("summary")
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("summary")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														), XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("\t///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(
																XmlTextLiteral(
																	TriviaList(),
																	"'true' when freezing was successful, 'false' when the instance was already frozen.",
																	"'true' when freezing was successful, 'false' when the instance was already frozen.",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("returns")
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("returns")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														)
													)
												)
										}
									)
								)
							)
						),
						SyntaxKind.PublicKeyword,
						TriviaList()
					)
				)
			).WithBody(
				Block(
					IfStatement(
						IdentifierName("IsFrozen"),
						Block(
							SingletonList<StatementSyntax>(
								ReturnStatement(
									LiteralExpression(
										SyntaxKind.FalseLiteralExpression
									)
								)
							)
						)
					),
					ReturnStatement(
						AssignmentExpression(
							SyntaxKind.SimpleAssignmentExpression,
							IdentifierName("IsFrozen"),
							LiteralExpression(
								SyntaxKind.TrueLiteralExpression
							)
						)
					)
				)
			)
		};
	}

	private static MemberDeclarationSyntax[] CreateUnfreezeMethods(string typeName)
	{
		return new MemberDeclarationSyntax[]
		{
			MethodDeclaration(
					PredefinedType(
						Token(SyntaxKind.VoidKeyword)
					),
					Identifier("Unfreeze")
				).WithModifiers(
					TokenList(
						Token(
							TriviaList(
								Trivia(
									DocumentationCommentTrivia(
										SyntaxKind.SingleLineDocumentationCommentTrivia,
										List(
											new XmlNodeSyntax[]
											{
												XmlText()
													.WithTextTokens(
														TokenList(
															XmlTextLiteral(
																TriviaList(
																	DocumentationCommentExterior("///")
																),
																" ",
																" ",
																TriviaList()
															)
														)
													),
												XmlExampleElement(
													SingletonList<XmlNodeSyntax>(
														XmlText()
															.WithTextTokens(
																TokenList(XmlTextNewLine(
																		TriviaList(),
																		"\n",
																		"\n",
																		TriviaList()
																	), XmlTextLiteral(
																		TriviaList(
																			DocumentationCommentExterior("\t///")
																		),
																		$" Unfreezes this '{typeName}' instance.",
																		$" Unfreezes this '{typeName}' instance.",
																		TriviaList()
																	), XmlTextNewLine(
																		TriviaList(),
																		"\n",
																		"\n",
																		TriviaList()
																	), XmlTextLiteral(
																		TriviaList(
																			DocumentationCommentExterior("\t///")
																		),
																		" ",
																		" ",
																		TriviaList()
																	)
																)
															)
													)
												).WithStartTag(
													XmlElementStartTag(
														XmlName(
															Identifier("summary")
														)
													)
												).WithEndTag(
													XmlElementEndTag(
														XmlName(
															Identifier("summary")
														)
													)
												),
												XmlText()
													.WithTextTokens(
														TokenList(XmlTextNewLine(
																TriviaList(),
																"\n",
																"\n",
																TriviaList()
															), XmlTextLiteral(
																TriviaList(
																	DocumentationCommentExterior("\t///")
																),
																" ",
																" ",
																TriviaList()
															)
														)
													),
												XmlExampleElement(
														SingletonList<XmlNodeSyntax>(
															XmlText()
																.WithTextTokens(
																	TokenList(
																		XmlTextLiteral(
																			TriviaList(),
																			"When this instance is not frozen.",
																			"When this instance is not frozen.",
																			TriviaList()
																		)
																	)
																)
														)
													)
													.WithStartTag(
														XmlElementStartTag(
															XmlName(
																Identifier("exception")
															)
														).WithAttributes(
															SingletonList<XmlAttributeSyntax>(
																XmlCrefAttribute(
																	NameMemberCref(
																		IdentifierName("InvalidOperationException")
																	)
																)
															)
														)
													).WithEndTag(
														XmlElementEndTag(
															XmlName(
																Identifier("exception")
															)
														)
													),
												XmlText()
													.WithTextTokens(
														TokenList(
															XmlTextNewLine(
																TriviaList(),
																"\n",
																"\n",
																TriviaList()
															)
														)
													)
											}
										)
									)
								)
							),
							SyntaxKind.PublicKeyword,
							TriviaList()
						)
					)
				)
				.WithBody(
					Block(
						IfStatement(
							PrefixUnaryExpression(
								SyntaxKind.LogicalNotExpression,
								IdentifierName("IsFrozen")
							),
							Block(
								SingletonList<StatementSyntax>(
									ThrowStatement(
										ObjectCreationExpression(
											IdentifierName("InvalidOperationException")
										).WithArgumentList(
											ArgumentList(
												SingletonSeparatedList(
													Argument(
														LiteralExpression(
															SyntaxKind.StringLiteralExpression,
															Literal($"'{typeName}' is not frozen.")
														)
													)
												)
											)
										)
									)
								)
							)
						),
						ExpressionStatement(
							AssignmentExpression(
								SyntaxKind.SimpleAssignmentExpression,
								IdentifierName("IsFrozen"),
								LiteralExpression(
									SyntaxKind.FalseLiteralExpression
								)
							)
						)
					)
				),
			MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.BoolKeyword)
				),
				Identifier("TryUnfreeze")
			).WithModifiers(
				TokenList(
					Token(
						TriviaList(
							Trivia(
								DocumentationCommentTrivia(
									SyntaxKind.SingleLineDocumentationCommentTrivia,
									List(
										new XmlNodeSyntax[]
										{
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	$" Tries to unfreeze this '{typeName}' instance.",
																	$" Tries to unfreeze this '{typeName}' instance.",
																	TriviaList()
																), XmlTextNewLine(
																	TriviaList(),
																	"\n",
																	"\n",
																	TriviaList()
																), XmlTextLiteral(
																	TriviaList(
																		DocumentationCommentExterior("\t///")
																	),
																	" ",
																	" ",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("summary")
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("summary")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														), XmlTextLiteral(
															TriviaList(
																DocumentationCommentExterior("\t///")
															),
															" ",
															" ",
															TriviaList()
														)
													)
												),
											XmlExampleElement(
												SingletonList<XmlNodeSyntax>(
													XmlText()
														.WithTextTokens(
															TokenList(
																XmlTextLiteral(
																	TriviaList(),
																	"'true' when unfreezing was successful, 'false' when the instance was not frozen.",
																	"'true' when unfreezing was successful, 'false' when the instance was not frozen.",
																	TriviaList()
																)
															)
														)
												)
											).WithStartTag(
												XmlElementStartTag(
													XmlName(
														Identifier("returns")
													)
												)
											).WithEndTag(
												XmlElementEndTag(
													XmlName(
														Identifier("returns")
													)
												)
											),
											XmlText()
												.WithTextTokens(
													TokenList(
														XmlTextNewLine(
															TriviaList(),
															"\n",
															"\n",
															TriviaList()
														)
													)
												)
										}
									)
								)
							)
						),
						SyntaxKind.PublicKeyword,
						TriviaList()
					)
				)
			).WithBody(
				Block(
					IfStatement(
						PrefixUnaryExpression(
							SyntaxKind.LogicalNotExpression,
							IdentifierName("IsFrozen")
						),
						Block(
							SingletonList<StatementSyntax>(
								ReturnStatement(
									LiteralExpression(
										SyntaxKind.FalseLiteralExpression
									)
								)
							)
						)
					),
					ReturnStatement(
						PrefixUnaryExpression(
							SyntaxKind.LogicalNotExpression,
							ParenthesizedExpression(
								AssignmentExpression(
									SyntaxKind.SimpleAssignmentExpression,
									IdentifierName("IsFrozen"),
									LiteralExpression(
										SyntaxKind.FalseLiteralExpression
									)
								)
							)
						)
					)
				)
			)
		};
	}
}
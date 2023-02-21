using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Lombok.NET.Analyzers;
using Lombok.NET.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET;

/// <summary>
/// Generates a static class representation of the appsettings.json file
/// </summary>
[Generator]
public class AppSettingsGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG
        SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		var appSettings = context.SyntaxProvider.ForAttributeWithMetadataName(typeof(AppSettingsAttribute).FullName, IsCandidate, Transform)
			.Combine(context.AdditionalTextsProvider.Where(static x => x.Path.EndsWith("appsettings.json")).Collect())
			.Where(static x => !x.Right.IsEmpty)
			.Select(static (x, token) => GenerateAppSettingsClass(x.Left, x.Right[0], token));
		context.AddSources(appSettings);
	}

	private static NameSyntax Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		return ((BaseNamespaceDeclarationSyntax)context.TargetNode).Name;
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken _)
	{
		return node is BaseNamespaceDeclarationSyntax;
	}

	private static GeneratorResult GenerateAppSettingsClass(NameSyntax @namespace, AdditionalText additionalText, CancellationToken cancellationToken)
	{
		var source = additionalText.GetText(cancellationToken);
		if (source is null)
		{
			return GeneratorResult.Empty;
		}

		var text = source.ToString();
		JsonNode model;
		try
		{
			model = JsonNode.Parse(text)!;
		}
		catch (JsonException)
		{
			return new GeneratorResult(Diagnostic.Create(DiagnosticDescriptors.InvalidJson, null));
		}

		var cls = GetClass("AppSettings", model);
		cancellationToken.ThrowIfCancellationRequested();
		var sourceText = CompilationUnit()
			.WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					FileScopedNamespaceDeclaration(@namespace)
						.WithNamespaceKeyword(
						Token(
							TriviaList(
								Trivia(
									PragmaWarningDirectiveTrivia(
											Token(SyntaxKind.DisableKeyword),
											true
										)
										.WithErrorCodes(
											SingletonSeparatedList<ExpressionSyntax>(
												IdentifierName("CS1591")
											)
										)
								)
							),
							SyntaxKind.NamespaceKeyword,
							TriviaList()
						)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(cls)
					)
				)
			).WithEndOfFileToken(
				Token(
					TriviaList(
						Trivia(
							PragmaWarningDirectiveTrivia(
									Token(SyntaxKind.RestoreKeyword),
									true
								)
								.WithErrorCodes(
									SingletonSeparatedList<ExpressionSyntax>(
										IdentifierName("CS1591")
									)
								)
						)
					),
					SyntaxKind.EndOfFileToken,
					TriviaList()
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);

		return new GeneratorResult("AppSettings", sourceText);
	}

	private static ClassDeclarationSyntax GetClass(string className, JsonNode node)
	{
		var members = new List<MemberDeclarationSyntax>();
		foreach (var kv in node.AsObject())
		{
			if (kv.Value is JsonObject obj)
			{
				var nestedClass = GetClass(kv.Key, obj);
				members.Add(nestedClass);
			}
			else if (kv.Value is JsonValue value)
			{
				var element = value.GetValue<JsonElement>();
				var (literal, type) = element.ValueKind switch
				{
					JsonValueKind.String => (LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(element.GetString()!)),
						PredefinedType(Token(SyntaxKind.StringKeyword))),
					JsonValueKind.Number => (LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(element.GetInt32())),
						PredefinedType(Token(SyntaxKind.StringKeyword))),
					JsonValueKind.False => (LiteralExpression(SyntaxKind.FalseLiteralExpression), PredefinedType(Token(SyntaxKind.BoolKeyword))),
					JsonValueKind.True => (LiteralExpression(SyntaxKind.TrueLiteralExpression), PredefinedType(Token(SyntaxKind.BoolKeyword))),
					_ => throw new Exception("No type found.")
				};
				var constant = FieldDeclaration(
					VariableDeclaration(type).WithVariables(
						SingletonSeparatedList(
							VariableDeclarator(
								Identifier(kv.Key)
							).WithInitializer(
								EqualsValueClause(literal)
							)
						)
					)
				).WithModifiers(
					TokenList(
						Token(SyntaxKind.PublicKeyword),
						Token(SyntaxKind.ConstKeyword))
				);
				members.Add(constant);
			}
		}

		return ClassDeclaration(className)
			.WithModifiers(
				TokenList(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.StaticKeyword)
				)
			).WithMembers(List(members));
	}
}
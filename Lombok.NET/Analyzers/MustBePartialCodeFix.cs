using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.Analyzers;

/// <summary>
/// Code fix for types which need to be partial. Simply adds the partial modifier to the type declaration.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class MustBePartialCodeFix : CodeFixProvider
{
	private static readonly string TriggeringDiagnosticId = DiagnosticDescriptors.TypeMustBePartial.Id;
		
	/// <summary>
	/// Registers the code fix.
	/// </summary>
	/// <param name="context">The context of registration.</param>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		if (root?.FindNode(context.Span) is not TypeDeclarationSyntax typeDeclaration)
		{
			return;
		}

		var newDeclaration = typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
		var newRoot = root.ReplaceNode(typeDeclaration, newDeclaration);

		var codeFix = CodeAction.Create(
			$"Make '{typeDeclaration.Identifier.Text}' partial",
			token => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)),
			TriggeringDiagnosticId
		);
		
		context.RegisterCodeFix(codeFix, context.Diagnostics);
	}
		
	/// <summary>
	/// Supplies a fix all provider.
	/// </summary>
	/// <returns>A batch fixer.</returns>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <summary>
	/// Diagnostics which can be fixed by this analyzer.
	/// </summary>
	public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(TriggeringDiagnosticId);
}
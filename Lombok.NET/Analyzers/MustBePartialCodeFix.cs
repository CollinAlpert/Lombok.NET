using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.Analyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MustBePartialCodeFix : CodeFixProvider
	{
		private static readonly string TriggeringDiagnosticId = DiagnosticDescriptors.TypeMustBePartial.Id;
		
		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
			var node = root?.FindNode(context.Span);

			if (!(node is TypeDeclarationSyntax typeDeclaration))
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
		
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(TriggeringDiagnosticId);
	}
}
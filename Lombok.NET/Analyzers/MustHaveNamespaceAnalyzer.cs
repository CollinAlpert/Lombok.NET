using System.Collections.Immutable;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Lombok.NET.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MustHaveNamespaceAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.TypeMustHaveNamespace);

		public override void Initialize(AnalysisContext context)
		{
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(CheckType, SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.StructDeclaration);
		}

		private static void CheckType(SyntaxNodeAnalysisContext context)
		{
			var type = (TypeDeclarationSyntax)context.Node;
			var symbol = context.SemanticModel.GetDeclaredSymbol(type);
			if (symbol?.RequiresNamespace() is true && symbol.ContainingNamespace.IsGlobalNamespace)
			{
				var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, type.Identifier.GetLocation(), type.Identifier.Text);
				context.ReportDiagnostic(diagnostic);	
			}
		}
	}
}
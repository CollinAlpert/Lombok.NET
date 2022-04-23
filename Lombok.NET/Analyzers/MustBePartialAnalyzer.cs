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
	/// <summary>
	/// Analyzer which makes sure that types for which code should be generated are partial.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MustBePartialAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Diagnostics supported/raised by this analyzer.
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.TypeMustBePartial);

		/// <summary>
		/// Initializes the analyzer.
		/// </summary>
		/// <param name="context">The context of analysis.</param>
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
			var partialAttributeType = context.Compilation.GetTypeByMetadataName(typeof(PartialAttribute).FullName);
			if (partialAttributeType is null)
			{
				return;
			}

			var type = (TypeDeclarationSyntax)context.Node;
			var symbol = context.SemanticModel.GetDeclaredSymbol(type);
			if (symbol?.RequiresPartialModifier(partialAttributeType) is true && !type.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, type.Identifier.GetLocation(), type.Identifier.Text);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
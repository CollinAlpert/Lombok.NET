using System.Collections.Immutable;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lombok.NET.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MustBePartialAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.TypeMustBePartial);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSymbolAction(CheckType, SymbolKind.NamedType);
		}

		private static void CheckType(SymbolAnalysisContext context)
		{
			var partialAttributeType = context.Compilation.GetTypeByMetadataName(typeof(PartialAttribute).FullName ?? "Lombok.NET.PartialAttribute");
			if (partialAttributeType is null)
			{
				return;
			}

			var type = (INamedTypeSymbol)context.Symbol;
			if (!type.RequiresPartialModifier(partialAttributeType))
			{
				return;
			}

			if (type.TryGetDeclarationMissingPartialModifier(out var typeDeclaration))
			{
				var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, typeDeclaration.Identifier.GetLocation(), type.Name);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
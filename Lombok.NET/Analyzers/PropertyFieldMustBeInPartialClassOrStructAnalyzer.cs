using System;
using System.Collections.Immutable;
using System.Linq;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lombok.NET.Analyzers
{
	/// <summary>
	/// Analyzer which makes sure that methods marked with the [Property] attribute are withing a partial class or partial struct.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PropertyFieldMustBeInPartialClassOrStructAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Initializes the analyzer.
		/// </summary>
		/// <param name="context">The context of analysis.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(CheckField, SyntaxKind.FieldDeclaration);
		}

		private static void CheckField(SyntaxNodeAnalysisContext context)
		{
			SymbolCache.PropertyAttributeSymbol = context.Compilation.GetSymbolByType<PropertyAttribute>();
			
			var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
			if (context.ContainingSymbol?.HasAttribute(SymbolCache.PropertyAttributeSymbol) == true)
			{
				TypeDeclarationSyntax parentType;
				if (!fieldDeclaration.Parent.IsKind(SyntaxKind.ClassDeclaration) && !fieldDeclaration.Parent.IsKind(SyntaxKind.StructDeclaration))
				{
					var fieldName = string.Join(", ", fieldDeclaration.Declaration.Variables.Select(v => v.Identifier.Text));
					var diagnostic = Diagnostic.Create(DiagnosticDescriptors.PropertyFieldMustBeInClassOrStruct, fieldDeclaration.Declaration.GetLocation(), fieldName);
					context.ReportDiagnostic(diagnostic);
				}
				else if (!(parentType = (TypeDeclarationSyntax)fieldDeclaration.Parent).Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, parentType.Identifier.GetLocation(), parentType.Identifier.Text);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		/// <summary>
		/// Diagnostics supported/raised by this analyzer.
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(DiagnosticDescriptors.PropertyFieldMustBeInClassOrStruct, DiagnosticDescriptors.TypeMustBePartial);
	}
}
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
	public class AsyncMethodMustBeInPartialClassOrStructAnalyzer : DiagnosticAnalyzer
	{
		public override void Initialize(AnalysisContext context)
		{
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(CheckMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
		}

		private static void CheckMethod(SyntaxNodeAnalysisContext context)
		{
			SymbolCache.AsyncAttributeSymbol ??= context.Compilation.GetSymbolByType<AsyncAttribute>();

			SyntaxToken? GetIdentifier()
			{
				return context.Node switch
				{
					MethodDeclarationSyntax method
						when method.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncAttributeSymbol!) => method.Identifier,
					LocalFunctionStatementSyntax localFunction
						when localFunction.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncAttributeSymbol!) => localFunction.Identifier,
					_ => null
				};
			}

			var identifier = GetIdentifier();
			if (identifier.HasValue)
			{
				TypeDeclarationSyntax parentType;
				if (!context.Node.Parent.IsKind(SyntaxKind.ClassDeclaration) && !context.Node.Parent.IsKind(SyntaxKind.StructDeclaration))
				{
					var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AsyncMethodMustBeInClassOrStruct, identifier.Value.GetLocation(), identifier.Value.Text);
					context.ReportDiagnostic(diagnostic);
				}
				else if (!(parentType = (TypeDeclarationSyntax)context.Node.Parent).Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					var diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, parentType.Identifier.GetLocation(), parentType.Identifier.Text);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
			ImmutableArray.Create(DiagnosticDescriptors.AsyncMethodMustBeInClassOrStruct, DiagnosticDescriptors.TypeMustBePartial);
	}
}
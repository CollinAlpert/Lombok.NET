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
			SyntaxToken? GetIdentifier()
			{
				var asyncAttribute = typeof(AsyncAttribute).FullName;

				return context.Node switch
				{
					MethodDeclarationSyntax method 
						when method.AttributeLists.ContainsAttribute(context.SemanticModel, asyncAttribute) => method.Identifier,
					LocalFunctionStatementSyntax localFunction 
						when localFunction.AttributeLists.ContainsAttribute(context.SemanticModel, asyncAttribute) => localFunction.Identifier,
					_ => null
				};
			}

			var identifier = GetIdentifier();
			if (identifier.HasValue)
			{
				TypeDeclarationSyntax parentType;
				if (context.Node.Parent is not ClassDeclarationSyntax && context.Node.Parent is not StructDeclarationSyntax)
				{
					var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AsyncMethodMustBeInClassOrStruct, identifier.Value.GetLocation(), identifier.Value.Text);
					context.ReportDiagnostic(diagnostic);
				} else if (!(parentType = (TypeDeclarationSyntax)context.Node.Parent).Modifiers.Any(SyntaxKind.PartialKeyword))
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
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

namespace Lombok.NET.Analyzers;

/// <summary>
/// Analyzer which makes sure that methods marked with the [Async] attribute are withing a partial class or partial struct.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodMustBeInPartialClassOrStructAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	/// Initializes the analyzer.
	/// </summary>
	/// <param name="context">The context of analysis.</param>
	public override void Initialize(AnalysisContext context)
	{
#if DEBUG
		SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(CheckMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
	}

	private static void CheckMethod(SyntaxNodeAnalysisContext context)
	{
		var asyncAttributeSymbol = context.Compilation.GetSymbolByType<AsyncAttribute>();

		SyntaxToken? GetIdentifier()
		{
			return context.Node switch
			{
				MethodDeclarationSyntax method
					when ContainsAttribute(method, context.SemanticModel, asyncAttributeSymbol) => method.Identifier,
				LocalFunctionStatementSyntax localFunction
					when ContainsAttribute(localFunction, context.SemanticModel, asyncAttributeSymbol) => localFunction.Identifier,
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

	/// <summary>
	/// Diagnostics supported/raised by this analyzer.
	/// </summary>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(DiagnosticDescriptors.AsyncMethodMustBeInClassOrStruct, DiagnosticDescriptors.TypeMustBePartial);
	
	/// <summary>
	/// Checks if a node is marked with a specific attribute.
	/// </summary>
	/// <param name="node">The node to check.</param>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="attributeSymbol">The attributes symbol.</param>
	/// <returns>True, if the node is marked with the attribute.</returns>
	private static bool ContainsAttribute(SyntaxNode node, SemanticModel semanticModel, INamedTypeSymbol attributeSymbol)
	{
		var symbol = semanticModel.GetDeclaredSymbol(node);

		return symbol is not null && symbol.HasAttribute(attributeSymbol);
	}
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if DEBUG
using System.Diagnostics;
#endif

namespace SyntaxReceiverGenerators
{
	/// <summary>
	/// Source which detects attribute classes (i.e. classes which inherit from System.Attribute).
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public class AttributeSyntaxReceiverGenerator : IIncrementalGenerator
	{
		private static string CreateSyntaxReceiverCode(string attributeName, string fullAttributeName)
		{
			return $@"
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET {{
    internal class {attributeName}SyntaxReceiver : BaseAttributeSyntaxReceiver
    {{
        protected override string FullAttributeName {{ get; }} = ""{fullAttributeName}"";
    }}
}}
";
		}

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			
			var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, GetSyntaxReceiverCode).Where(s => s != null);
			
			context.RegisterSourceOutput(classDeclarations, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
		}

		private static bool IsCandidate(SyntaxNode node, CancellationToken _)
		{
			return node is ClassDeclarationSyntax { Parent: BaseNamespaceDeclarationSyntax ns } && ns.Name.ToString() == "Lombok.NET";
		}

		private static string? GetSyntaxReceiverCode(GeneratorSyntaxContext context, CancellationToken _)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			if (classDeclaration.TryGetDescendantNode<BaseTypeSyntax>(out var baseType) &&
			    context.SemanticModel.GetTypeInfo(baseType.Type).Type?.ToDisplayString() == "System.Attribute")
			{
				var fullClassName = context.SemanticModel.GetDeclaredSymbol(classDeclaration)!.ToDisplayString();
				var attributeName = classDeclaration.Identifier.Text.Remove(classDeclaration.Identifier.Text.Length - "Attribute".Length);
				return CreateSyntaxReceiverCode(attributeName, fullClassName);
			}

			return null;
		}
	}

	internal static class Extensions
	{
		public static bool TryGetDescendantNode<T>(this SyntaxNode node, [NotNullWhen(true)] out T? descendantNode)
			where T : SyntaxNode
		{
			descendantNode = node.DescendantNodes().OfType<T>().FirstOrDefault();

			return descendantNode != null;
		}
	}
}
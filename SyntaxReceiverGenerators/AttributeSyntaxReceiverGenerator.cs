using System;
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
namespace Lombok.NET {{
    internal class {attributeName}SyntaxReceiver : BaseAttributeSyntaxReceiver
    {{
        protected override string FullAttributeName {{ get; }} = ""{fullAttributeName}"";
    }}
}}";
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
			return node is ClassDeclarationSyntax cls && cls.Identifier.Text.EndsWith("Attribute");
		}

		private static string? GetSyntaxReceiverCode(GeneratorSyntaxContext context, CancellationToken _)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
			if (classSymbol is null
			    || classSymbol.ContainingNamespace.ToDisplayString() != "Lombok.NET"
			    || !classDeclaration.TryGetDescendantNode<BaseTypeSyntax>(out var baseType)
			    || context.SemanticModel.GetTypeInfo(baseType!.Type).Type?.ToDisplayString() != "System.Attribute"
			    || !Enum.TryParse(classSymbol.GetAttributes().FirstOrDefault()?.ConstructorArguments.First().Value?.ToString(), out AttributeTargets attribute)
			    || !attribute.HasFlag(AttributeTargets.Class) && !attribute.HasFlag(AttributeTargets.Interface))
			{
				return null;
			}

			var attributeName = classDeclaration.Identifier.Text.Remove(classDeclaration.Identifier.Text.Length - "Attribute".Length);

			return CreateSyntaxReceiverCode(attributeName, classSymbol.ToDisplayString());
		}
	}

	internal static class Extensions
	{
		public static bool TryGetDescendantNode<T>(this SyntaxNode node, out T? descendantNode)
			where T : SyntaxNode
		{
			descendantNode = node.DescendantNodes().OfType<T>().FirstOrDefault();

			return descendantNode != null;
		}
	}
}
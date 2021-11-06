using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace SyntaxReceiverGenerators
{
	[Generator]
	public class AttributeSyntaxReceiverGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new AttributeSyntaxReceiver());
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxContextReceiver is AttributeSyntaxReceiver attributeSyntaxReceiver)
			{
				foreach (var (classDeclaration, fullName) in attributeSyntaxReceiver.Candidates)
				{

					var attributeName = classDeclaration.Identifier.Text.Remove(classDeclaration.Identifier.Text.Length - "Attribute".Length);
					context.AddSource(attributeName, CreateSyntaxReceiverCode(attributeName, fullName));
				}
			}
		}

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

		/// <summary>
		/// Syntax receiver which detects attribute classes (i.e. classes which inherit from System.Attribute).
		/// </summary>
		private class AttributeSyntaxReceiver : ISyntaxContextReceiver
		{
			public readonly List<(ClassDeclarationSyntax Declaration, string FullName)> Candidates = new List<(ClassDeclarationSyntax, string)>();

			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				if (context.Node is ClassDeclarationSyntax classDeclaration
				    && context.Node.TryGetDescendantNode<BaseTypeSyntax>(out var baseType)
				    && context.SemanticModel.GetTypeInfo(baseType.Type).Type?.ToDisplayString() == "System.Attribute")
				{
					var fullClassName = context.SemanticModel.GetDeclaredSymbol(classDeclaration)!.ToDisplayString();
					Candidates.Add((classDeclaration, fullClassName));
				}
			}
		}
	}

	internal static class Extensions
	{
		public static bool TryGetDescendantNode<T>(this SyntaxNode node, [NotNullWhen(true)] out T? descendantNode)
			where T : SyntaxNode
		{
			descendantNode = node.DescendantNodes().FirstOrDefault(n => n is T) as T;

			return descendantNode != null;
		}
	}
}
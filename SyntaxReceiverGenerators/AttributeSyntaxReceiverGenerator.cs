using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SyntaxReceiverGenerators
{
    [Generator]
    public class AttributeSyntaxReceiverGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AttributeSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is AttributeSyntaxReceiver attributeSyntaxReceiver)
            {
                foreach (var classDeclaration in attributeSyntaxReceiver.Candidates)
                {
                    var attributeName = classDeclaration.Identifier.Text.Remove(classDeclaration.Identifier.Text.Length - "Attribute".Length);
                    context.AddSource(attributeName, CreateSyntaxReceiverCode(attributeName));
                }
            }
        }

        private static string CreateSyntaxReceiverCode(string attributeName)
        {
            return $@"
namespace AttributeSourceGenerators {{
    internal class {attributeName}SyntaxReceiver : BaseAttributeSyntaxReceiver
    {{
        protected override string AttributeName {{ get; }} = ""{attributeName}"";
    }}
}}
";
        }
        
        private class AttributeSyntaxReceiver : ISyntaxReceiver
        {
            public readonly List<ClassDeclarationSyntax> Candidates = new List<ClassDeclarationSyntax>();
            
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if(syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.BaseList?.Types.Any(a => a.Type.ToString() == "Attribute") == true)
                {
                    Candidates.Add(classDeclaration);
                }                 
            }
        }
    }
}
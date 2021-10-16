using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET
{
    public abstract class BaseAttributeSyntaxReceiver : ISyntaxReceiver
    {
        protected abstract string AttributeName { get; }
            
        public readonly List<TypeDeclarationSyntax> Candidates = new List<TypeDeclarationSyntax>();
            
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax typeDeclaration && typeDeclaration.AttributeLists.Any(l => l.Attributes.Any(a => a.Name.ToString() == AttributeName)))
            {
                Candidates.Add(typeDeclaration);
            }
        }
    }
}
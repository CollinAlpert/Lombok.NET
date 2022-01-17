using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET
{
    /// <summary>
    /// Base class for syntax receivers that discover types which have certain attributes. 
    /// </summary>
    public abstract class BaseAttributeSyntaxReceiver : ISyntaxContextReceiver
    {
        protected abstract string FullAttributeName { get; }
            
        public readonly List<TypeDeclarationSyntax> Candidates = new List<TypeDeclarationSyntax>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclaration
                && typeDeclaration.AttributeLists.SelectMany(l => l.Attributes).Any(a => context.SemanticModel.GetTypeInfo(a).Type?.ToDisplayString() == FullAttributeName))
            {
                Candidates.Add(typeDeclaration);
            }
        }
    }
}
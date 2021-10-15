using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AttributeSourceGenerators
{
    [Generator]
    public class NoArgsConstructorGenerator : BaseConstructorGenerator
    {
        protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new NoArgsConstructorSyntaxReceiver();
        
        protected override (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax _)
        {
            return (SyntaxFactory.ParameterList(), SyntaxFactory.Block());
        }
    }
}
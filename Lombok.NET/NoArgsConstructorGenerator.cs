using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET
{
    /// <summary>
    /// Generator which generates an empty constructor. No parameters, no body.
    /// </summary>
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
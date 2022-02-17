using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.ConstructorGenerators
{
	/// <summary>
	/// Generator which generates an empty constructor. No parameters, no body.
	/// </summary>
	[Generator]
	public class NoArgsConstructorGenerator : BaseConstructorGenerator
	{
		protected override (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax _)
		{
			return (SyntaxFactory.ParameterList(), SyntaxFactory.Block());
		}

		protected override string AttributeName { get; } = "NoArgsConstructor";
		
		protected override INamedTypeSymbol GetAttributeSymbol(SemanticModel model)
		{
			return SymbolCache.NoArgsConstructorAttributeSymbol ??= model.Compilation.GetSymbolByType<NoArgsConstructorAttribute>();
		}
	}
}
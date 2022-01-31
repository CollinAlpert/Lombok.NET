using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.Extensions
{
	public static class SymbolExtensions
	{
		public static bool RequiresPartialModifier(this ISymbol symbol, INamedTypeSymbol partialAttributeType)
		{
			return symbol.GetAttributes()
				.Where(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Lombok.NET")
				.SelectMany(a => a.AttributeClass?.GetAttributes() ?? Enumerable.Empty<AttributeData>())
				.Any(a => partialAttributeType.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
		}

		public static bool TryGetDeclarationMissingPartialModifier(this ISymbol symbol, out TypeDeclarationSyntax typeDeclaration)
		{
			return (typeDeclaration = symbol.DeclaringSyntaxReferences
				.Select(s => s.GetSyntax())
				.OfType<TypeDeclarationSyntax>()
				.FirstOrDefault(t => !t.Modifiers.Any(SyntaxKind.PartialKeyword))) != null;
		}
	}
}
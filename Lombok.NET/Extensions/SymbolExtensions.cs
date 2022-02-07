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

		public static bool RequiresNamespace(this ISymbol symbol)
		{
			return symbol.GetAttributes().Any(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Lombok.NET");
		}

		public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
		{
			return symbol.GetAttributes().Any(a => attribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
		}
	}
}
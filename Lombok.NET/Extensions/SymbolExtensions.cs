using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Lombok.NET.Extensions;

/// <summary>
/// Extension methods for symbol-related operations.
/// </summary>
internal static class SymbolExtensions
{
	/// <summary>
	/// Checks if a symbol represents a type which has an attribute which requires the type to be partial.
	/// </summary>
	/// <param name="symbol">The symbol representing a type.</param>
	/// <param name="partialAttributeType">The partial attribute to check against.</param>
	/// <returns>True, if the type needs to be partial.</returns>
	public static bool RequiresPartialModifier(this ISymbol symbol, INamedTypeSymbol partialAttributeType)
	{
		return symbol.GetAttributes()
			.Where(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Lombok.NET")
			.SelectMany(a => a.AttributeClass?.GetAttributes() ?? Enumerable.Empty<AttributeData>())
			.Any(a => partialAttributeType.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
	}

	/// <summary>
	/// Checks if a symbol represents a type which requires a namespace.
	/// </summary>
	/// <param name="symbol">The symbol to check.</param>
	/// <returns>True, if the type requires to be within a namespace.</returns>
	public static bool RequiresNamespace(this ISymbol symbol)
	{
		return symbol.GetAttributes().Any(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Lombok.NET");
	}

	/// <summary>
	/// Checks if a symbol represents a type which is marked with the specified attribute.
	/// </summary>
	/// <param name="symbol">The symbol to check.</param>
	/// <param name="attribute">The attribute to look for.</param>
	/// <returns>True, if the type is marked with the attribute.</returns>
	public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
	{
		return symbol.GetAttributes().Any(a => attribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
	}

	/// <summary>
	/// Gets the symbol for a type.
	/// </summary>
	/// <param name="compilation">The compilation to retrieve the symbol from.</param>
	/// <typeparam name="T">The type for retrieve the symbol for.</typeparam>
	/// <returns>The type's symbol.</returns>
	/// <exception cref="TypeAccessException">If the type cannot be found in the specified compilation.</exception>
	public static INamedTypeSymbol GetSymbolByType<T>(this Compilation compilation)
	{
		var name = typeof(T).FullName;

		return compilation.GetTypeByMetadataName(name) ?? throw new TypeAccessException($"{name} could not be found in compilation.");
	}
}
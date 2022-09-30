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
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Lombok.NET.Extensions;

/// <summary>
/// Extension methods for symbol-related operations.
/// </summary>
internal static class SymbolExtensions
{
	private static readonly IDictionary<AccessTypes, Accessibility> AccessibilitiesByAccessType = new Dictionary<AccessTypes, Accessibility>(4)
	{
		[AccessTypes.Private] = Accessibility.Private,
		[AccessTypes.Protected] = Accessibility.Protected,
		[AccessTypes.Internal] = Accessibility.Internal,
		[AccessTypes.Public] = Accessibility.Public
	};

	/// <summary>
	/// Removes all the members which do not have the desired access modifier.
	/// </summary>
	/// <param name="members">The members to filter</param>
	/// <param name="accessType">The access modifer to look out for.</param>
	/// <typeparam name="T">The type of the members (<code>PropertyDeclarationSyntax</code>/<code>FieldDeclarationSyntax</code>).</typeparam>
	/// <returns>The members which have the desired access modifier.</returns>
	/// <exception cref="ArgumentOutOfRangeException">If an access modifier is supplied which is not supported.</exception>
	public static IEnumerable<T> Where<T>(this IEnumerable<T> members, AccessTypes accessType)
		where T : ISymbol
	{
		var predicateBuilder = PredicateBuilder.False<T>();
		foreach (AccessTypes t in typeof(AccessTypes).GetEnumValues())
		{
			if (accessType.HasFlag(t))
			{
				predicateBuilder = predicateBuilder.Or(m => m.DeclaredAccessibility == AccessibilitiesByAccessType[t]);
			}
		}

		return members.Where(predicateBuilder.Compile());
	}
}
using System;
using System.Collections.Generic;

namespace Lombok.NET.Extensions;

internal static class EnumerableExtensions
{
	public static IEnumerable<TProjection> SelectWhereNotNull<T, TProjection>(this IEnumerable<T> source, Func<T, TProjection?> projection)
	{
		foreach (var element in source)
		{
			var projectedElement = projection(element);
			if (projectedElement is not null)
			{
				yield return projectedElement;
			}
		}
	} 
}
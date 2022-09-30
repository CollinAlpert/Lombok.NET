using System;
using System.Linq.Expressions;

namespace Lombok.NET;

/// <summary>
/// Builds a predicate by chaining multiple conditions.
/// </summary>
internal static class PredicateBuilder
{
	/// <summary>
	/// Returns an expression which is always true.
	/// </summary>
	/// <typeparam name="T">The type this predicate targets.</typeparam>
	/// <returns>An always-true predicate.</returns>
	public static Expression<Func<T, bool>> True<T>()
	{
		return static f => true;
	}

	/// <summary>
	/// Returns an expression which is always false.
	/// </summary>
	/// <typeparam name="T">The type this predicate targets.</typeparam>
	/// <returns>An always-false predicate.</returns>
	public static Expression<Func<T, bool>> False<T>()
	{
		return static f => false;
	}

	/// <summary>
	/// Adds a new condition to the chain and combines it using an OR expression.
	/// </summary>
	/// <param name="expr1">The existing predicate chain.</param>
	/// <param name="expr2">The predicate to add.</param>
	/// <typeparam name="T">The type this predicate targets.</typeparam>
	/// <returns>A new predicate with an additional OR predicate.</returns>
	public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
	{
		var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);

		return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
	}

	/// <summary>
	/// Adds a new condition to the chain and combines it using an AND expression.
	/// </summary>
	/// <param name="expr1">The existing predicate chain.</param>
	/// <param name="expr2">The predicate to add.</param>
	/// <typeparam name="T">The type this predicate targets.</typeparam>
	/// <returns>A new predicate with an additional AND predicate.</returns>
	public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
	{
		var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);

		return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
	}
}
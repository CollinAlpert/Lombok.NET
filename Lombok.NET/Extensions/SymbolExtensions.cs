using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

	public static string GetFullName(this ITypeSymbol typeSymbol)
	{
		string name = typeSymbol.Name;
		if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
		{
			string typeParameters = string.Join(", ", namedType.TypeParameters.Select(tp => tp.Name));
			
			return string.Concat(name, "<", typeParameters, ">");
		}

		return name;
	}

	public static IEnumerable<ISymbol> GetAllMembersIncludingInherited(this ITypeSymbol typeSymbol)
	{
		return typeSymbol
			.GetThisAndBaseTypes()
			.SelectMany(t => t.GetMembers())
			.Concat(typeSymbol.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Distinct(SymbolEqualityComparer.Default);
	}

	public static IEnumerable<ITypeSymbol> GetThisAndBaseTypes(this ITypeSymbol type)
	{
		var current = type;
		while (current != null)
		{
			yield return current;
			current = current.BaseType;
		}
	}

	public static ParameterListSyntax GenerateParameterList(this ImmutableArray<IParameterSymbol> parameters) =>
		ParameterList(SeparatedList(parameters.Select(p =>
			Parameter(Identifier(p.Name))
				.WithType(p.Type.ToTypeSyntax())
				.WithModifiers(TokenList(p.RefKind.GenerateRefKindToken()))
		)));

	public static SyntaxToken GenerateAccessibilityToken(this IMethodSymbol symbol)
	{
		return symbol.DeclaredAccessibility switch
		{
			Accessibility.Public => Token(SyntaxKind.PublicKeyword),
			Accessibility.Protected => Token(SyntaxKind.ProtectedKeyword),
			Accessibility.Private => Token(SyntaxKind.PrivateKeyword),
			_ => Token(SyntaxKind.None)
		};
	}

	public static SyntaxToken GenerateRefKindToken(this RefKind refKind) => refKind switch
	{
		RefKind.Ref => Token(SyntaxKind.RefKeyword),
		RefKind.Out => Token(SyntaxKind.OutKeyword),
		RefKind.In => Token(SyntaxKind.InKeyword),
		_ => Token(SyntaxKind.None)
	};

	public static TypeSyntax ToTypeSyntax(this ITypeSymbol type) =>
		ParseTypeName(type.ToDisplayString()); //TODO Check if this works for generics
}


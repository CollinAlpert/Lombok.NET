using System.Collections.Generic;

namespace Lombok.NET.Extensions;

/// <summary>
/// Extension methods for string-related operations.
/// </summary>
internal static class StringExtensions
{
	// Taken from https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
	private static readonly ISet<string> ReservedKeywords = new HashSet<string>
	{
		"abstract",
		"as",
		"base",
		"bool",
		"break",
		"byte",
		"case",
		"catch",
		"char",
		"checked",
		"class",
		"const",
		"continue",
		"decimal",
		"default",
		"delegate",
		"do",
		"double",
		"else",
		"enum",
		"event",
		"explicit",
		"extern",
		"false",
		"finally",
		"fixed",
		"float",
		"for",
		"foreach",
		"goto",
		"if",
		"implicit",
		"in",
		"int",
		"interface",
		"internal",
		"is",
		"lock",
		"long",
		"namespace",
		"new",
		"null",
		"object",
		"operator",
		"out",
		"override",
		"params",
		"private",
		"protected",
		"public",
		"readonly",
		"ref",
		"return",
		"sbyte",
		"sealed",
		"short",
		"sizeof",
		"stackalloc",
		"static",
		"string",
		"struct",
		"switch",
		"this",
		"throw",
		"true",
		"try",
		"typeof",
		"uint",
		"ulong",
		"unchecked",
		"unsafe",
		"ushort",
		"using",
		"virtual",
		"void",
		"volatile",
		"while"
	};

	/// <summary>
	/// Lowercases the first character of a given string.
	/// </summary>
	/// <param name="s">The string whose first character to lowercase.</param>
	/// <returns>The string with its first character lowercased.</returns>
	public static string? Decapitalize(this string? s)
	{
		if (s is null || char.IsLower(s[0]))
		{
			return s;
		}

		return char.ToLower(s[0]) + s.Substring(1);
	}

	/// <summary>
	/// Uppercases the first character of a given string.
	/// </summary>
	/// <param name="s">The string whose first character to uppercase.</param>
	/// <returns>The string with its first character uppercased.</returns>
	public static string? Capitalize(this string? s)
	{
		if (s is null || char.IsUpper(s[0]))
		{
			return s;
		}

		return char.ToUpper(s[0]) + s.Substring(1);
	}

	/// <summary>
	/// Escapes a reserved keyword which should be used as an identifier.
	/// </summary>
	/// <param name="identifier">The identifier to be used.</param>
	/// <returns>A valid identifier.</returns>
	public static string EscapeReservedKeyword(this string identifier)
	{
		if (ReservedKeywords.Contains(identifier))
		{
			return "@" + identifier;
		}

		return identifier;
	}
		
	/// <summary>
	/// Ensures normal PascalCase for an identifier. (e.g. "_age" becomes "Age").
	/// </summary>
	/// <param name="identifier">The identifier to get the property name for.</param>
	/// <returns>A PascalCase identifier.</returns>
	public static string ToPascalCaseIdentifier(this string identifier)
	{
		if (identifier.StartsWith("_"))
		{
			identifier = identifier.Substring(1);
		}

		return identifier.Capitalize()!;
	}

	/// <summary>
	/// Transforms an identifier to camelCase. (e.g. "_myAge" -> "myAge", "MyAge" -> "myAge").
	/// </summary>
	/// <param name="identifier">The identifier to transform.</param>
	/// <returns>A camelCase identifier.</returns>
	public static string ToCamelCaseIdentifier(this string identifier)
	{
		if (identifier.StartsWith("_"))
		{
			return identifier.Substring(1).Decapitalize()!;
		}

		return identifier.Decapitalize()!;
	}
}
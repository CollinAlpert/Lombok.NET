using System.Collections.Generic;

namespace Lombok.NET.Extensions
{
	public static class StringExtensions
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
		public static string Decapitalize(this string s)
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
		public static string Capitalize(this string s)
		{
			if (s is null || char.IsUpper(s[0]))
			{
				return s;
			}

			return char.ToUpper(s[0]) + s.Substring(1);
		}

		public static string EscapeReservedKeyword(this string s)
		{
			if(ReservedKeywords.Contains(s))
			{
				return "@" + s;
			}

			return s;
		}
	}
}
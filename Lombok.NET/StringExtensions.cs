using System.Diagnostics.CodeAnalysis;

namespace Lombok.NET
{
	public static class StringExtensions
	{
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
	}
}
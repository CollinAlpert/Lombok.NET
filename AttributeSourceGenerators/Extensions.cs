using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AttributeSourceGenerators
{
	public static class Extensions
	{
		#region String

		[return: NotNullIfNotNull("s")]
		public static string? Decapitalize(this string? s)
		{
			if (s is null || char.IsLower(s[0]))
			{
				return s;
			}

			return char.ToLower(s[0]) + s[1..];
		}

		#endregion

		#region SyntaxNode

		public static string? GetNamespace(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent != null)
			{
				if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
				{
					return namespaceDeclaration.Name.ToString();
				}

				parent = parent.Parent;
			}

			return null;
		}

		#endregion
	}
}
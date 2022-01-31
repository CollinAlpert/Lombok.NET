using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.Extensions
{
	internal static class Extensions
	{
		private static readonly IDictionary<AccessTypes, SyntaxKind> SyntaxKindsByAccessType = new Dictionary<AccessTypes, SyntaxKind>(4)
		{
			[AccessTypes.Private] = SyntaxKind.PrivateKeyword,
			[AccessTypes.Protected] = SyntaxKind.ProtectedKeyword,
			[AccessTypes.Internal] = SyntaxKind.InternalKeyword,
			[AccessTypes.Public] = SyntaxKind.PublicKeyword
		};

		/// <summary>
		/// Traverses a syntax node upwards until it reaches a <code>NamespaceDeclarationSyntax</code>.
		/// </summary>
		/// <param name="node">The syntax node to traverse.</param>
		/// <returns>The namespace this syntax node is in. <code>null</code> if a namespace cannot be found.</returns>
		public static string GetNamespace(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent != null)
			{
				if (parent is BaseNamespaceDeclarationSyntax namespaceDeclaration)
				{
					return namespaceDeclaration.Name.ToString();
				}

				parent = parent.Parent;
			}

			return null;
		}

		public static T? GetAttributeArgument<T>(this MemberDeclarationSyntax memberDeclaration, string attributeName)
			where T : struct, Enum
		{
			var attributes = memberDeclaration.AttributeLists.SelectMany(l => l.Attributes);
			var attribute = attributes.FirstOrDefault(a => a.Name.ToString() == attributeName);
			if (attribute is null)
			{
				throw new Exception($"Attribute '{attributeName}' could not be found on {memberDeclaration}");
			}

			var argumentList = attribute.ArgumentList;
			if (argumentList is null || argumentList.Arguments.Count == 0)
			{
				return null;
			}

			var typeName = typeof(T).Name;

			var argument = argumentList.Arguments.FirstOrDefault(a =>
			{
				if (a.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					return memberAccess.Expression.ToString() == typeName;
				}

				if (a.Expression is BinaryExpressionSyntax binary)
				{
					return binary.GetOperandType()?.Name == typeName;
				}

				return false;
			});

			switch (argument?.Expression)
			{
				case MemberAccessExpressionSyntax m when Enum.TryParse(m.Name.Identifier.Text, out T value):
					return value;
				case BinaryExpressionSyntax b:
					return b.GetMembers().Select(m => (T)Enum.Parse(typeof(T), m.Name.Identifier.Text)).Aggregate(default, GenericHelper<T>.Or);
				default:
					return null;
			}
		}

		private static Type GetOperandType(this BinaryExpressionSyntax b)
		{
			if (!(b.Right is MemberAccessExpressionSyntax memberAccess))
			{
				return null;
			}

			return Type.GetType($"Lombok.NET.{memberAccess.Expression.ToString()}");
		}

		private static List<MemberAccessExpressionSyntax> GetMembers(this BinaryExpressionSyntax b, List<MemberAccessExpressionSyntax> l = null)
		{
			l = l ?? new List<MemberAccessExpressionSyntax>();
			switch (b.Right)
			{
				case MemberAccessExpressionSyntax m:
					l.Add(m);

					break;
				case BinaryExpressionSyntax b2:
					return b2.GetMembers(l);
			}

			switch (b.Left)
			{
				case MemberAccessExpressionSyntax m2:
					l.Add(m2);

					break;
				case BinaryExpressionSyntax b3:
					return b3.GetMembers(l);
			}

			return l;
		}

		public static SyntaxKind GetAccessibilityModifier(this BaseTypeDeclarationSyntax typeDeclaration)
		{
			if (typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return SyntaxKind.PublicKeyword;
			}

			return SyntaxKind.InternalKeyword;
		}

		public static void EnsurePartial(this TypeDeclarationSyntax typeDeclaration, string messageOnFailure = "'{0}' must be partial and cannot be a nested type.")
		{
			if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) || typeDeclaration.Parent is TypeDeclarationSyntax)
			{
				throw new NotSupportedException(string.Format(messageOnFailure, typeDeclaration.Identifier.Text));
			}
		}

		public static void EnsureNamespace(this BaseTypeDeclarationSyntax typeDeclaration, out string @namespace)
		{
			@namespace = typeDeclaration.GetNamespace();
			if (@namespace is null)
			{
				throw new Exception($"Namespace could not be found for {typeDeclaration.Identifier.Text}.");
			}
		}

		public static bool HasAttribute(this MemberDeclarationSyntax member, SemanticModel semanticModel, string fullAttributeName)
		{
			bool AttributeMatches(AttributeSyntax attribute)
			{
				var typeInfo = semanticModel.GetTypeInfo(attribute).Type;
				if (typeInfo is null)
				{
					return false;
				}

				return $"{typeInfo.ContainingAssembly.Name}.{typeInfo.Name}" == fullAttributeName;
			}
			
			return member.AttributeLists.SelectMany(l => l.Attributes).Any(AttributeMatches);
		}

		public static ClassDeclarationSyntax CreateNewPartialType(this ClassDeclarationSyntax classDeclaration)
		{
			return ClassDeclaration(classDeclaration.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(classDeclaration.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				);
		}

		public static StructDeclarationSyntax CreateNewPartialType(this StructDeclarationSyntax structDeclaration)
		{
			return StructDeclaration(structDeclaration.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(structDeclaration.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				);
		}

		public static InterfaceDeclarationSyntax CreateNewPartialType(this InterfaceDeclarationSyntax interfaceDeclaration)
		{
			return InterfaceDeclaration(interfaceDeclaration.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(interfaceDeclaration.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				);
		}

		public static bool IsVoid(this TypeSyntax typeSyntax)
		{
			return typeSyntax is PredefinedTypeSyntax t && t.Keyword.IsKind(SyntaxKind.VoidKeyword);
		}

		/// <summary>
		/// Removes all the members which do not have the desired access modifier.
		/// </summary>
		/// <param name="members">The members to filter</param>
		/// <param name="accessType">The access modifer to look out for.</param>
		/// <typeparam name="T">The type of the members (<code>PropertyDeclarationSyntax</code>/<code>FieldDeclarationSyntax</code>).</typeparam>
		/// <returns>The members which have the desired access modifier.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If an access modifier is supplied which is not supported.</exception>
		public static IEnumerable<T> Where<T>(this IEnumerable<T> members, AccessTypes accessType)
			where T : MemberDeclarationSyntax
		{
			var predicateBuilder = PredicateBuilder.False<T>();
			foreach (AccessTypes t in typeof(AccessTypes).GetEnumValues())
			{
				if (accessType.HasFlag(t))
				{
					predicateBuilder = predicateBuilder.Or(m => m.Modifiers.Any(SyntaxKindsByAccessType[t]));
				}
			}

			return members.Where(predicateBuilder.Compile());
		}

		private static class GenericHelper<T>
			where T : Enum
		{
			public static readonly Func<T, T, T> Or = BinaryCombine();

			private static Func<T, T, T> BinaryCombine()
			{
				Type underlyingType = Enum.GetUnderlyingType(typeof(T));

				var currentParameter = Expression.Parameter(typeof(T), "current");
				var nextParameter = Expression.Parameter(typeof(T), "next");

				return Expression.Lambda<Func<T, T, T>>(
					Expression.Convert(
						Expression.Or(
							Expression.Convert(currentParameter, underlyingType),
							Expression.Convert(nextParameter, underlyingType)
						),
						typeof(T)
					),
					currentParameter,
					nextParameter
				).Compile();
			}
		}
	}
}
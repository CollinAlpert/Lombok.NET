using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.Extensions
{
	internal static class SyntaxNodeExtensions
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
		public static string? GetNamespace(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent != null)
			{
				if (parent.IsKind(SyntaxKind.NamespaceDeclaration) || parent.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
				{
					return ((BaseNamespaceDeclarationSyntax)parent).Name.ToString();
				}

				parent = parent.Parent;
			}

			return null;
		}

		/// <summary>
		/// Gets the using directives from a SyntaxNode. Traverses the tree upwards until it finds using directives.
		/// </summary>
		/// <param name="node">The staring point.</param>
		/// <returns>A list of using directives.</returns>
		public static SyntaxList<UsingDirectiveSyntax> GetUsings(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent is not null)
			{
				BaseNamespaceDeclarationSyntax @namespace;
				if ((parent.IsKind(SyntaxKind.NamespaceDeclaration) || parent.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
				    && (@namespace = (BaseNamespaceDeclarationSyntax)parent).Usings.Any())
				{
					return @namespace.Usings;
				}

				CompilationUnitSyntax compilationUnit;
				if (parent.IsKind(SyntaxKind.CompilationUnit) && (compilationUnit = (CompilationUnitSyntax)parent).Usings.Any())
				{
					return compilationUnit.Usings;
				}

				parent = parent.Parent;
			}

			return default;
		}

		/// <summary>
		/// Gets the argument value from an attribute by type.
		/// </summary>
		/// <param name="memberDeclaration">The member which is marked with the attribute.</param>
		/// <param name="attributeName">The name of the argument containing the argument.</param>
		/// <typeparam name="T">The type of the argument.</typeparam>
		/// <returns>The argument value.</returns>
		/// <exception cref="Exception">If the argument cannot be found on the member.</exception>
		public static T? GetAttributeArgument<T>(this MemberDeclarationSyntax memberDeclaration, string attributeName)
			where T : struct, Enum
		{
			var attributes = memberDeclaration.AttributeLists.SelectMany(static l => l.Attributes);
			var attribute = attributes.FirstOrDefault(a => a.IsNamed(attributeName));
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
					return b.GetMembers().Select(static m => (T)Enum.Parse(typeof(T), m.Name.Identifier.Text)).Aggregate(default, GenericHelper<T>.Or);
				default:
					return null;
			}
		}

		private static Type? GetOperandType(this BinaryExpressionSyntax b)
		{
			if (b.Right is not MemberAccessExpressionSyntax memberAccess)
			{
				return null;
			}

			return Type.GetType($"Lombok.NET.{memberAccess.Expression.ToString()}");
		}

		private static List<MemberAccessExpressionSyntax> GetMembers(this BinaryExpressionSyntax b, List<MemberAccessExpressionSyntax>? l = null)
		{
			l ??= new List<MemberAccessExpressionSyntax>();
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

		/// <summary>
		/// Gets the accessibility modifier for a type declaration.
		/// </summary>
		/// <param name="typeDeclaration">The type declaration's accessibility modifier to find.</param>
		/// <returns>The types accessibility modifier.</returns>
		public static SyntaxKind GetAccessibilityModifier(this BaseTypeDeclarationSyntax typeDeclaration)
		{
			if (typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				return SyntaxKind.PublicKeyword;
			}

			return SyntaxKind.InternalKeyword;
		}

		/// <summary>
		/// Checks if a node is marked with a specific attribute.
		/// </summary>
		/// <param name="node">The node to check.</param>
		/// <param name="semanticModel">The semantic model.</param>
		/// <param name="attributeSymbol">The attributes symbol.</param>
		/// <returns>True, if the node is marked with the attribute.</returns>
		public static bool ContainsAttribute(this SyntaxNode node, SemanticModel semanticModel, INamedTypeSymbol attributeSymbol)
		{
			var symbol = semanticModel.GetDeclaredSymbol(node);

			return symbol is not null && symbol.HasAttribute(attributeSymbol);
		}

		/// <summary>
		/// Constructs a new partial type from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="typeDeclaration">The type to clone.</param>
		/// <returns>A new partial type with a few of the original types traits.</returns>
		public static TypeDeclarationSyntax CreateNewPartialType(this TypeDeclarationSyntax typeDeclaration)
		{
			if (typeDeclaration.IsKind(SyntaxKind.ClassDeclaration))
			{
				return typeDeclaration.CreateNewPartialClass();
			}
			
			if (typeDeclaration.IsKind(SyntaxKind.StructDeclaration))
			{
				return typeDeclaration.CreateNewPartialStruct();
			}

			if (typeDeclaration.IsKind(SyntaxKind.InterfaceDeclaration))
			{
				return typeDeclaration.CreateNewPartialInterface();
			}

			return typeDeclaration;
		}

		/// <summary>
		/// Constructs a new partial class from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial class with a few of the original types traits.</returns>
		public static ClassDeclarationSyntax CreateNewPartialClass(this TypeDeclarationSyntax type)
		{
			return ClassDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
		}

		/// <summary>
		/// Constructs a new partial struct from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial struct with a few of the original types traits.</returns>
		public static StructDeclarationSyntax CreateNewPartialStruct(this TypeDeclarationSyntax type)
		{
			return StructDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
		}

		/// <summary>
		/// Constructs a new partial interface from the original type's name, accessibility and type arguments.
		/// </summary>
		/// <param name="type">The type to clone.</param>
		/// <returns>A new partial interface with a few of the original types traits.</returns>
		public static InterfaceDeclarationSyntax CreateNewPartialInterface(this TypeDeclarationSyntax type)
		{
			return InterfaceDeclaration(type.Identifier.Text)
				.WithModifiers(
					TokenList(
						Token(type.GetAccessibilityModifier()),
						Token(SyntaxKind.PartialKeyword)
					)
				).WithTypeParameterList(type.TypeParameterList);
		}

		/// <summary>
		/// Checks if a TypeSyntax represents void.
		/// </summary>
		/// <param name="typeSyntax">The TypeSyntax to check.</param>
		/// <returns>True, if the type represents void.</returns>
		public static bool IsVoid(this TypeSyntax typeSyntax)
		{
			return typeSyntax.IsKind(SyntaxKind.PredefinedType) 
			       && ((PredefinedTypeSyntax)typeSyntax).Keyword.IsKind(SyntaxKind.VoidKeyword);
		}

		/// <summary>
		/// Checks if a type is declared as a nested type.
		/// </summary>
		/// <param name="typeDeclaration">The type to check.</param>
		/// <returns>True, if the type is declared within another type.</returns>
		public static bool IsNestedType(this TypeDeclarationSyntax typeDeclaration)
		{
			return typeDeclaration.Parent is TypeDeclarationSyntax;
		}

		/// <summary>
		/// Determines if the type is eligible for code generation.
		/// </summary>
		/// <param name="typeDeclaration">The type to check for.</param>
		/// <param name="namespace">The type's namespace. Will be set in this method.</param>
		/// <param name="diagnostic">A diagnostic to be emitted if the type is not valid.</param>
		/// <returns>True, if code can be generated for this type.</returns>
		public static bool TryValidateType(this TypeDeclarationSyntax typeDeclaration, [NotNullWhen(true)] out string? @namespace, [NotNullWhen(false)] out Diagnostic? diagnostic)
		{
			@namespace = null;
			diagnostic = null;
			if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, typeDeclaration.Identifier.GetLocation());

				return false;
			}

			if (typeDeclaration.IsNestedType())
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustBeNonNested, typeDeclaration.Identifier.GetLocation());

				return false;
			}
			
			@namespace = typeDeclaration.GetNamespace();
			if (@namespace is null)
			{
				diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeMustHaveNamespace, typeDeclaration.Identifier.GetLocation());

				return false;
			}

			return true;
		}

		/// <summary>
		/// Creates a using directive from a string.
		/// </summary>
		/// <param name="usingQualifier">The name of the using directive.</param>
		/// <returns>A using directive.</returns>
		public static UsingDirectiveSyntax CreateUsingDirective(this string usingQualifier)
		{
			var usingParts = usingQualifier.Split('.');

			static NameSyntax GetParts(string[] parts)
			{
				if (parts.Length == 1)
				{
					return IdentifierName(parts[0]);
				}

				var newParts = new string[parts.Length - 1];
				Array.Copy(parts, newParts, newParts.Length);

				return QualifiedName(
					GetParts(newParts),
					IdentifierName(parts[parts.Length - 1])
				);
			}

			return UsingDirective(
				GetParts(usingParts)
			);
		}

		/// <summary>
		/// Checks if the name of an attribute matches a given name.
		/// </summary>
		/// <param name="attribute">The attribute to check.</param>
		/// <param name="name">The name to check against.</param>
		/// <returns>True, if the attribute's name matches.</returns>
		public static bool IsNamed(this AttributeSyntax attribute, string name)
		{
			return attribute.Name is QualifiedNameSyntax qualifiedName && qualifiedName.Right.Identifier.Text == name 
			       || attribute.Name is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == name;
		}

		public static bool TryConvertToMethod(this SyntaxNode node, [NotNullWhen(true)] out MethodDeclarationSyntax? method)
		{
			method = node as MethodDeclarationSyntax;

			return method is not null;
		}

		public static bool TryConvertToEnum(this SyntaxNode node, [NotNullWhen(true)] out EnumDeclarationSyntax? enumDeclaration)
		{
			enumDeclaration = node as EnumDeclarationSyntax;

			return enumDeclaration is not null;
		}

		public static bool TryConvertToClass(this SyntaxNode node, [NotNullWhen(true)] out ClassDeclarationSyntax? classDeclaration)
		{
			classDeclaration = node as ClassDeclarationSyntax;

			return classDeclaration is not null;
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

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }
		
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}
	
	/// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values when returning with the specified return value condition.</summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		/// <param name="member">
		/// The field or property member that is promised to be not-null.
		/// </param>
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			ReturnValue = returnValue;
			Members = new[] { member };
		}

		/// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </param>
		/// <param name="members">
		/// The list of field and property members that are promised to be not-null.
		/// </param>
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			ReturnValue = returnValue;
			Members = members;
		}

		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }

		/// <summary>Gets field or property member names.</summary>
		public string[] Members { get; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class PropertyGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s));
		}

		private static bool IsCandidate(SyntaxNode node, CancellationToken _)
		{
			return node is FieldDeclarationSyntax f
			       && f.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == "Property");
		}

		private static SourceText Transform(GeneratorSyntaxContext context, CancellationToken _)
		{
			var field = (FieldDeclarationSyntax)context.Node;
			if (!field.HasAttribute(context.SemanticModel, "Lombok.NET.PropertyAttribute"))
			{
				return null;
			}

			var @namespace = field.GetNamespace();
			if (@namespace is null)
			{
				throw new Exception($"Namespace could not be found for field {field}.");
			}

			var properties = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
				? field.Declaration.Variables.Select(v => CreateReadonlyProperty(field.Declaration.Type, v.Identifier.Text))
				: field.Declaration.Variables.Select(v =>
					CreateProperty(field.Declaration.Type, v.Identifier.Text, field.GetAttributeArgument<PropertyChangeType>("Property")));

			switch (field.Parent)
			{
				case ClassDeclarationSyntax containingClass:
					containingClass.EnsurePartial();

					return CreateTypeWithProperties(@namespace, containingClass.CreateNewPartialType(), properties);
				case StructDeclarationSyntax containingStruct:
					containingStruct.EnsurePartial();

					return CreateTypeWithProperties(@namespace, containingStruct.CreateNewPartialType(), properties);
				default:
					throw new Exception($"Field '{field}' is in neither a class, nor a struct. This behavior is not supported.");
			}
		}

		private static PropertyDeclarationSyntax CreateReadonlyProperty(TypeSyntax type, string fieldName)
		{
			return PropertyDeclaration(type, FieldToPropertyName(fieldName))
				.WithModifiers(
					TokenList(
						Token(SyntaxKind.PublicKeyword)
					)
				).WithExpressionBody(
					ArrowExpressionClause(
						IdentifierName(fieldName)
					)
				).WithSemicolonToken(
					Token(SyntaxKind.SemicolonToken)
				);
		}

		private static PropertyDeclarationSyntax CreateProperty(TypeSyntax type, string fieldName, PropertyChangeType? propertyChangeType)
		{
			return PropertyDeclaration(type, FieldToPropertyName(fieldName))
				.WithModifiers(
					TokenList(
						Token(SyntaxKind.PublicKeyword)
					)
				).WithAccessorList(
					AccessorList(
						List(
							new[]
							{
								AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
									.WithExpressionBody(
										ArrowExpressionClause(
											IdentifierName(fieldName)
										)
									).WithSemicolonToken(
										Token(SyntaxKind.SemicolonToken)
									),
								CreatePropertySetter(fieldName, propertyChangeType)
									.WithSemicolonToken(
										Token(SyntaxKind.SemicolonToken)
									)
							}
						)
					)
				);
		}

		private static AccessorDeclarationSyntax CreatePropertySetter(string fieldName, PropertyChangeType? propertyChangeType)
		{
			switch (propertyChangeType)
			{
				case PropertyChangeType.PropertyChanged:
					return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
						.WithExpressionBody(
							ArrowExpressionClause(
								InvocationExpression(
									IdentifierName(NotifyPropertyChangedGenerator.SetFieldMethodName)
								).WithArgumentList(
									ArgumentList(
										SeparatedList<ArgumentSyntax>(
											new SyntaxNodeOrToken[]
											{
												Argument(
													IdentifierName(fieldName)
												).WithRefOrOutKeyword(
													Token(SyntaxKind.OutKeyword)
												),
												Token(SyntaxKind.CommaToken),
												Argument(
													IdentifierName("value")
												)
											}
										)
									)
								)
							)
						);
				case PropertyChangeType.PropertyChanging:
					return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
						.WithExpressionBody(
							ArrowExpressionClause(
								InvocationExpression(
									IdentifierName(NotifyPropertyChangingGenerator.SetFieldMethodName)
								).WithArgumentList(
									ArgumentList(
										SeparatedList<ArgumentSyntax>(
											new SyntaxNodeOrToken[]
											{
												Argument(
													IdentifierName(fieldName)
												).WithRefOrOutKeyword(
													Token(SyntaxKind.OutKeyword)
												),
												Token(SyntaxKind.CommaToken),
												Argument(
													IdentifierName("value")
												)
											}
										)
									)
								)
							)
						);
				default:
					return AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
						.WithExpressionBody(
							ArrowExpressionClause(
								AssignmentExpression(
									SyntaxKind.SimpleAssignmentExpression,
									IdentifierName(fieldName),
									IdentifierName("value")
								)
							)
						);
			}
		}

		private static SourceText CreateTypeWithProperties(string @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<PropertyDeclarationSyntax> properties)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.WithMembers(
							List<MemberDeclarationSyntax>(properties)
						)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}

		/// <summary>
		/// Converts a field name to a property (e.g. "_age" becomes "Age").
		/// </summary>
		/// <param name="fieldName">The field name to get the property name from.</param>
		/// <returns>A property name from the given field name.</returns>
		private static string FieldToPropertyName(string fieldName)
		{
			return fieldName.Substring(1).Capitalize();
		}
	}
}
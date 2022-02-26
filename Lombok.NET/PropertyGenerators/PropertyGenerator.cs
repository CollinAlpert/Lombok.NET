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
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
		}

		private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			return node is FieldDeclarationSyntax f
			       && f.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == "Property");
		}

		private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			SymbolCache.PropertyAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<PropertyAttribute>();

			var field = (FieldDeclarationSyntax)context.Node;
			if (cancellationToken.IsCancellationRequested
			    || !field.Declaration.Variables.Any(v => v.ContainsAttribute(context.SemanticModel, SymbolCache.PropertyAttributeSymbol)))
			{
				return null;
			}

			var properties = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
				? field.Declaration.Variables.Select(v => CreateReadonlyProperty(field.Declaration.Type, v.Identifier.Text))
				: field.Declaration.Variables.Select(v =>
					CreateProperty(field.Declaration.Type, v.Identifier.Text, field.GetAttributeArgument<PropertyChangeType>("Property")));

			return field.Parent switch
			{
				// Caught by LOM001, LOM002 and LOM003
				ClassDeclarationSyntax containingClass
					when containingClass.CanGenerateCodeForType(out var @namespace) => CreateTypeWithProperties(@namespace, containingClass, properties),
				StructDeclarationSyntax containingStruct
					when containingStruct.CanGenerateCodeForType(out var @namespace) => CreateTypeWithProperties(@namespace, containingStruct, properties),
				// Caught by LOM005
				_ => null
			};
		}

		private static PropertyDeclarationSyntax CreateReadonlyProperty(TypeSyntax type, string fieldName)
		{
			return PropertyDeclaration(type, fieldName.ToPascalCaseIdentifier())
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
			return PropertyDeclaration(type, fieldName.ToPascalCaseIdentifier())
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
								AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
									.WithExpressionBody(
										CreatePropertySetter(fieldName, propertyChangeType)
									).WithSemicolonToken(
										Token(SyntaxKind.SemicolonToken)
									)
							}
						)
					)
				);
		}

		private static ArrowExpressionClauseSyntax CreatePropertySetter(string fieldName, PropertyChangeType? propertyChangeType)
		{
			switch (propertyChangeType)
			{
				case PropertyChangeType.PropertyChanged:
					return ArrowExpressionClause(
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
					);
				case PropertyChangeType.PropertyChanging:
					return ArrowExpressionClause(
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
					);
				case PropertyChangeType.ReactivePropertyChange:
					return ArrowExpressionClause(
						InvocationExpression(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								ThisExpression(),
								IdentifierName("RaiseAndSetIfChanged")
							)
						).WithArgumentList(
							ArgumentList(
								SeparatedList<ArgumentSyntax>(
									new SyntaxNodeOrToken[]
									{
										Argument(
											IdentifierName(fieldName)
										).WithRefOrOutKeyword(
											Token(SyntaxKind.RefKeyword)
										),
										Token(SyntaxKind.CommaToken),
										Argument(
											IdentifierName("value")
										)
									}
								)
							)
						)
					);
				default:
					return ArrowExpressionClause(
						AssignmentExpression(
							SyntaxKind.SimpleAssignmentExpression,
							IdentifierName(fieldName),
							IdentifierName("value")
						)
					);
			}
		}

		private static SourceText CreateTypeWithProperties(string @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<PropertyDeclarationSyntax> properties)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(typeDeclaration.GetUsings())
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.CreateNewPartialType()
							.WithMembers(
								List<MemberDeclarationSyntax>(properties)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
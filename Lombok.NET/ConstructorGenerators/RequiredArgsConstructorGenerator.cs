using System;
using System.Collections.Generic;
using System.Linq;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.ConstructorGenerators;

/// <summary>
/// Generates a constructor which takes all of the required members as arguments.
/// "Required" is defined differently depending on the type of the member:
/// - A property is considered required when there is no set accessor.
/// - A field is considered required when the <code>readonly</code> keyword has been applied to it.
/// </summary>
[Generator]
public class RequiredArgsConstructorGenerator : BaseConstructorGenerator
{
	/// <summary>
	/// The name (as used in user code) of the attribute this generator targets.
	/// </summary>
	protected override string AttributeName { get; } = typeof(RequiredArgsConstructorAttribute).FullName;

	/// <summary>
	/// Gets the to-be-generated constructor's parameters as well as its body.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration to generate the parts for.</param>
	/// <param name="attribute">The attribute declared on the type.</param>
	/// <returns>The constructor's parameters and its body.</returns>
	protected override (SyntaxKind modifier, ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorParts(TypeDeclarationSyntax typeDeclaration, AttributeData attribute)
	{
		var memberTypeArgument = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == nameof(RequiredArgsConstructorAttribute.MemberType));
		var accessTypesArgument = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == nameof(RequiredArgsConstructorAttribute.AccessTypes));
		var modifierTypeArgument = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == nameof(RequiredArgsConstructorAttribute.ModifierType));
		var memberType = (MemberType?)(memberTypeArgument.Value.Value as int?) ?? MemberType.Field;
		var accessType = (AccessTypes?)(accessTypesArgument.Value.Value as int?) ?? AccessTypes.Private;
		var modifierType = (AccessTypes?)(modifierTypeArgument.Value.Value as int?);
		var modifier = modifierType switch
		{
			AccessTypes.Public => SyntaxKind.PublicKeyword,
			AccessTypes.Internal => SyntaxKind.InternalKeyword,
			AccessTypes.Protected => SyntaxKind.ProtectedKeyword,
			AccessTypes.Private => SyntaxKind.PrivateKeyword,
			_ => typeDeclaration.GetAccessibilityModifier(),
		};
		switch (memberType)
		{
			case MemberType.Field:
			{
				var fields = typeDeclaration.Members
					.OfType<FieldDeclarationSyntax>()
					.Where(IsFieldRequired)
					.Where(static p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
					.Where(accessType)
					.ToList();
				if (fields.Count == 0)
				{
					return (modifier, ParameterList(), Block());
				}

				List<(TypeSyntax Type, string Name)> typesAndNames = fields
					.SelectMany(static p => p.Declaration.Variables.Select(v => (p.Declaration.Type, v.Identifier.Text)))
					.ToList();

				return GetConstructorParts(modifier, typesAndNames, static s => s.ToCamelCaseIdentifier());
			}
			case MemberType.Property:
			{
				var properties = typeDeclaration.Members
					.OfType<PropertyDeclarationSyntax>()
					.Where(IsPropertyRequired)
					.Where(static p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
					.Where(accessType)
					.ToList();
				if (properties.Count == 0)
				{
					return (modifier, ParameterList(), Block());
				}

				List<(TypeSyntax Type, string Name)> typesAndNames = properties
					.Select(static p => (p.Type, p.Identifier.Text))
					.ToList();

				return GetConstructorParts(modifier, typesAndNames, static s => s.ToCamelCaseIdentifier());
			}
			default: throw new ArgumentOutOfRangeException(nameof(memberType));
		}
	}

	/// <summary>
	/// Specifies if the property is considered required. 
	/// </summary>
	/// <returns>True, if the property does not have a setter.</returns>
	protected virtual bool IsPropertyRequired(PropertyDeclarationSyntax p)
	{
		return p.AccessorList == null || !p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration);
	}

	/// <summary>
	/// Specifies if the field is considered required. 
	/// </summary>
	/// <returns>True, if the field is marked as readonly.</returns>
	protected virtual bool IsFieldRequired(FieldDeclarationSyntax f)
	{
		return f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
	}

	/// <summary>
	/// Gets the constructor's parameters as well as its body.
	/// </summary>
	/// <param name="modifier"></param>
	/// <param name="members">The type's members split into type and name.</param>
	/// <param name="parameterTransformer">A function for transforming the member's name into the name which will be used for the constructor.</param>
	/// <returns>The constructor's parameters as well as its body.</returns>
	private static (SyntaxKind modifier, ParameterListSyntax Parameters, BlockSyntax Body) GetConstructorParts(SyntaxKind modifier, IReadOnlyCollection<(TypeSyntax Type, string Name)> members,
		Func<string, string> parameterTransformer)
	{
		int duplicationCounter = 1;
		HashSet<string> parameters = new();
		var constructorParameters = new List<ParameterSyntax>();
		var constructorBody = new List<ExpressionStatementSyntax>();
		foreach (var (type, name) in members)
		{
			string suggestedParameterName = parameterTransformer(name);
			if(!parameters.Add(suggestedParameterName))
			{
				suggestedParameterName += duplicationCounter++;
			}

			ParameterSyntax parameter = CreateParameter(type, suggestedParameterName);
			constructorParameters.Add(parameter);
			constructorBody.Add(CreateExpression(name, parameter.Identifier.Text));
		}

		return (modifier, ParameterList(SeparatedList(constructorParameters)), Block(constructorBody));
	}

	private static ExpressionStatementSyntax CreateExpression(string variable, string argument)
	{
		return ExpressionStatement(
			AssignmentExpression(
				SyntaxKind.SimpleAssignmentExpression,
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					ThisExpression(),
					IdentifierName(variable)
				),
				IdentifierName(argument.EscapeReservedKeyword())
			)
		);
	}

	private static ParameterSyntax CreateParameter(TypeSyntax type, string name)
	{
		return Parameter(Identifier(name.EscapeReservedKeyword())).WithType(type);
	}
}
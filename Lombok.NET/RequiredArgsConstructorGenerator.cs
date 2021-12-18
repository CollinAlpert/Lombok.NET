using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET
{
	/// <summary>
	/// Generates a constructor which takes all of the required members as arguments.
	/// "Required" is defined differently depending on the type of the member:
	/// - A property is considered required when there is no set accessor.
	/// - A field is considered required when the <code>readonly</code> keyword has been applied to it.
	/// </summary>
	[Generator]
	public class RequiredArgsConstructorGenerator : BaseConstructorGenerator
	{
		protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new RequiredArgsConstructorSyntaxReceiver();

		protected virtual string AttributeName { get; } = "RequiredArgsConstructor";

		protected override (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax typeDeclaration)
		{
			var memberType = typeDeclaration.GetAttributeArgument<MemberType>(AttributeName);
			var accessType = typeDeclaration.GetAttributeArgument<AccessTypes>(AttributeName);
			switch (memberType)
			{
				case MemberType.Field:
				{
					var fields = typeDeclaration.Members
						.OfType<FieldDeclarationSyntax>()
						.Where(IsFieldRequired)
						.Where(p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
						.Where(accessType)
						.ToList();
					if(fields.Count == 0)
					{
						return (ParameterList(), Block());
					}
					
					List<(TypeSyntax Type, string Name)> typesAndNames = fields
						.SelectMany(p => p.Declaration.Variables.Select(v => (p.Declaration.Type, v.Identifier.Text)))
						.ToList();

					return GetConstructorParts(typesAndNames, s => s.Substring(1));
				}
				case MemberType.Property:
				{
					var properties = typeDeclaration.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(IsPropertyRequired)
						.Where(p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
						.Where(accessType)
						.ToList();
					if(properties.Count == 0)
					{
						return (ParameterList(), Block());
					}
					
					List<(TypeSyntax Type, string Name)> typesAndNames = properties
						.Select(p => (p.Type, p.Identifier.Text))
						.ToList();

					return GetConstructorParts(typesAndNames, s => char.ToLower(s[0]) + s.Substring(1));
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(memberType));
			}
		}

		protected virtual bool IsPropertyRequired(PropertyDeclarationSyntax p)
		{
			return p.AccessorList == null || !p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration);
		}

		protected virtual bool IsFieldRequired(FieldDeclarationSyntax f)
		{
			return f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
		}

		/// <summary>
		/// Gets the constructor's parameters as well as its body.
		/// </summary>
		/// <param name="members">The type's members split into type and name.</param>
		/// <param name="parameterTransformer">A function for transforming the member's name into the name which will be used for the constructor.</param>
		/// <returns>The constructor's parameters as well as its body.</returns>
		private static (ParameterListSyntax Parameters, BlockSyntax Body) GetConstructorParts(IReadOnlyCollection<(TypeSyntax Type, string Name)> members,
			Func<string, string> parameterTransformer)
		{
			
			var parameters = members.Select(tn => (tn.Type, parameterTransformer(tn.Name))).ToList();
			var constructorParameters = ParameterList(SeparatedList(members.Select(tn => Parameter(Identifier(parameterTransformer(tn.Name))).WithType(tn.Type))));
			var constructorBody = Block(members.Zip(parameters, (t1, t2) => CreateExpression(t1.Name, t2.Item2)));

			return (constructorParameters, constructorBody);
		}

		private static ExpressionStatementSyntax CreateExpression(string variable, string argument)
		{
			return ExpressionStatement(
				AssignmentExpression(
					SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(variable),
					IdentifierName(argument)
				)
			);
		}
	}
}
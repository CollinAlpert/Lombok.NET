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
			var attributeArguments = GetAttribute(typeDeclaration).ArgumentList;
			var (memberType, accessType) = GetAttributeModifiers(attributeArguments);
			switch (memberType)
			{
				case MemberType.Field:
				{
					var fields = typeDeclaration.Members
						.OfType<FieldDeclarationSyntax>()
						.Where(IsFieldRequired)
						.ToList();
					if(fields.Count == 0)
					{
						return (ParameterList(), Block());
					}
					
					List<(TypeSyntax Type, string Name)> typesAndNames = FilterByAccessType(fields, accessType)
						.SelectMany(p => p.Declaration.Variables.Select(v => (p.Declaration.Type, v.Identifier.Text)))
						.ToList();

					return GetConstructorParts(typesAndNames, s => s[1..]);
				}
				case MemberType.Property:
				{
					var properties = typeDeclaration.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(IsPropertyRequired)
						.ToList();
					if(properties.Count == 0)
					{
						return (ParameterList(), Block());
					}
					
					List<(TypeSyntax Type, string Name)> typesAndNames = FilterByAccessType(properties, accessType)
						.Select(p => (p.Type, p.Identifier.Text))
						.ToList();

					return GetConstructorParts(typesAndNames, s => char.ToLower(s[0]) + s[1..]);
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

		private AttributeSyntax GetAttribute(MemberDeclarationSyntax typeDeclarationSyntax)
		{
			bool GetAttributeCondition(AttributeSyntax a) => a.Name.ToString() == AttributeName;

			return typeDeclarationSyntax.AttributeLists.First(l => l.Attributes.Any(GetAttributeCondition)).Attributes.First(GetAttributeCondition);
		}

		/// <summary>
		/// Extracts the modifiers which tell the generator which type of members to include in the constructor.
		/// </summary>
		/// <param name="argumentList">The argument list of the attribute</param>
		/// <returns>A tuple containing the type of member to focus on (property or field) as well as its access modifier.</returns>
		private static (MemberType, AccessType) GetAttributeModifiers(AttributeArgumentListSyntax? argumentList)
		{
			var memberType = MemberType.Field;
			var accessType = AccessType.Private;

			if (argumentList is null || argumentList.Arguments.Count == 0)
			{
				return (memberType, accessType);
			}

			var arguments = argumentList.Arguments;
			var memberTypeArgument = arguments.FirstOrDefault(a => a.Expression is MemberAccessExpressionSyntax memberAccess
			                                                       && memberAccess.Expression.ToString() == nameof(MemberType));
			var accessTypeArgument = arguments.FirstOrDefault(a => a.Expression is MemberAccessExpressionSyntax memberAccess
			                                                       && memberAccess.Expression.ToString() == nameof(AccessType));
			if (memberTypeArgument != null &&
			    Enum.TryParse(((MemberAccessExpressionSyntax)memberTypeArgument.Expression).Name.Identifier.Text, out MemberType mT))
			{
				memberType = mT;
			}

			if (accessTypeArgument != null &&
			    Enum.TryParse(((MemberAccessExpressionSyntax)accessTypeArgument.Expression).Name.Identifier.Text, out AccessType aT))
			{
				accessType = aT;
			}

			return (memberType, accessType);
		}

		/// <summary>
		/// Removes all the members which do not have the desired access modifier.
		/// </summary>
		/// <param name="members">The members to filter</param>
		/// <param name="accessType">The access modifer to look out for.</param>
		/// <typeparam name="T">The type of the members (<code>PropertyDeclarationSyntax</code>/<code>FieldDeclarationSyntax</code>).</typeparam>
		/// <returns>The members which have the desired access modifier.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If an access modifier is supplied which is not supported.</exception>
		private static IEnumerable<T> FilterByAccessType<T>(IEnumerable<T> members, AccessType accessType) where T : MemberDeclarationSyntax
		{
			return accessType switch
			{
				AccessType.Private => members.Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword)),
				AccessType.Protected => members.Where(m => m.Modifiers.Any(SyntaxKind.ProtectedKeyword)),
				AccessType.Public => members.Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword)),
				AccessType.PrivateAndProtected => members.Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword) || m.Modifiers.Any(SyntaxKind.ProtectedKeyword)),
				AccessType.PrivateAndPublic => members.Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword) || m.Modifiers.Any(SyntaxKind.PublicKeyword)),
				AccessType.ProtectedAndPublic => members.Where(m => m.Modifiers.Any(SyntaxKind.ProtectedKeyword) || m.Modifiers.Any(SyntaxKind.PublicKeyword)),
				AccessType.PrivateProtectedAndPublic => members.Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword)
				                                                           || m.Modifiers.Any(SyntaxKind.ProtectedKeyword)
				                                                           || m.Modifiers.Any(SyntaxKind.PublicKeyword)),
				_ => throw new ArgumentOutOfRangeException(nameof(accessType))
			};
		}

		/// <summary>
		/// Gets the constructor's parameters as well as its body.
		/// </summary>
		/// <param name="members"></param>
		/// <param name="parameterTransformer"></param>
		/// <returns></returns>
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
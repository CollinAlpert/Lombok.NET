using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET
{
	/// <summary>
	/// Base class for syntax receivers that discover types which have certain attributes. 
	/// </summary>
	public abstract class BaseAttributeSyntaxReceiver : ISyntaxContextReceiver
	{
		protected abstract string FullAttributeName { get; }

		public readonly List<ClassDeclarationSyntax> ClassCandidates = new List<ClassDeclarationSyntax>();
		public readonly List<InterfaceDeclarationSyntax> InterfaceCandidates = new List<InterfaceDeclarationSyntax>();
		public readonly List<EnumDeclarationSyntax> EnumCandidates = new List<EnumDeclarationSyntax>();
		public readonly List<StructDeclarationSyntax> StructCandidates = new List<StructDeclarationSyntax>();

		public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
		{
			switch (context.Node)
			{
				case ClassDeclarationSyntax classDeclaration when HasAttribute(classDeclaration, context.SemanticModel, FullAttributeName):
					ClassCandidates.Add(classDeclaration);

					break;
				case InterfaceDeclarationSyntax interfaceDeclaration when HasAttribute(interfaceDeclaration, context.SemanticModel, FullAttributeName):
					InterfaceCandidates.Add(interfaceDeclaration);

					break;
				case EnumDeclarationSyntax enumDeclaration when HasAttribute(enumDeclaration, context.SemanticModel, FullAttributeName):
					EnumCandidates.Add(enumDeclaration);

					break;
				case StructDeclarationSyntax structDeclaration when HasAttribute(structDeclaration, context.SemanticModel, FullAttributeName):
					StructCandidates.Add(structDeclaration);

					break;
			}
		}

		private static bool HasAttribute(MemberDeclarationSyntax member, SemanticModel semanticModel, string fullAttributeName)
		{
			return member.AttributeLists.SelectMany(l => l.Attributes).Any(a => AttributeMatches(semanticModel, a, fullAttributeName));
		}

		private static bool AttributeMatches(SemanticModel semanticModel, AttributeSyntax attribute, string fullAttributeName)
		{
			var typeInfo = semanticModel.GetTypeInfo(attribute).Type;
			if (typeInfo is null)
			{
				return false;
			}

			return $"{typeInfo.ContainingAssembly.Name}.{typeInfo.Name}" == fullAttributeName;
		}
	}
}
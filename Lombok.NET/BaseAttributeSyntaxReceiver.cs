using System.Collections.Generic;
using Lombok.NET.Extensions;
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
				case ClassDeclarationSyntax classDeclaration when classDeclaration.HasAttribute(context.SemanticModel, FullAttributeName):
					ClassCandidates.Add(classDeclaration);

					break;
				case InterfaceDeclarationSyntax interfaceDeclaration when interfaceDeclaration.HasAttribute(context.SemanticModel, FullAttributeName):
					InterfaceCandidates.Add(interfaceDeclaration);

					break;
				case EnumDeclarationSyntax enumDeclaration when enumDeclaration.HasAttribute(context.SemanticModel, FullAttributeName):
					EnumCandidates.Add(enumDeclaration);

					break;
				case StructDeclarationSyntax structDeclaration when structDeclaration.HasAttribute(context.SemanticModel, FullAttributeName):
					StructCandidates.Add(structDeclaration);

					break;
			}
		}
	}
}
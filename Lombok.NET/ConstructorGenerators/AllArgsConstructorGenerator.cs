﻿using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.ConstructorGenerators;

/// <summary>
/// Generates a constructor which takes all of the members as arguments.
/// An all-arguments constructor is basically just a required-arguments constructor where all the members are required.
/// </summary>
[Generator]
public sealed class AllArgsConstructorGenerator : RequiredArgsConstructorGenerator
{
	/// <summary>
	/// The name (as used in user code) of the attribute this generator targets.
	/// </summary>
	protected override string AttributeName { get; } = "AllArgsConstructor";

	/// <summary>
	/// Specifies if the property is considered required. In the case of the AllArgsConstructor, this is always the case. 
	/// </summary>
	/// <returns>Always true.</returns>
	protected override bool IsPropertyRequired(PropertyDeclarationSyntax _)
	{
		return true;
	}

	/// <summary>
	/// Specifies if the field is considered required. In the case of the AllArgsConstructor, this is always the case. 
	/// </summary>
	/// <returns>Always true.</returns>
	protected override bool IsFieldRequired(FieldDeclarationSyntax _)
	{
		return true;
	}

	/// <summary>
	/// Gets the type symbol for the targeted attribute.
	/// </summary>
	/// <param name="model">The semantic model to retrieve the symbol from.</param>
	/// <returns>The attribute's type symbol.</returns>
	protected override INamedTypeSymbol GetAttributeSymbol(SemanticModel model)
	{
		return SymbolCache.AllArgsConstructorAttributeSymbol ??= model.Compilation.GetSymbolByType<AllArgsConstructorAttribute>();
	}
}
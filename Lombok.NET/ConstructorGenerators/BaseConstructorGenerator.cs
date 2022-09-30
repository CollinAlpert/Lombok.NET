using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.ConstructorGenerators;

/// <summary>
/// Base class for source generators which generate constructors.
/// </summary>
public abstract class BaseConstructorGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Initializes this generator.
	/// </summary>
	/// <param name="context"></param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG
        SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform);
		context.AddSources(sources);
	}

	private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		TypeDeclarationSyntax? typeDeclaration = node as ClassDeclarationSyntax;
		typeDeclaration ??= node as StructDeclarationSyntax;
		if (typeDeclaration is null)
		{
			return false;
		}

		return typeDeclaration.AttributeLists
			.SelectMany(static l => l.Attributes)
			.Any(a => a.IsNamed(AttributeName));
	}

	private GeneratorResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.Node;
		if (!typeDeclaration.ContainsAttribute(context.SemanticModel, GetAttributeSymbol(context.SemanticModel)))
		{
			return GeneratorResult.Empty;
		}

		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		var (constructorParameters, constructorBody) = GetConstructorParts(typeDeclaration);
		// Dirty.
		if (constructorParameters.Parameters.Count == 0 && AttributeName != "NoArgsConstructor")
		{
			// No members were found to generate a constructor for.
			return GeneratorResult.Empty;
		}
		
		cancellationToken.ThrowIfCancellationRequested();

		var sourceText = CreateConstructorCode(@namespace, typeDeclaration, constructorParameters, constructorBody);

		return new GeneratorResult(typeDeclaration.Identifier.Text, sourceText);
	}

	/// <summary>
	/// Gets the to-be-generated constructor's parameters as well as its body.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration to generate the parts for.</param>
	/// <returns>The constructor's parameters and its body.</returns>
	protected abstract (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorParts(TypeDeclarationSyntax typeDeclaration);

	/// <summary>
	/// class HiddenAttribute : Attribute
	/// 
	/// ->
	/// 
	/// "Hidden"
	/// </summary>
	protected abstract string AttributeName { get; }

	/// <summary>
	/// Gets the type symbol for the targeted attribute.
	/// </summary>
	/// <param name="model">The semantic model to retrieve the symbol from.</param>
	/// <returns>The attribute's type symbol.</returns>
	protected abstract INamedTypeSymbol GetAttributeSymbol(SemanticModel model);

	private static SourceText CreateConstructorCode(string @namespace, TypeDeclarationSyntax typeDeclaration, ParameterListSyntax constructorParameters, BlockSyntax constructorBody)
	{
		MemberDeclarationSyntax constructor = ConstructorDeclaration(typeDeclaration.Identifier.Text)
			.WithParameterList(constructorParameters)
			.WithBody(constructorBody)
			.WithModifiers(TokenList(Token(typeDeclaration.GetAccessibilityModifier())));

		return CompilationUnit()
			.WithUsings(typeDeclaration.GetUsings())
			.WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					NamespaceDeclaration(
						IdentifierName(@namespace)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							typeDeclaration.CreateNewPartialType()
								.WithMembers(
									SingletonList(constructor)
								)
						)
					)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
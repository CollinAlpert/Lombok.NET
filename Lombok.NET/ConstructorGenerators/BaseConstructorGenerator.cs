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
		var sources = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeName, IsCandidate, Transform);
		context.AddSources(sources);
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node is ClassDeclarationSyntax or StructDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		var (modifier, constructorParameters, constructorBody) = GetConstructorParts(typeDeclaration, context.Attributes[0]);
		// Dirty.
		if (constructorParameters.Parameters.Count == 0 && AttributeName != typeof(NoArgsConstructorAttribute).FullName)
		{
			// No members were found to generate a constructor for.
			return GeneratorResult.Empty;
		}

		cancellationToken.ThrowIfCancellationRequested();

		var sourceText = CreateConstructorCode(@namespace, typeDeclaration, modifier, constructorParameters, constructorBody);

		return new GeneratorResult(typeDeclaration.GetHintName(@namespace), sourceText);
	}

	/// <summary>
	/// Gets the to-be-generated constructor's parameters as well as its body.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration to generate the parts for.</param>
	/// <param name="attribute">The attribute declared on the type.</param>
	/// <returns>The constructor's parameters and its body.</returns>
	protected abstract (SyntaxKind modifier, ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorParts(TypeDeclarationSyntax typeDeclaration, AttributeData attribute);

	/// <summary>
	/// The name of the target attribute. Should be the result of "typeof(NoArgsConstructorAttribute).FullName".
	/// </summary>
	protected abstract string AttributeName { get; }

	private static SourceText CreateConstructorCode(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, SyntaxKind modifier, ParameterListSyntax constructorParameters, BlockSyntax constructorBody)
	{
		MemberDeclarationSyntax constructor = ConstructorDeclaration(typeDeclaration.Identifier.Text)
			.WithParameterList(constructorParameters)
			.WithBody(constructorBody)
			.WithModifiers(TokenList(Token(modifier)));

		return @namespace.CreateNewNamespace(typeDeclaration.GetUsings(),
				typeDeclaration.CreateNewPartialType()
					.WithMembers(
						SingletonList(constructor)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
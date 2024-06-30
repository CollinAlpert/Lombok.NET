using System.Text;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generates With builder methods for a class.
/// </summary>
[Generator]
internal sealed class WithMethodsGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(WithAttribute).FullName;

	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var sources = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeName, IsCandidate, Transform);
		context.AddSources(sources);
	}

	private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node is ClassDeclarationSyntax;
	}

	private GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		var namedType = (INamedTypeSymbol)context.TargetSymbol;
		if (!classDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var memberTypeArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(RequiredArgsConstructorAttribute.MemberType));
		var memberType = (MemberType?)(memberTypeArgument.Value.Value as int?) ?? MemberType.Field;
		var includeInheritedMembers = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(WithAttribute.IncludeInheritedMembers)).Value.Value as bool? ?? false;
		
		var methods = memberType switch
		{
			MemberType.Property => new WithMethodPropertyProvider().Generate(namedType, includeInheritedMembers),
			MemberType.Field => new WithMethodFieldProvider().Generate(namedType, includeInheritedMembers),
			_ => throw new ArgumentOutOfRangeException(nameof(memberType))
		};

		var partialClassSourceText = CreatePartialClass(@namespace, classDeclaration, methods);

		return new GeneratorResult(classDeclaration.GetHintName(@namespace), partialClassSourceText);
	}

	private static SourceText CreatePartialClass(NameSyntax @namespace, ClassDeclarationSyntax classDeclaration, IEnumerable<MethodDeclarationSyntax> methods)
	{
		return @namespace.CreateNewNamespace(classDeclaration.GetUsings(),
				classDeclaration.CreateNewPartialClass()
					.WithMembers(
						List<MemberDeclarationSyntax>(methods)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
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

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Generator which generated async overloads for abstract or interface methods.
/// </summary>
[Generator]
public sealed class AsyncOverloadsGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(AsyncOverloadsAttribute).FullName;

	private static readonly ParameterSyntax CancellationTokenParameter = Parameter(
		Identifier("cancellationToken")
	).WithType(
		IdentifierName("global::System.Threading.CancellationToken")
	).WithDefault(
		EqualsValueClause(
			LiteralExpression(
				SyntaxKind.DefaultLiteralExpression,
				Token(SyntaxKind.DefaultKeyword)
			)
		)
	);

	/// <summary>
	/// Initializes the generator logic.
	/// </summary>
	/// <param name="context">The context of initializing the generator.</param>
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
		return node is ClassDeclarationSyntax or InterfaceDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		IEnumerable<MemberDeclarationSyntax> asyncOverloads;
		SourceText? partialTypeSourceText;
		switch (typeDeclaration)
		{
			case InterfaceDeclarationSyntax interfaceDeclaration when interfaceDeclaration.Members.Any():
				asyncOverloads = interfaceDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.Where(static m => m.Body is null)
					.Select(CreateAsyncOverload);
				partialTypeSourceText = CreatePartialType(@namespace, interfaceDeclaration, asyncOverloads);

				return new GeneratorResult(interfaceDeclaration.Identifier.Text, partialTypeSourceText);
			case ClassDeclarationSyntax classDeclaration when classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword):
				asyncOverloads = classDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.Where(static m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
					.Select(CreateAsyncOverload);
				partialTypeSourceText = CreatePartialType(@namespace, classDeclaration, asyncOverloads);

				return new GeneratorResult(classDeclaration.GetHintName(@namespace), partialTypeSourceText);
			default:
				throw new ArgumentOutOfRangeException(nameof(typeDeclaration));
		}
	}

	private static MethodDeclarationSyntax CreateAsyncOverload(MethodDeclarationSyntax m)
	{
		var newReturnType = m.ReturnType.IsVoid()
			? (TypeSyntax)IdentifierName("global::System.Threading.Tasks.Task")
			: GenericName(
				Identifier("global::System.Threading.Tasks.Task")
			).WithTypeArgumentList(
				TypeArgumentList(
					SingletonSeparatedList(m.ReturnType)
				)
			);

		return m.WithIdentifier(
				Identifier(m.Identifier.Text + "Async")
			).WithReturnType(newReturnType)
			.AddParameterListParameters(CancellationTokenParameter);
	}

	private static SourceText CreatePartialType(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<MemberDeclarationSyntax> methods)
	{
		return @namespace.CreateNewNamespace(typeDeclaration.GetUsings(),
				typeDeclaration.CreateNewPartialType()
					.WithMembers(
						List(methods)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
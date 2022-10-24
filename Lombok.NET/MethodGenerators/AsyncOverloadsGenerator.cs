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
	private static readonly ParameterSyntax CancellationTokenParameter = Parameter(
		Identifier("cancellationToken")
	).WithType(
		IdentifierName("CancellationToken")
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
		var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(static s => s != null);
		context.AddSources(sources);
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		TypeDeclarationSyntax? typeDeclaration = node as InterfaceDeclarationSyntax;
		typeDeclaration ??= node as ClassDeclarationSyntax;
		if (typeDeclaration is null)
		{
			return false;
		}

		return typeDeclaration.AttributeLists
			.SelectMany(static l => l.Attributes)
			.Any(static a => a.IsNamed("AsyncOverloads"));
	}

	private static GeneratorResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		SymbolCache.AsyncOverloadsAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<AsyncOverloadsAttribute>();

		var typeDeclaration = (TypeDeclarationSyntax)context.Node;
		if (!typeDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncOverloadsAttributeSymbol))
		{
			return GeneratorResult.Empty;
		}

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

				return new GeneratorResult(classDeclaration.Identifier.Text, partialTypeSourceText);
			default:
				throw new ArgumentOutOfRangeException(nameof(typeDeclaration));
		}
	}

	private static MethodDeclarationSyntax CreateAsyncOverload(MethodDeclarationSyntax m)
	{
		var newReturnType = m.ReturnType.IsVoid()
			? (TypeSyntax)IdentifierName("Task")
			: GenericName(
				Identifier("Task")
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

	private static SourceText CreatePartialType(string @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<MemberDeclarationSyntax> methods)
	{
		var usings = typeDeclaration.GetUsings();
		var threadingUsing = "System.Threading.Tasks".CreateUsingDirective();
		if (usings.All(u => !AreEquivalent(u, threadingUsing)))
		{
			usings = usings.Add(threadingUsing);
		}

		return CompilationUnit()
			.WithUsings(usings)
			.WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					FileScopedNamespaceDeclaration(
						IdentifierName(@namespace)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							typeDeclaration.CreateNewPartialType()
								.WithMembers(
									List(methods)
								)
						)
					)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
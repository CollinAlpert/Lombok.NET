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
/// Generator which generates async versions of methods.
/// </summary>
[Generator]
public class AsyncGenerator : IIncrementalGenerator
{
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
		return node.IsMethod(out var method) &&
		       method.AttributeLists
			       .SelectMany(static l => l.Attributes)
			       .Any(static a => a.IsNamed("Async"));
	}

	private static GeneratorResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		SymbolCache.AsyncAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<AsyncAttribute>();

		var method = (MethodDeclarationSyntax)context.Node;
		if (!method.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncAttributeSymbol))
		{
			return GeneratorResult.Empty;
		}

		var arguments = method.ParameterList.Parameters.Select(static p =>
			Argument(
				IdentifierName(p.Identifier.Text)
			)
		);
		var syncMethodInvocation = InvocationExpression(
			IdentifierName(method.Identifier.Text)
		).WithArgumentList(
			ArgumentList(
				SeparatedList(arguments)
			)
		);

		cancellationToken.ThrowIfCancellationRequested();

		MethodDeclarationSyntax asyncMethod;
		if (method.ReturnType.IsVoid())
		{
			asyncMethod = method.WithIdentifier(
					Identifier(method.Identifier.Text + "Async")
				).WithReturnType(
					IdentifierName("Task")
				).WithBody(
					Block(
						ExpressionStatement(
							syncMethodInvocation
						),
						ReturnStatement(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName("Task"),
								IdentifierName("CompletedTask")
							)
						)
					)
				).WithExpressionBody(null)
				.WithAttributeLists(List<AttributeListSyntax>());
		}
		else
		{
			asyncMethod = method.WithIdentifier(
					Identifier(method.Identifier.Text + "Async")
				).WithReturnType(
					GenericName(
							Identifier("Task")
						)
						.WithTypeArgumentList(
							TypeArgumentList(
								SingletonSeparatedList(method.ReturnType)
							)
						)
				).WithBody(null)
				.WithExpressionBody(
					ArrowExpressionClause(
						InvocationExpression(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName("Task"),
								IdentifierName("FromResult")
							)
						).WithArgumentList(
							ArgumentList(
								SingletonSeparatedList(
									Argument(syncMethodInvocation)
								)
							)
						)
					)
				).WithSemicolonToken(
					Token(SyntaxKind.SemicolonToken)
				).WithAttributeLists(List<AttributeListSyntax>());
		}

		if (method.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			return GeneratorResult.Empty;
		}

		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		var partialTypeSourceText = CreatePartialType(@namespace, typeDeclaration, asyncMethod);

		return new GeneratorResult($"{typeDeclaration.Identifier.Text}.{method.Identifier.Text}", partialTypeSourceText);
	}

	private static SourceText CreatePartialType(string @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax asyncMethod)
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
					NamespaceDeclaration(
							IdentifierName(@namespace)
						).WithMembers(
							SingletonList<MemberDeclarationSyntax>(
								typeDeclaration.CreateNewPartialType()
									.WithMembers(
										SingletonList<MemberDeclarationSyntax>(asyncMethod)
									)
							)
						)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
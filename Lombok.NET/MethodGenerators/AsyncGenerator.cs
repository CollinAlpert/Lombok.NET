using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Analyzers;
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
public sealed class AsyncGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(AsyncAttribute).FullName;

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
		return node is MethodDeclarationSyntax or LocalFunctionStatementSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetNode is LocalFunctionStatementSyntax || context.TargetNode.Parent is not TypeDeclarationSyntax typeDeclaration)
		{
			var d = Diagnostic.Create(DiagnosticDescriptors.MethodMustBeInPartialClassOrStruct, context.TargetNode.GetLocation());

			return new GeneratorResult(d);
		}

		var method = (MethodDeclarationSyntax)context.TargetNode;

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
					IdentifierName("global::System.Threading.Tasks.Task")
				).WithBody(
					Block(
						ExpressionStatement(
							syncMethodInvocation
						),
						ReturnStatement(
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName("global::System.Threading.Tasks.Task"),
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
							Identifier("global::System.Threading.Tasks.Task")
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
								IdentifierName("global::System.Threading.Tasks.Task"),
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

		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		var partialTypeSourceText = CreatePartialType(@namespace, typeDeclaration, asyncMethod);
		var hintName = typeDeclaration.GetHintName(@namespace);

		return new GeneratorResult($"{hintName}.{method.Identifier.Text}", partialTypeSourceText);
	}

	private static SourceText CreatePartialType(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax asyncMethod)
	{
		return @namespace.CreateNewNamespace(typeDeclaration.GetUsings(),
				typeDeclaration.CreateNewPartialType()
					.WithMembers(
						SingletonList<MemberDeclarationSyntax>(asyncMethod)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
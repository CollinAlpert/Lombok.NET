using System;
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

namespace Lombok.NET.MethodGenerators
{
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
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
		}

		private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
		{
			return node.IsMethod(out var method) &&
			       method.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.IsNamed("Async"));
		}

		private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			SymbolCache.AsyncAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<AsyncAttribute>();

			var method = (MethodDeclarationSyntax)context.Node;
			if (!method.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncAttributeSymbol))
			{
				return null;
			}

			var arguments = method.ParameterList.Parameters.Select(p =>
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

			// Caught by LOM001, LOM002, LOM003 and LOM004
			if (method.Parent is not TypeDeclarationSyntax typeDeclaration || !typeDeclaration.CanGenerateCodeForType(out var @namespace))
			{
				return null;
			}

			return CreatePartialType(@namespace, typeDeclaration, asyncMethod);
		}

		private static SourceText CreatePartialType(string @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax asyncMethod)
		{
			var usings = typeDeclaration.GetUsings();
			usings.Add("System.Threading.Tasks".CreateUsingDirective());

			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(usings)
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.CreateNewPartialType()
							.WithMembers(
								SingletonList<MemberDeclarationSyntax>(asyncMethod)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
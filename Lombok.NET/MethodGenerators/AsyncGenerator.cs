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
	[Generator]
	public class AsyncGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s));
		}

		private static bool IsCandidate(SyntaxNode node, CancellationToken _)
		{
			return node is MethodDeclarationSyntax f
			       && f.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == "Async");
		}

		private static SourceText Transform(GeneratorSyntaxContext context, CancellationToken _)
		{
			var method = (MethodDeclarationSyntax)context.Node;
			if (!method.HasAttribute(context.SemanticModel, typeof(AsyncAttribute).FullName))
			{
				return null;
			}

			var @namespace = method.GetNamespace();
			if (@namespace is null)
			{
				throw new Exception($"Namespace could not be found for field {method.Identifier.Text}.");
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

			switch (method.Parent)
			{
				case ClassDeclarationSyntax classDeclaration:
					classDeclaration.EnsurePartial();

					return CreatePartialType(@namespace, classDeclaration.CreateNewPartialType(), asyncMethod);
				case StructDeclarationSyntax structDeclaration:
					structDeclaration.EnsurePartial();

					return CreatePartialType(@namespace, structDeclaration.CreateNewPartialType(), asyncMethod);
				default:
					throw new Exception($"Method '{method.Identifier.Text}' must be placed within a class or struct. It cannot be a local function.");
			}
		}

		private static SourceText CreatePartialType(string @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax asyncMethod)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(
					SingletonList(
						UsingDirective(
							QualifiedName(
								QualifiedName(
									IdentifierName("System"),
									IdentifierName("Threading")
								),
								IdentifierName("Tasks")
							)
						)
					)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.WithMembers(
							SingletonList<MemberDeclarationSyntax>(asyncMethod)
						)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
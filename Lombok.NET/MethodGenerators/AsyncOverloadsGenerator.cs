using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Lombok.NET.MethodGenerators
{
	[Generator]
	public class AsyncOverloadsGenerator : ISourceGenerator
	{
		private static readonly ParameterSyntax CancellationTokenParameter = Parameter(
				Identifier("cancellationToken")
			)
			.WithType(
				IdentifierName("CancellationToken")
			)
			.WithDefault(
				EqualsValueClause(
					LiteralExpression(
						SyntaxKind.DefaultLiteralExpression,
						Token(SyntaxKind.DefaultKeyword)
					)
				)
			);

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new AsyncOverloadsSyntaxReceiver());
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!(context.SyntaxContextReceiver is AsyncOverloadsSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var interfaceDeclaration in syntaxReceiver.InterfaceCandidates)
			{
				interfaceDeclaration.EnsurePartial();
				interfaceDeclaration.EnsureNamespace(out var @namespace);

				var asyncOverloadMethods = interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Body is null).Select(CreateAsyncOverload);

				var partialInterface = CreatePartialInterface(@namespace, interfaceDeclaration.CreateNewPartialType(), asyncOverloadMethods);
				context.AddSource(interfaceDeclaration.Identifier.Text, partialInterface);
			}

			foreach (var classDeclaration in syntaxReceiver.ClassCandidates)
			{
				classDeclaration.EnsurePartial();
				classDeclaration.EnsureNamespace(out var @namespace);
				if (!classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
				{
					continue;
				}

				var asyncOverloadMethods = classDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
					.Select(CreateAsyncOverload);

				var partialInterface = CreatePartialInterface(@namespace, classDeclaration.CreateNewPartialType(), asyncOverloadMethods);
				context.AddSource(classDeclaration.Identifier.Text, partialInterface);
			}
		}

		private static MethodDeclarationSyntax CreateAsyncOverload(MethodDeclarationSyntax m)
		{
			var newReturnType = m.ReturnType.IsVoid()
				? (TypeSyntax)IdentifierName("Task")
				: GenericName(
						Identifier("Task")
					)
					.WithTypeArgumentList(
						TypeArgumentList(
							SingletonSeparatedList(m.ReturnType)
						)
					);

			return m.WithIdentifier(
					Identifier(m.Identifier.Text + "Async")
				).WithReturnType(newReturnType)
				.AddParameterListParameters(CancellationTokenParameter);
		}

		private static SourceText CreatePartialInterface(string @namespace, TypeDeclarationSyntax typeDeclaration, IEnumerable<MemberDeclarationSyntax> methods)
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
						typeDeclaration
							.WithMembers(
								List(methods)
							)
					)
				)
				.NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
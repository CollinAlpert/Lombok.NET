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

namespace Lombok.NET.MethodGenerators
{
	/// <summary>
	/// Generator which generated async overloads for abstract or interface methods.
	/// </summary>
	[Generator]
	public class AsyncOverloadsGenerator : IIncrementalGenerator
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
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
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
				.SelectMany(l => l.Attributes)
				.Any(a => a.IsNamed("AsyncOverloads"));
		}

		private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			SymbolCache.AsyncOverloadsAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<AsyncOverloadsAttribute>();

			var typeDeclaration = (TypeDeclarationSyntax)context.Node;
			if (!typeDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.AsyncOverloadsAttributeSymbol)
			    // Caught by LOM001, LOM002 and LOM003 
			    || !typeDeclaration.CanGenerateCodeForType(out var @namespace))
			{
				return null;
			}
			
			cancellationToken.ThrowIfCancellationRequested();

			IEnumerable<MemberDeclarationSyntax> asyncOverloads;
			switch (typeDeclaration)
			{
				case InterfaceDeclarationSyntax interfaceDeclaration when interfaceDeclaration.Members.Any():
					asyncOverloads = interfaceDeclaration.Members
						.OfType<MethodDeclarationSyntax>()
						.Where(m => m.Body is null)
						.Select(CreateAsyncOverload);

					return CreatePartialType(@namespace, interfaceDeclaration, asyncOverloads);
				case ClassDeclarationSyntax classDeclaration when classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword):
					asyncOverloads = classDeclaration.Members
						.OfType<MethodDeclarationSyntax>()
						.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
						.Select(CreateAsyncOverload);

					return CreatePartialType(@namespace, classDeclaration, asyncOverloads);
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
			usings.Add("System.Threading.Tasks".CreateUsingDirective());

			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(usings)
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.CreateNewPartialType()
							.WithMembers(
								List(methods)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
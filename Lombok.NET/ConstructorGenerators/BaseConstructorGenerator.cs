using System;
using System.Linq;
using System.Text;
using System.Threading;
using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
#endif

namespace Lombok.NET.ConstructorGenerators
{
	/// <summary>
	/// Base class for source generators which generate constructors.
	/// </summary>
	public abstract class BaseConstructorGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
			var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(s => s != null);
			context.RegisterSourceOutput(sources, (ctx, s) => ctx.AddSource(Guid.NewGuid().ToString(), s!));
		}

		private bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
		{
			TypeDeclarationSyntax? typeDeclaration = node as ClassDeclarationSyntax;
			typeDeclaration ??= node as StructDeclarationSyntax;
			if (typeDeclaration is null || cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			return typeDeclaration.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == AttributeName);
		}

		private SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var typeDeclaration = (TypeDeclarationSyntax)context.Node;
			if (cancellationToken.IsCancellationRequested 
			    || !typeDeclaration.ContainsAttribute(context.SemanticModel, GetAttributeSymbol(context.SemanticModel))
			    // Caught by LOM001, LOM002 and LOM003
			    || !typeDeclaration.CanGenerateCodeForType(out var @namespace))
			{
				return null;
			}
			
			var (constructorParameters, constructorBody) = GetConstructorDetails(typeDeclaration);

			return CreateConstructorCode(@namespace, typeDeclaration, constructorParameters, constructorBody);
		}

		protected abstract (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax typeDeclaration);
		
		/// <summary>
		/// class HiddenAttribute : Attribute
		/// 
		/// ->
		/// 
		/// "Hidden"
		/// </summary>
		protected abstract string AttributeName { get; }

		protected abstract INamedTypeSymbol GetAttributeSymbol(SemanticModel model);

		private static SourceText CreateConstructorCode(string @namespace, TypeDeclarationSyntax typeDeclaration, ParameterListSyntax constructorParameters,
			BlockSyntax constructorBody)
		{
			MemberDeclarationSyntax constructor = ConstructorDeclaration(typeDeclaration.Identifier.Text)
				.WithParameterList(constructorParameters)
				.WithBody(constructorBody)
				.WithModifiers(TokenList(Token(typeDeclaration.GetAccessibilityModifier())));

			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(typeDeclaration.GetUsings())
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.CreateNewPartialType()
							.WithMembers(
							SingletonList(constructor)
						)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
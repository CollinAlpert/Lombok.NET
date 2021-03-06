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
	/// Generator which generates With builder methods for a class.
	/// </summary>
	[Generator]
	public class WithMethodsGenerator : IIncrementalGenerator
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
			return node.IsClass(out var classDeclaration) &&
			       classDeclaration.AttributeLists
				       .SelectMany(l => l.Attributes)
				       .Any(a => a.IsNamed("With"));
		}

		private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			SymbolCache.WithAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<WithAttribute>();

			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			if (!classDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.WithAttributeSymbol)
			    // Caught by LOM001, LOM002 and LOM003 
			    || !classDeclaration.CanGenerateCodeForType(out var @namespace))
			{
				return null;
			}

			cancellationToken.ThrowIfCancellationRequested();

			var memberType = classDeclaration.GetAttributeArgument<MemberType>("With") ?? MemberType.Field;

			var methods = memberType switch
			{
				MemberType.Property => classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
					.Where(p => p.AccessorList != null && p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
					.Select(CreateMethodFromProperty),
				MemberType.Field => classDeclaration.Members.OfType<FieldDeclarationSyntax>()
					.Where(p => !p.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
					.SelectMany(CreateMethodFromField),
				_ => throw new ArgumentOutOfRangeException(nameof(memberType))
			};

			return CreatePartialClass(@namespace, classDeclaration, methods);
		}

		private static MethodDeclarationSyntax CreateMethodFromProperty(PropertyDeclarationSyntax p)
		{
			var parent = (ClassDeclarationSyntax)p.Parent!;
			var method = MethodDeclaration(IdentifierName(parent.Identifier.Text), "With" + p.Identifier.Text);
			var parameter = Parameter(Identifier(p.Identifier.Text.ToCamelCaseIdentifier())).WithType(p.Type);

			return CreateMethod(method, parameter, p.Identifier.Text);
		}

		private static IEnumerable<MethodDeclarationSyntax> CreateMethodFromField(FieldDeclarationSyntax f)
		{
			var parent = (ClassDeclarationSyntax)f.Parent!;

			return f.Declaration.Variables.Select(v =>
				CreateMethod(
					MethodDeclaration(IdentifierName(parent.Identifier.Text), "With" + v.Identifier.Text.ToPascalCaseIdentifier()),
					Parameter(Identifier(v.Identifier.Text.ToCamelCaseIdentifier())).WithType(f.Declaration.Type),
					v.Identifier.Text
				)
			);
		}

		private static MethodDeclarationSyntax CreateMethod(MethodDeclarationSyntax method, ParameterSyntax parameter, string memberName)
		{
			return method.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					parameter
				)
				.WithBody(
					Block(
						ExpressionStatement(
							AssignmentExpression(
								SyntaxKind.SimpleAssignmentExpression,
								IdentifierName(memberName),
								IdentifierName(parameter.Identifier.Text)
							)
						),
						ReturnStatement(
							ThisExpression()
						)
					)
				);
		}

		private static SourceText CreatePartialClass(string @namespace, ClassDeclarationSyntax classDeclaration, IEnumerable<MethodDeclarationSyntax> methods)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithUsings(classDeclaration.GetUsings())
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						classDeclaration.CreateNewPartialClass()
							.WithMembers(
								List<MemberDeclarationSyntax>(methods)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
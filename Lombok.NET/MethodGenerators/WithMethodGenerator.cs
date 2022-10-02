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
        SpinWait.SpinUntil(static () => Debugger.IsAttached);
#endif
		var sources = context.SyntaxProvider.CreateSyntaxProvider(IsCandidate, Transform).Where(static s => s != null);
		context.AddSources(sources);
	}

	private static bool IsCandidate(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node.TryConvertToClass(out var classDeclaration) &&
		       classDeclaration.AttributeLists
			       .SelectMany(static l => l.Attributes)
			       .Any(static a => a.IsNamed("With"));
	}

	private static GeneratorResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		SymbolCache.WithAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<WithAttribute>();

		var classDeclaration = (ClassDeclarationSyntax)context.Node;
		if (!classDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.WithAttributeSymbol))
		{
			return GeneratorResult.Empty;
		}
		
		if (!classDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var memberType = classDeclaration.GetAttributeArgument<MemberType>("With") ?? MemberType.Field;

		var methods = memberType switch
		{
			MemberType.Property => classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
				.Where(static p => p.AccessorList != null && p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
				.Select(CreateMethodFromProperty),
			MemberType.Field => classDeclaration.Members.OfType<FieldDeclarationSyntax>()
				.Where(static p => !p.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
				.SelectMany(CreateMethodFromField),
			_ => throw new ArgumentOutOfRangeException(nameof(memberType))
		};

		var partialClassSourceText = CreatePartialClass(@namespace, classDeclaration, methods);

		return new GeneratorResult(classDeclaration.Identifier.Text, partialClassSourceText);
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
		return CompilationUnit()
			.WithUsings(classDeclaration.GetUsings())
			.WithMembers(
				SingletonList<MemberDeclarationSyntax>(
					FileScopedNamespaceDeclaration(
						IdentifierName(@namespace)
					).WithMembers(
						SingletonList<MemberDeclarationSyntax>(
							classDeclaration.CreateNewPartialClass()
								.WithMembers(
									List<MemberDeclarationSyntax>(methods)
								)
						)
					)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
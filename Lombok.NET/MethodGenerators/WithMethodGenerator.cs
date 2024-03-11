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
public sealed class WithMethodsGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(WithAttribute).FullName;

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
		return node is ClassDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		if (!classDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var memberTypeArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(RequiredArgsConstructorAttribute.MemberType));
		var memberType = (MemberType?)(memberTypeArgument.Value.Value as int?) ?? MemberType.Field;

		var methods = memberType switch
		{
			MemberType.Property => classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
				.Where(static p => p.AccessorList != null && p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration) && !p.Modifiers.Any(SyntaxKind.StaticKeyword))
				.Select(CreateMethodFromProperty),
			MemberType.Field => classDeclaration.Members.OfType<FieldDeclarationSyntax>()
				.Where(static f => !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !f.Modifiers.Any(SyntaxKind.StaticKeyword))
				.SelectMany(CreateMethodFromField),
			_ => throw new ArgumentOutOfRangeException(nameof(memberType))
		};

		var partialClassSourceText = CreatePartialClass(@namespace, classDeclaration, methods);

		return new GeneratorResult(classDeclaration.GetHintName(@namespace), partialClassSourceText);
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
							MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								ThisExpression(),
								IdentifierName(memberName)
							),
							IdentifierName(parameter.Identifier.Text)
						)
					),
					ReturnStatement(
						ThisExpression()
					)
				)
			);
	}

	private static SourceText CreatePartialClass(NameSyntax @namespace, ClassDeclarationSyntax classDeclaration, IEnumerable<MethodDeclarationSyntax> methods)
	{
		return @namespace.CreateNewNamespace(classDeclaration.GetUsings(),
				classDeclaration.CreateNewPartialClass()
					.WithMembers(
						List<MemberDeclarationSyntax>(methods)
					)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
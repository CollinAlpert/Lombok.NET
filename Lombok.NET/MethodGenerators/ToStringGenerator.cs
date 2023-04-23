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
/// Generator which generates a ToString implementation for a type.
/// </summary>
[Generator]
public sealed class ToStringGenerator : IIncrementalGenerator
{
	private static readonly string AttributeName = typeof(ToStringAttribute).FullName;

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
		return node is ClassDeclarationSyntax or StructDeclarationSyntax;
	}

	private static GeneratorResult Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		var typeDeclaration = (TypeDeclarationSyntax)context.TargetNode;
		if (!typeDeclaration.TryValidateType(out var @namespace, out var diagnostic))
		{
			return new GeneratorResult(diagnostic);
		}

		var memberTypeArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(ToStringAttribute.MemberType));
		var accessTypesArgument = context.Attributes[0].NamedArguments.FirstOrDefault(kv => kv.Key == nameof(ToStringAttribute.AccessTypes));
		var memberType = (MemberType?)(memberTypeArgument.Value.Value as int?) ?? MemberType.Field;
		var accessType = (AccessTypes?)(accessTypesArgument.Value.Value as int?) ?? AccessTypes.Private;
		var toStringMethod = CreateToStringMethod((INamedTypeSymbol)context.TargetSymbol, memberType, accessType);
		if (toStringMethod is null)
		{
			return GeneratorResult.Empty;
		}

		cancellationToken.ThrowIfCancellationRequested();

		var sourceText = CreateType(@namespace, typeDeclaration.CreateNewPartialType(), toStringMethod);

		return new GeneratorResult(typeDeclaration.GetHintName(@namespace), sourceText);
	}

	private static MethodDeclarationSyntax? CreateToStringMethod(INamedTypeSymbol typeSymbol, MemberType memberType, AccessTypes accessType)
	{
		var identifiers = memberType == MemberType.Property
			? typeSymbol.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(accessType)
				.Cast<ISymbol>()
				.ToArray()
			: typeSymbol.GetMembers()
				.OfType<IFieldSymbol>()
				.Where(accessType)
				.Cast<ISymbol>()
				.ToArray();

		if (identifiers.Length == 0)
		{
			return null;
		}

		var stringInterpolationContent = new List<InterpolatedStringContentSyntax>
		{
			CreateStringInterpolationContent(string.Concat(typeSymbol.Name, ": ", identifiers[0].Name, "=")),
			GetValueFromSymbol(identifiers[0])
		};

		for (var i = 1; i < identifiers.Length; i++)
		{
			stringInterpolationContent.Add(CreateStringInterpolationContent(string.Concat("; ", identifiers[i].Name, "=")));
			stringInterpolationContent.Add(GetValueFromSymbol(identifiers[i]));
		}

		return MethodDeclaration(
			PredefinedType(
				Token(SyntaxKind.StringKeyword)
			),
			"ToString"
		).WithModifiers(
			TokenList(
				Token(SyntaxKind.PublicKeyword),
				Token(SyntaxKind.OverrideKeyword)
			)
		).WithBody(
			Block(
				ReturnStatement(
					InterpolatedStringExpression(
						Token(SyntaxKind.InterpolatedStringStartToken)
					).WithContents(List(stringInterpolationContent))
				)
			)
		);
	}

	private static InterpolatedStringContentSyntax GetValueFromSymbol(ISymbol symbol)
	{
		if (symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == typeof(MaskedAttribute).FullName))
		{
			return CreateStringInterpolationContent("****");
		}

		return Interpolation(IdentifierName(symbol.Name));
	}

	private static InterpolatedStringContentSyntax CreateStringInterpolationContent(string s)
	{
		return InterpolatedStringText(
			Token(
				TriviaList(),
				SyntaxKind.InterpolatedStringTextToken,
				s,
				s,
				TriviaList()
			)
		);
	}

	private static SourceText CreateType(NameSyntax @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax toStringMethod)
	{
		return @namespace.CreateNewNamespace(
				typeDeclaration.WithMembers(
					new SyntaxList<MemberDeclarationSyntax>(toStringMethod)
				)
			).NormalizeWhitespace()
			.GetText(Encoding.UTF8);
	}
}
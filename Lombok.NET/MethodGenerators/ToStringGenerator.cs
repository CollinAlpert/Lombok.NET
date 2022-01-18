using System;
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
	public class ToStringGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new ToStringSyntaxReceiver());
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!(context.SyntaxContextReceiver is ToStringSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var classDeclaration in syntaxReceiver.ClassCandidates)
			{
				classDeclaration.EnsurePartial();
				classDeclaration.EnsureNamespace(out var @namespace);
				var toStringMethod = CreateToStringMethod(classDeclaration);

				context.AddSource(classDeclaration.Identifier.Text, CreateType(@namespace, classDeclaration.CreateNewPartialType(), toStringMethod));
			}
			
			foreach (var structDeclaration in syntaxReceiver.StructCandidates)
			{
				structDeclaration.EnsurePartial();
				structDeclaration.EnsureNamespace(out var @namespace);
				var toStringMethod = CreateToStringMethod(structDeclaration);

				context.AddSource(structDeclaration.Identifier.Text, CreateType(@namespace, structDeclaration.CreateNewPartialType(), toStringMethod));
			}
		}

		private static MethodDeclarationSyntax CreateToStringMethod(TypeDeclarationSyntax typeDeclaration)
		{
			var memberType = typeDeclaration.GetAttributeArgument<MemberType>("ToString") ?? MemberType.Field;
			var accessType = typeDeclaration.GetAttributeArgument<AccessTypes>("ToString") ?? AccessTypes.Private;

			string[] identifiers;
			switch (memberType)
			{
				case MemberType.Property:
					identifiers = typeDeclaration.Members
						.OfType<PropertyDeclarationSyntax>()
						.Where(accessType)
						.Select(p => p.Identifier.Text)
						.ToArray();

					break;
				case MemberType.Field:
					identifiers = typeDeclaration.Members
						.OfType<FieldDeclarationSyntax>()
						.Where(accessType)
						.SelectMany(f => f.Declaration.Variables.Select(v => v.Identifier.Text))
						.ToArray();

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(memberType));
			}

			var stringInterpolationContent = new List<InterpolatedStringContentSyntax>
			{
				CreateStringInterpolationContent(typeDeclaration.Identifier.Text + ": "),
				CreateStringInterpolationContent(identifiers[0] + "="),
				Interpolation(
					IdentifierName(identifiers[0])
				)
			};

			for (int i = 1; i < identifiers.Length; i++)
			{
				stringInterpolationContent.Add(CreateStringInterpolationContent("; " + identifiers[i] + "="));
				stringInterpolationContent.Add(Interpolation(IdentifierName(identifiers[i])));
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

		private static SourceText CreateType(string @namespace, TypeDeclarationSyntax typeDeclaration, MethodDeclarationSyntax toStringMethod)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						typeDeclaration.WithMembers(
							new SyntaxList<MemberDeclarationSyntax>(toStringMethod)
						)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Lombok.NET
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

			foreach (var typeDeclaration in syntaxReceiver.Candidates)
			{
				if (!(typeDeclaration is ClassDeclarationSyntax classDeclaration))
				{
					throw new NotSupportedException("Only classes are supported for the 'ToString' attribute.");
				}

				if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					throw new NotSupportedException("Class must be partial.");
				}


				var @namespace = classDeclaration.GetNamespace();
				if (@namespace is null)
				{
					throw new Exception($"Namespace could not be found for {classDeclaration.Identifier.Text}.");
				}

				var memberType = classDeclaration.GetAttributeArgument<MemberType>("ToString");
				var accessType = classDeclaration.GetAttributeArgument<AccessTypes>("ToString");

				MethodDeclarationSyntax toStringMethod;
				switch (memberType)
				{
					case MemberType.Property:
						var propertyNames = classDeclaration.Members
							.OfType<PropertyDeclarationSyntax>()
							.Where(accessType)
							.Select(p => p.Identifier.Text)
							.ToArray();
						toStringMethod = CreateToStringMethod(classDeclaration.Identifier.Text, propertyNames);
						break;
					case MemberType.Field:
						var fieldNames = classDeclaration.Members
							.OfType<FieldDeclarationSyntax>()
							.Where(accessType)
							.SelectMany(f => f.Declaration.Variables.Select(v => v.Identifier.Text))
							.ToArray();
						toStringMethod = CreateToStringMethod(classDeclaration.Identifier.Text, fieldNames);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(memberType));
				}

				context.AddSource(classDeclaration.Identifier.Text, CreateClass(@namespace, classDeclaration, toStringMethod));
			}
		}

		private static MethodDeclarationSyntax CreateToStringMethod(string className, string[] identifiers)
		{
			var stringInterpolationContent = new List<InterpolatedStringContentSyntax>
			{
				CreateStringInterpolationContent(className + ": "),
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

		private static SourceText CreateClass(string @namespace, ClassDeclarationSyntax classDeclaration, MethodDeclarationSyntax toStringMethod)
		{
			return NamespaceDeclaration(
					IdentifierName(@namespace)
				).WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						ClassDeclaration(classDeclaration.Identifier.Text)
							.WithModifiers(
								TokenList(
									Token(classDeclaration.GetAccessibilityModifier()),
									Token(SyntaxKind.PartialKeyword)
								)
							).WithMembers(
								new SyntaxList<MemberDeclarationSyntax>(toStringMethod)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
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
	public class WithMethodsGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new WithSyntaxReceiver());
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxContextReceiver is not WithSyntaxReceiver syntaxReceiver)
			{
				return;
			}

			foreach (var classDeclaration in syntaxReceiver.ClassCandidates)
			{
				// Caught by LOM001, LOM002 and LOM003 
				if(!classDeclaration.CanGenerateCodeForType(out var @namespace))
				{
					continue;
				}

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

				context.AddSource(classDeclaration.Identifier.Text, CreatePartialClass(@namespace, classDeclaration.CreateNewPartialType(), methods));
			}
		}

		private static MethodDeclarationSyntax CreateMethodFromProperty(PropertyDeclarationSyntax p)
		{
			var parent = (ClassDeclarationSyntax)p.Parent!;
			var method = MethodDeclaration(IdentifierName(parent.Identifier.Text), "With" + p.Identifier.Text);
			var parameter = Parameter(Identifier(p.Identifier.Text.Decapitalize()!)).WithType(p.Type);

			return CreateMethod(method, parameter, p.Identifier.Text);
		}

		private static IEnumerable<MethodDeclarationSyntax> CreateMethodFromField(FieldDeclarationSyntax f)
		{
			var parent = (ClassDeclarationSyntax)f.Parent!;

			return f.Declaration.Variables.Select(v =>
				CreateMethod(
					MethodDeclaration(IdentifierName(parent.Identifier.Text), "With" + v.Identifier.Text.Substring(1).Capitalize()),
					Parameter(Identifier(v.Identifier.Text.Substring(1))).WithType(f.Declaration.Type),
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
			return NamespaceDeclaration(IdentifierName(@namespace))
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						classDeclaration.WithMembers(
								List<MemberDeclarationSyntax>(methods)
							)
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
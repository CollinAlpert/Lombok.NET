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
			if (!(context.SyntaxContextReceiver is WithSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var typeDeclaration in syntaxReceiver.Candidates)
			{
				if (!(typeDeclaration is ClassDeclarationSyntax classDeclaration))
				{
					throw new NotSupportedException("Only classes are supported for the 'With' attribute.");
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

				var memberType = classDeclaration.GetAttributeArgument<MemberType>("With");

				IEnumerable<MethodDeclarationSyntax> methods;
				switch (memberType)
				{
					case MemberType.Property:
						methods = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
							.Where(p => p.AccessorList != null && p.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
							.Select(CreateMethodFromProperty);
						break;
					case MemberType.Field:
						methods = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
							.Where(p => !p.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
							.SelectMany(CreateMethodFromField);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(memberType));
				}

				context.AddSource(classDeclaration.Identifier.Text, CreatePartialClass(@namespace, classDeclaration, methods));
			}
		}

		private static MethodDeclarationSyntax CreateMethodFromProperty(PropertyDeclarationSyntax p)
		{
			var parent = p.Parent as ClassDeclarationSyntax ?? throw new ArgumentException("Parent is not the original class");
			var method = MethodDeclaration(IdentifierName(parent.Identifier.Text), "With" + p.Identifier.Text);
			var parameter = Parameter(Identifier(p.Identifier.Text.Decapitalize())).WithType(p.Type);
			
			return CreateMethod(method, parameter, p.Identifier.Text);
		}

		private static IEnumerable<MethodDeclarationSyntax> CreateMethodFromField(FieldDeclarationSyntax f)
		{
			var parent = f.Parent as ClassDeclarationSyntax ?? throw new ArgumentException("Parent is not the original class");
			
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
						ClassDeclaration(classDeclaration.Identifier.Text)
							.WithModifiers(TokenList(Token(classDeclaration.GetAccessibilityModifier()), Token(SyntaxKind.PartialKeyword)))
							.WithMembers(List<MemberDeclarationSyntax>(methods))
					)
				).NormalizeWhitespace()
				.GetText(Encoding.UTF8);
		}
	}
}
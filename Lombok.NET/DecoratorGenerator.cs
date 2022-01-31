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

namespace Lombok.NET
{
	/// <summary>
	/// Generator which generates the decorator subclasses for abstract classes or interfaces.
	/// </summary>
	[Generator]
	public class DecoratorGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new DecoratorSyntaxReceiver());
#if DEBUG
			SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxContextReceiver is not DecoratorSyntaxReceiver syntaxReceiver)
			{
				return;
			}

			foreach (var classDeclaration in syntaxReceiver.ClassCandidates)
			{
				if (!classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
				{
					throw new Exception($"{classDeclaration.Identifier.Text} is not abstract, thus a decorator subclass cannot be generated.");
				}

				var subclass = CreateSubclass(classDeclaration);
				context.AddSource($"{classDeclaration.Identifier.Text}Decorator", subclass);
			}

			foreach (var interfaceDeclaration in syntaxReceiver.InterfaceCandidates)
			{
				var subclass = CreateSubclass(interfaceDeclaration);
				context.AddSource($"{interfaceDeclaration.Identifier.Text}Decorator", subclass);
			}
		}

		private static SourceText CreateSubclass(ClassDeclarationSyntax classDeclaration)
		{
			classDeclaration.EnsureNamespace(out var @namespace);

			var methods = classDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
				.Select(m => m.WithModifiers(m.Modifiers.Replace(m.Modifiers[m.Modifiers.IndexOf(SyntaxKind.AbstractKeyword)],
					Token(SyntaxKind.OverrideKeyword))));

			return CreateDecoratorCode(@namespace, classDeclaration, methods);
		}

		private static SourceText CreateSubclass(InterfaceDeclarationSyntax interfaceDeclaration)
		{
			interfaceDeclaration.EnsureNamespace(out var @namespace);

			var methods = interfaceDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Body is null)
				.Select(m => m.WithModifiers(m.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword)).Insert(1, Token(SyntaxKind.VirtualKeyword))));

			return CreateDecoratorCode(@namespace, interfaceDeclaration, methods);
		}

		private static SourceText CreateDecoratorCode(string @namespace, TypeDeclarationSyntax type, IEnumerable<MethodDeclarationSyntax> methods)
		{
			string typeName;
			switch (type)
			{
				case InterfaceDeclarationSyntax _ when type.Identifier.Text.StartsWith("I"):
					typeName = type.Identifier.Text.Substring(1);

					break;
				default:
					typeName = type.Identifier.Text;

					break;
			}

			var variableName = char.ToLower(typeName[0]) + typeName.Substring(1);

			var memberVariableName = "_" + variableName;
			methods = methods.Select(m =>
			{
				m = m.WithSemicolonToken(Token(SyntaxKind.None));
				if (m.ReturnType.IsVoid())
				{
					return m.WithBody(Block(
							SingletonList<StatementSyntax>(
								ExpressionStatement(
									InvocationExpression(
										MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(memberVariableName), IdentifierName(m.Identifier))
									)
								)
							)
						)
					);
				}

				return m.WithBody(Block(
						SingletonList<StatementSyntax>(
							ReturnStatement(
								InvocationExpression(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(memberVariableName), IdentifierName(m.Identifier))
								)
							)
						)
					)
				);
			});

			return NamespaceDeclaration(IdentifierName(@namespace))
				.WithMembers(
					SingletonList<MemberDeclarationSyntax>(
						ClassDeclaration($"{typeName}Decorator")
							.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
							.WithBaseList(
								BaseList(
									SingletonSeparatedList<BaseTypeSyntax>(
										SimpleBaseType(
											IdentifierName(type.Identifier.Text)
										)
									)
								)
							)
							.WithMembers(
								List(
									new MemberDeclarationSyntax[]
									{
										FieldDeclaration(
											VariableDeclaration(IdentifierName(type.Identifier))
												.WithVariables(
													SingletonSeparatedList(
														VariableDeclarator(
															Identifier(memberVariableName)
														)
													)
												)
										).WithModifiers(
											TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
										),
										ConstructorDeclaration(
												Identifier($"{typeName}Decorator"))
											.WithModifiers(
												TokenList(
													type.Modifiers.Where(IsAccessModifier).Cast<SyntaxToken?>().FirstOrDefault() ?? Token(SyntaxKind.InternalKeyword)
												)
											).WithParameterList(
												ParameterList(
													SingletonSeparatedList(
														Parameter(
																Identifier(variableName))
															.WithType(
																IdentifierName(type.Identifier)
															)
													)
												)
											).WithBody(
												Block(
													SingletonList<StatementSyntax>(
														ExpressionStatement(
															AssignmentExpression(
																SyntaxKind.SimpleAssignmentExpression,
																IdentifierName(memberVariableName),
																IdentifierName(variableName)
															)
														)
													)
												)
											)
									}.Concat(methods)
								)
							)
					)
				).NormalizeWhitespace().GetText(Encoding.UTF8);
		}

		private static bool IsAccessModifier(SyntaxToken token)
		{
			return token.IsKind(SyntaxKind.PublicKeyword)
			       || token.IsKind(SyntaxKind.ProtectedKeyword)
			       || token.IsKind(SyntaxKind.PrivateKeyword)
			       || token.IsKind(SyntaxKind.InternalKeyword);
		}
	}
}
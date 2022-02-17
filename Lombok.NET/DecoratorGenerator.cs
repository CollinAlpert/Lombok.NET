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

namespace Lombok.NET
{
	/// <summary>
	/// Generator which generates the decorator subclasses for abstract classes or interfaces.
	/// </summary>
	[Generator]
	public class DecoratorGenerator : IIncrementalGenerator
	{
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
			TypeDeclarationSyntax? typeDeclaration = node as InterfaceDeclarationSyntax;
			typeDeclaration ??= node as ClassDeclarationSyntax;
			if (typeDeclaration is null || cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			return typeDeclaration.AttributeLists
				.SelectMany(l => l.Attributes)
				.Any(a => a.Name is IdentifierNameSyntax name && name.Identifier.Text == "Decorator");
		}

		private static SourceText? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			SymbolCache.DecoratorAttributeSymbol ??= context.SemanticModel.Compilation.GetSymbolByType<DecoratorAttribute>();
			
			var typeDeclaration = (TypeDeclarationSyntax)context.Node;
			var @namespace = typeDeclaration.GetNamespace();
			if (cancellationToken.IsCancellationRequested
			    || !typeDeclaration.ContainsAttribute(context.SemanticModel, SymbolCache.DecoratorAttributeSymbol)
			    // Caught by LOM003 
			    || @namespace is null)
			{
				return null;
			}

			return typeDeclaration switch
			{
				ClassDeclarationSyntax classDeclaration => CreateSubclass(@namespace, classDeclaration),
				InterfaceDeclarationSyntax interfaceDeclaration => CreateSubclass(@namespace, interfaceDeclaration),
				_ => null
			};
		}

		private static SourceText CreateSubclass(string @namespace, ClassDeclarationSyntax classDeclaration)
		{
			var methods = classDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
				.Select(m => m.WithModifiers(m.Modifiers.Replace(m.Modifiers[m.Modifiers.IndexOf(SyntaxKind.AbstractKeyword)],
					Token(SyntaxKind.OverrideKeyword))));

			return CreateDecoratorCode(@namespace, classDeclaration, methods);
		}

		private static SourceText CreateSubclass(string @namespace, InterfaceDeclarationSyntax interfaceDeclaration)
		{
			var methods = interfaceDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Body is null)
				.Select(m => m.WithModifiers(m.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword)).Insert(1, Token(SyntaxKind.VirtualKeyword))));

			return CreateDecoratorCode(@namespace, interfaceDeclaration, methods);
		}

		private static SourceText CreateDecoratorCode(string @namespace, TypeDeclarationSyntax type, IEnumerable<MethodDeclarationSyntax> methods)
		{
			var typeName = type switch
			{
				InterfaceDeclarationSyntax _ when type.Identifier.Text.StartsWith("I") => type.Identifier.Text.Substring(1),
				_ => type.Identifier.Text
			};

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
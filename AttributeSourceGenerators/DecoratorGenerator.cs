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
using System.Threading;
using System.Diagnostics;
#endif

namespace AttributeSourceGenerators
{
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
			if (!(context.SyntaxReceiver is DecoratorSyntaxReceiver syntaxReceiver))
			{
				return;
			}

			foreach (var typeDeclaration in syntaxReceiver.Candidates)
			{
				if (typeDeclaration is ClassDeclarationSyntax classDeclaration)
				{
					if (!typeDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
					{
						throw new Exception($"{classDeclaration.Identifier.Text} is not abstract, thus a decorator subclass cannot be generated.");
					}

					var subclass = CreateSubclassFromAbstractClass(classDeclaration);
					context.AddSource($"{classDeclaration.Identifier.Text}Decorator", subclass);
				}
				else if (typeDeclaration is InterfaceDeclarationSyntax interfaceDeclaration)
				{
					var subclass = CreateSubclassFromInterface(interfaceDeclaration);
					context.AddSource($"{interfaceDeclaration.Identifier.Text}Decorator", subclass);
				}
				else
				{
					throw new Exception($"{typeDeclaration.Identifier.Text} must be an abstract class or an interface in order to generate a decorator for it.");
				}
			}
		}

		private static SourceText CreateSubclassFromAbstractClass(ClassDeclarationSyntax classDeclaration)
		{
			var @namespace = classDeclaration.GetNamespace();
			if (@namespace is null)
			{
				throw new Exception($"Namespace could not be found for {classDeclaration.Identifier.Text}.");
			}

			var methods = classDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword))
				.Select(m => m.WithModifiers(m.Modifiers.Replace(m.Modifiers[m.Modifiers.IndexOf(SyntaxKind.AbstractKeyword)],
					Token(SyntaxKind.OverrideKeyword))));

			return CreateDecoratorCode(@namespace, classDeclaration, methods);
		}

		private static SourceText CreateSubclassFromInterface(InterfaceDeclarationSyntax interfaceDeclaration)
		{
			var @namespace = interfaceDeclaration.GetNamespace();
			if (@namespace is null)
			{
				throw new Exception($"Namespace could not be found for {interfaceDeclaration.Identifier.Text}.");
			}

			var methods = interfaceDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Body is null)
				.Select(m => m.WithModifiers(m.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword))));

			return CreateDecoratorCode(@namespace, interfaceDeclaration, methods);
		}

		private static SourceText CreateDecoratorCode(string @namespace, TypeDeclarationSyntax type, IEnumerable<MethodDeclarationSyntax> methods)
		{
			var typeName = type switch
			{
				InterfaceDeclarationSyntax _ when type.Identifier.Text.StartsWith('I') => type.Identifier.Text[1..],
				_ => type.Identifier.Text
			};
			var variableName = char.ToLower(typeName[0]) + typeName[1..];

			var memberVariableName = "_" + variableName;
			methods = methods.Select(m =>
			{
				m = m.WithSemicolonToken(Token(SyntaxKind.None));
				if (m.ReturnType.IsKind(SyntaxKind.VoidKeyword))
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
				/*var modifierString = string.Join(" ", m.Modifiers.Select(mod => mod.Text));
				var parameterString = string.Join(", ", m.ParameterList.Parameters.Select(p => $"{p.Type!.ToString()} {p.Identifier.Text}"));
				var argumentString = string.Join(", ", m.ParameterList.Parameters.Select(p => p.Identifier.Text));
				var returnKeyword = m.ReturnType.IsKind(SyntaxKind.VoidKeyword) ? string.Empty : "return ";

				return $@"
        {modifierString} {m.ReturnType.ToString()} {m.Identifier.Text}({parameterString}) {{
            {returnKeyword}{memberVariableName}.{m.Identifier.Text}({argumentString});
        }}
";*/
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
											)
											.WithModifiers(
												TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword))
											),
										ConstructorDeclaration(
												Identifier($"{typeName}Decorator"))
											.WithModifiers(
												TokenList(
													Token(SyntaxKind.ProtectedKeyword)))
											.WithParameterList(
												ParameterList(
													SingletonSeparatedList(
														Parameter(
																Identifier(variableName))
															.WithType(
																IdentifierName(type.Identifier)
															)
													)
												)
											)
											.WithBody(
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

			/*var body = string.Join(Environment.NewLine, methodsList);

			var code = $@"
namespace {@namespace} {{
    public class {typeName}Decorator : {baseTypeName} {{
        private readonly {baseTypeName} {memberVariableName};

        protected {typeName}Decorator({baseTypeName} {variableName}) {{
            {memberVariableName} = {variableName};
        }}
        {body}
    }}
}}
";

			return code;*/
		}
	}
}
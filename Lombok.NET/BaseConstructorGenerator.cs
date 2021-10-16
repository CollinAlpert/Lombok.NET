using System;
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
    public abstract class BaseConstructorGenerator : ISourceGenerator
    {
        protected abstract BaseAttributeSyntaxReceiver SyntaxReceiver { get; }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => SyntaxReceiver);
#if DEBUG
            SpinWait.SpinUntil(() => Debugger.IsAttached);
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver == null || context.SyntaxReceiver.GetType() != SyntaxReceiver.GetType())
            {
                return;
            }

            foreach (var classDeclaration in SyntaxReceiver.Candidates)
            {
                if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    throw new NotSupportedException("Class must be partial.");
                }

                var @namespace = classDeclaration.GetNamespace();
                if (@namespace is null)
                {
                    throw new Exception($"Namespace could not be found for {classDeclaration.Identifier.Text}.");
                }
                
                var className = classDeclaration.Identifier.Text;
                var (constructorParameters, constructorBody) = GetConstructorDetails(classDeclaration);

                context.AddSource(className, CreateConstructorCode(@namespace, className, constructorParameters, constructorBody));
            }
        }

        protected abstract (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax typeDeclaration);

        private static SourceText CreateConstructorCode(string @namespace, string className, ParameterListSyntax constructorParameters, BlockSyntax constructorBody)
        {
            MemberDeclarationSyntax constructor = ConstructorDeclaration(className)
                .WithParameterList(constructorParameters)
                .WithBody(constructorBody)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
            return NamespaceDeclaration(IdentifierName(@namespace)).WithMembers(
                SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(className)
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(SingletonList(constructor))
                )
            ).NormalizeWhitespace().GetText(Encoding.UTF8);
        }
    }
}
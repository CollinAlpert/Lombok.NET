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
    /// <summary>
    /// Base class for source generators which generate constructors.
    /// </summary>
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
            if (context.SyntaxContextReceiver == null || context.SyntaxContextReceiver.GetType() != SyntaxReceiver.GetType())
            {
                return;
            }

            foreach (var typeDeclaration in SyntaxReceiver.Candidates)
            {
                if (!(typeDeclaration is ClassDeclarationSyntax classDeclaration))
                {
                    throw new NotSupportedException("Constructors can only be generated for classes.");    
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
                
                var className = classDeclaration.Identifier.Text;
                var (constructorParameters, constructorBody) = GetConstructorDetails(classDeclaration);

                context.AddSource(className, CreateConstructorCode(@namespace, classDeclaration, constructorParameters, constructorBody));
            }
        }

        protected abstract (ParameterListSyntax constructorParameters, BlockSyntax constructorBody) GetConstructorDetails(TypeDeclarationSyntax typeDeclaration);

        private static SourceText CreateConstructorCode(string @namespace, ClassDeclarationSyntax classDeclaration, ParameterListSyntax constructorParameters, BlockSyntax constructorBody)
        {
            MemberDeclarationSyntax constructor = ConstructorDeclaration(classDeclaration.Identifier.Text)
                .WithParameterList(constructorParameters)
                .WithBody(constructorBody)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));
            return NamespaceDeclaration(IdentifierName(@namespace)).WithMembers(
                SingletonList<MemberDeclarationSyntax>(
                    ClassDeclaration(classDeclaration.Identifier.Text)
                        .WithModifiers(TokenList(Token(classDeclaration.GetAccessibilityModifier()), Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(SingletonList(constructor))
                )
            ).NormalizeWhitespace().GetText(Encoding.UTF8);
        }
    }
}
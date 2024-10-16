using Lombok.NET.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.MethodGenerators;

/// <summary>
/// Provides an abstraction over creating "With" methods from a type's members.
/// </summary>
/// <typeparam name="TSymbol">The type of member to generate methods from (field or property).</typeparam>
internal abstract class WithMethodProvider<TSymbol>
    where TSymbol : ISymbol
{
    /// <summary>
    /// Generates a list of <see cref="MethodDeclarationSyntax"/> for all members in a type. 
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="includeInheritedMembers">Whether to include a type's inherited members.</param>
    /// <returns>A list of "With" methods.</returns>
    public IEnumerable<MethodDeclarationSyntax> Generate(INamedTypeSymbol type, bool includeInheritedMembers)
    {
        string returnType = type.GetFullName();
        foreach (var member in type.GetMembers().OfType<TSymbol>().Where(IsCandidate))
        {
            yield return CreateMethod(member, returnType);
        }

        if (includeInheritedMembers)
        {
            var baseType = type.BaseType;
            while (baseType is not null)
            {
                foreach (var member in baseType.GetMembers().OfType<TSymbol>().Where(IsCandidate))
                {
                    yield return CreateMethod(member, returnType);
                }

                baseType = baseType.BaseType;
            }
        }
    }

    protected abstract MethodDeclarationSyntax CreateMethod(TSymbol property, string returnType);

    protected abstract bool IsCandidate(TSymbol symbol);
    
    protected MethodDeclarationSyntax CreateMethod(MethodDeclarationSyntax method, ParameterSyntax parameter, string memberName)
    {
        return method.AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                parameter
            ).WithBody(
                Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                IdentifierName(memberName)
                            ),
                            IdentifierName(parameter.Identifier.Text.EscapeReservedKeyword())
                        )
                    ),
                    ReturnStatement(
                        ThisExpression()
                    )
                )
            );
    }
}

internal sealed class WithMethodFieldProvider : WithMethodProvider<IFieldSymbol>
{
    protected override MethodDeclarationSyntax CreateMethod(IFieldSymbol field, string returnType)
    {
        return CreateMethod(
            MethodDeclaration(
                IdentifierName(returnType),
                "With" + field.Name.ToPascalCaseIdentifier()
            ),
            Parameter(
                Identifier(field.Name.ToCamelCaseIdentifier().EscapeReservedKeyword())
            ).WithType(
                GetTypeSyntax(field) ?? IdentifierName(field.Type.ToString())
            ),
            field.Name
        );
    }

    protected override bool IsCandidate(IFieldSymbol field) => !field.IsReadOnly && !field.IsStatic && field.AssociatedSymbol is null;

    private static TypeSyntax? GetTypeSyntax(IFieldSymbol field)
    {
        if (field.DeclaringSyntaxReferences.Length > 0
            && field.DeclaringSyntaxReferences[0].GetSyntax() is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } declaration })
        {
            return declaration.Type;
        }

        return null;
    }
}

internal sealed class WithMethodPropertyProvider : WithMethodProvider<IPropertySymbol>
{
    protected override MethodDeclarationSyntax CreateMethod(IPropertySymbol property, string returnType)
    {
        var method = MethodDeclaration(IdentifierName(returnType), "With" + property.Name);
        var parameter = Parameter(
            Identifier(property.Name.ToCamelCaseIdentifier().EscapeReservedKeyword())
        ).WithType(
            GetTypeSyntax(property) ?? IdentifierName(property.Type.ToString())
        );

        return CreateMethod(method, parameter, property.Name);
    }

    protected override bool IsCandidate(IPropertySymbol property)
    {
        return property.SetMethod is not null
               && !property.SetMethod.IsInitOnly
               && !property.IsStatic;
    }

    private static TypeSyntax? GetTypeSyntax(IPropertySymbol property)
    {
        if (property.DeclaringSyntaxReferences.Length > 0
            && property.DeclaringSyntaxReferences[0].GetSyntax() is PropertyDeclarationSyntax propertyDeclaration)
        {
            return propertyDeclaration.Type;
        }

        return null;
    }
}
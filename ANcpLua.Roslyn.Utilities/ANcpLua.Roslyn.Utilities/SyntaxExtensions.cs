using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for Roslyn syntax types.
/// </summary>
public static class
    SyntaxExtensions
{
    /// <summary>
    ///     Gets the method name from an invocation expression.
    /// </summary>
    public static string? GetMethodName(this InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    /// <summary>
    ///     Gets the identifier name from an expression.
    /// </summary>
    public static string? GetIdentifierName(this ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };

    /// <summary>
    ///     Checks if a member declaration has a specific modifier.
    /// </summary>
    public static bool HasModifier(this MemberDeclarationSyntax member, SyntaxKind kind) => member.Modifiers.Any(kind);

    /// <summary>
    ///     Checks if a type declaration has the partial modifier.
    /// </summary>
    public static bool IsPartial(this TypeDeclarationSyntax type) => type.Modifiers.Any(SyntaxKind.PartialKeyword);

    /// <summary>
    ///     Checks if a type declaration is a primary constructor type (class, struct, or record with parameter list).
    /// </summary>
    public static bool IsPrimaryConstructorType(this TypeDeclarationSyntax type) =>
        type is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax
        && type.ParameterList is not null;

    /// <summary>
    ///     Gets the name token location for a type declaration.
    /// </summary>
    public static Location GetNameLocation(this TypeDeclarationSyntax type) => type.Identifier.GetLocation();

    /// <summary>
    ///     Gets the name token location for a method declaration.
    /// </summary>
    public static Location GetNameLocation(this MethodDeclarationSyntax method) => method.Identifier.GetLocation();
}
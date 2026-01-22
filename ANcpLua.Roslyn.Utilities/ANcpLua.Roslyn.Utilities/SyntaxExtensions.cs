using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for Roslyn syntax types.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods for working with C# syntax nodes, including
///         expression analysis, modifier checking, and location extraction.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Extract method and identifier names from expressions</description>
///         </item>
///         <item>
///             <description>Check for specific modifiers on member declarations</description>
///         </item>
///         <item>
///             <description>Detect partial types and primary constructor types</description>
///         </item>
///         <item>
///             <description>Retrieve name token locations for diagnostics</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="InvocationExpressionSyntax" />
/// <seealso cref="MemberDeclarationSyntax" />
/// <seealso cref="TypeDeclarationSyntax" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SyntaxExtensions
{
    /// <summary>
    ///     Gets the method name from an invocation expression.
    /// </summary>
    /// <param name="invocation">The invocation expression to extract the method name from.</param>
    /// <returns>
    ///     The method name as a string, or <c>null</c> if the method name cannot be determined
    ///     from the expression syntax.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method handles both member access expressions (e.g., <c>obj.Method()</c>)
    ///         and simple identifier expressions (e.g., <c>Method()</c>).
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For member access expressions, returns the name portion (e.g., "Method" from "obj.Method")</description>
    ///         </item>
    ///         <item>
    ///             <description>For identifier expressions, returns the identifier text directly</description>
    ///         </item>
    ///         <item>
    ///             <description>For other expression types, returns <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetIdentifierName" />
    public static string? GetMethodName(this InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    ///     Gets the identifier name from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract the identifier name from.</param>
    /// <returns>
    ///     The identifier name as a string, or <c>null</c> if the identifier cannot be determined
    ///     from the expression syntax.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method extracts identifier text from both simple identifier expressions
    ///         and member access expressions.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For identifier expressions (e.g., <c>foo</c>), returns the identifier text</description>
    ///         </item>
    ///         <item>
    ///             <description>For member access expressions (e.g., <c>obj.Property</c>), returns the member name</description>
    ///         </item>
    ///         <item>
    ///             <description>For other expression types, returns <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetMethodName" />
    public static string? GetIdentifierName(this ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    ///     Checks if a member declaration has a specific modifier.
    /// </summary>
    /// <param name="member">The member declaration to check.</param>
    /// <param name="kind">The <see cref="SyntaxKind" /> of the modifier to look for.</param>
    /// <returns>
    ///     <c>true</c> if the member has the specified modifier; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Common modifier kinds include <see cref="SyntaxKind.PublicKeyword" />,
    ///         <see cref="SyntaxKind.PrivateKeyword" />, <see cref="SyntaxKind.StaticKeyword" />,
    ///         <see cref="SyntaxKind.AsyncKeyword" />, and <see cref="SyntaxKind.PartialKeyword" />.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Access modifiers: <see cref="SyntaxKind.PublicKeyword" />,
    ///                 <see cref="SyntaxKind.PrivateKeyword" />, <see cref="SyntaxKind.ProtectedKeyword" />,
    ///                 <see cref="SyntaxKind.InternalKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Member modifiers: <see cref="SyntaxKind.StaticKeyword" />,
    ///                 <see cref="SyntaxKind.ReadOnlyKeyword" />, <see cref="SyntaxKind.ConstKeyword" />,
    ///                 <see cref="SyntaxKind.VolatileKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Inheritance modifiers: <see cref="SyntaxKind.AbstractKeyword" />,
    ///                 <see cref="SyntaxKind.VirtualKeyword" />, <see cref="SyntaxKind.OverrideKeyword" />,
    ///                 <see cref="SyntaxKind.SealedKeyword" />, <see cref="SyntaxKind.NewKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Async/unsafe: <see cref="SyntaxKind.AsyncKeyword" />, <see cref="SyntaxKind.UnsafeKeyword" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Type modifiers: <see cref="SyntaxKind.PartialKeyword" />,
    ///                 <see cref="SyntaxKind.FileKeyword" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for async methods in an analyzer
    /// if (methodDeclaration.HasModifier(SyntaxKind.AsyncKeyword))
    /// {
    ///     // Analyze async-specific patterns
    /// }
    /// 
    /// // Check for static members
    /// if (memberDeclaration.HasModifier(SyntaxKind.StaticKeyword))
    /// {
    ///     // Apply static member rules
    /// }
    /// 
    /// // Check for virtual methods that could be overridden
    /// if (methodDeclaration.HasModifier(SyntaxKind.VirtualKeyword) ||
    ///     methodDeclaration.HasModifier(SyntaxKind.AbstractKeyword))
    /// {
    ///     // Method can be overridden in derived classes
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsPartial" />
    public static bool HasModifier(this MemberDeclarationSyntax member, SyntaxKind kind) => member.Modifiers.Any(kind);

    /// <summary>
    ///     Checks if a type declaration has the partial modifier.
    /// </summary>
    /// <param name="type">The type declaration to check.</param>
    /// <returns>
    ///     <c>true</c> if the type has the <c>partial</c> modifier; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Partial types are commonly required for source generators to augment existing types
    ///         with generated members.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HasModifier" />
    /// <seealso cref="IsPrimaryConstructorType" />
    public static bool IsPartial(this TypeDeclarationSyntax type) => type.Modifiers.Any(SyntaxKind.PartialKeyword);

    /// <summary>
    ///     Checks if a type declaration is a primary constructor type.
    /// </summary>
    /// <param name="type">The type declaration to check.</param>
    /// <returns>
    ///     <c>true</c> if the type is a class, struct, or record with a primary constructor
    ///     (parameter list); otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Primary constructors were introduced in C# 12 for classes and structs,
    ///         and have been available for records since C# 9. A primary constructor is
    ///         declared by adding a parameter list directly after the type name.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Applies to <see cref="ClassDeclarationSyntax" />, <see cref="StructDeclarationSyntax" />, and
    ///                 <see cref="RecordDeclarationSyntax" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Returns <c>true</c> only if the type has a non-null
    ///                 <see cref="TypeDeclarationSyntax.ParameterList" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Does not apply to interfaces or other type declarations</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsPartial" />
    public static bool IsPrimaryConstructorType(this TypeDeclarationSyntax type) =>
        type is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax
        && type.ParameterList is not null;

    /// <summary>
    ///     Gets the name token location for a type declaration.
    /// </summary>
    /// <param name="type">The type declaration to get the name location from.</param>
    /// <returns>
    ///     The <see cref="Location" /> of the type's identifier token.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is useful for reporting diagnostics that should point to the type name
    ///         rather than the entire type declaration.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetNameLocation(MethodDeclarationSyntax)" />
    public static Location GetNameLocation(this TypeDeclarationSyntax type) => type.Identifier.GetLocation();

    /// <summary>
    ///     Gets the name token location for a method declaration.
    /// </summary>
    /// <param name="method">The method declaration to get the name location from.</param>
    /// <returns>
    ///     The <see cref="Location" /> of the method's identifier token.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is useful for reporting diagnostics that should point to the method name
    ///         rather than the entire method declaration.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetNameLocation(TypeDeclarationSyntax)" />
    public static Location GetNameLocation(this MethodDeclarationSyntax method) => method.Identifier.GetLocation();
}

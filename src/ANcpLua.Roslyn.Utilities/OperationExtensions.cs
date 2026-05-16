using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using IOperation = Microsoft.CodeAnalysis.IOperation;
using ISymbol = Microsoft.CodeAnalysis.ISymbol;
using ITypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;
using OperationKind = Microsoft.CodeAnalysis.OperationKind;
using SymbolEqualityComparer = Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for working with <see cref="Microsoft.CodeAnalysis.IOperation" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Utilities for navigating, analyzing, and querying the operation tree in Roslyn analyzers.
///         The implementation is split across partial files by responsibility:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This file: ancestor navigation, <c>nameof</c> / expression-tree /
///             static-context checks</description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.Unwrap.cs</c> — conversion / parenthesized /
///             labeled-operation unwrapping + <see cref="GetActualType" /></description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.Constants.cs</c> — constant-value predicates and
///             extractors (<see cref="IsConstantZero" />, <see cref="TryGetConstantValue{T}" />, …)</description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.Traversal.cs</c> — descendants enumeration plus the
///             IsInside* family that shares a single <c>HasAncestor(predicate)</c> chokepoint</description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.SymbolContext.cs</c> —
///             <see cref="GetContainingMethod" />, <see cref="GetContainingType" />,
///             <see cref="GetCSharpLanguageVersion" /></description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.Assignment.cs</c> — assignment-target / by-ref
///             detection</description>
///         </item>
///         <item>
///             <description><c>OperationExtensions.Naming.cs</c> — human-readable name extraction
///             (<see cref="GetOperandName" />, <see cref="GetCollectionSourceName" />)</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Microsoft.CodeAnalysis.IOperation" />
/// <seealso cref="InvocationExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
    /// <summary>
    ///     Enumerates all ancestor operations of the specified operation.
    /// </summary>
    /// <param name="operation">The operation whose ancestors to enumerate.</param>
    /// <returns>
    ///     An enumerable sequence of ancestor operations, starting from the immediate parent
    ///     and proceeding upward to the root of the operation tree.
    /// </returns>
    /// <remarks>
    ///     The enumeration does not include <paramref name="operation" /> itself.
    /// </remarks>
    /// <seealso cref="FindAncestor{T}" />
    /// <seealso cref="IsDescendantOf{T}" />
    public static IEnumerable<IOperation> Ancestors(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    /// <summary>
    ///     Finds the first ancestor operation of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of operation to find. Must be a class implementing <see cref="IOperation" />.</typeparam>
    /// <param name="operation">The operation whose ancestors to search.</param>
    /// <returns>
    ///     The first ancestor of type <typeparamref name="T" />, or <c>null</c> if no such ancestor exists.
    /// </returns>
    /// <seealso cref="Ancestors" />
    /// <seealso cref="IsDescendantOf{T}" />
    public static T? FindAncestor<T>(this IOperation operation) where T : class, IOperation
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is T result)
                return result;
            parent = parent.Parent;
        }

        return null;
    }

    /// <summary>
    ///     Determines whether the operation is a descendant of an operation of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of ancestor operation to check for. Must be a class implementing <see cref="IOperation" />.
    /// </typeparam>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if an ancestor of type <typeparamref name="T" /> exists; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="FindAncestor{T}" />
    public static bool IsDescendantOf<T>(this IOperation operation) where T : class, IOperation
    {
        return operation.FindAncestor<T>() is not null;
    }

    /// <summary>
    ///     Determines whether the operation is inside a <c>nameof</c> expression.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>nameof</c> expression; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Operations inside <c>nameof</c> expressions are not evaluated at runtime, so analyzers
    ///     may want to skip certain checks for these operations.
    /// </remarks>
    public static bool IsInNameofOperation(this IOperation operation)
    {
        foreach (var ancestor in operation.Ancestors())
            if (ancestor.Kind is OperationKind.NameOf)
                return true;
        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside an expression tree.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="expressionSymbol">
    ///     The <see cref="INamedTypeSymbol" /> for <c>System.Linq.Expressions.Expression`1</c>
    ///     (the open generic type), or <c>null</c> if unavailable.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation is within an expression tree; otherwise, <c>false</c>.
    ///     Returns <c>false</c> if <paramref name="expressionSymbol" /> is <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Expression trees have different runtime semantics than regular code, as they are
    ///         compiled to data structures rather than executable code. Analyzers may need to
    ///         skip or modify checks for operations within expression trees.
    ///     </para>
    /// </remarks>
    public static bool IsInExpressionTree(this IOperation operation, INamedTypeSymbol? expressionSymbol)
    {
        if (expressionSymbol is null)
            return false;

        foreach (var op in operation.Ancestors())
            if (HasExpressionType(op, expressionSymbol))
                return true;

        return false;
    }

    // Pulled out so IsInExpressionTree reads as "any ancestor has an expression type"
    // rather than "any ancestor is one of these two shapes with this property check".
    // Each of the four shapes that carry a type contributes one switch arm.
    private static bool HasExpressionType(IOperation op, ISymbol expressionSymbol)
    {
        var type = op switch
        {
            IArgumentOperation { Parameter.Type: { } pt } => pt,
            IConversionOperation { Type: { } ct } => ct,
            _ => null
        };

        return type is not null && IsConstructedFromExpressionType(type, expressionSymbol);
    }

    /// <summary>
    ///     Checks if a type is constructed from the <c>Expression&lt;T&gt;</c> open generic type
    ///     or inherits from such a type.
    /// </summary>
    private static bool IsConstructedFromExpressionType(ITypeSymbol type, ISymbol expressionSymbol)
    {
        if (type is INamedTypeSymbol namedType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, expressionSymbol))
            return true;

        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, expressionSymbol))
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is in a static context.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a static member (method, property, local function,
    ///     static lambda, or field initializer); otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Walks up the syntax tree to find enclosing members and checks their static modifier.
    ///         For local functions and lambdas, checks the <c>static</c> keyword. Field declarations
    ///         are assumed to be in a static context for safety.
    ///     </para>
    /// </remarks>
    public static bool IsInStaticContext(this IOperation operation, CancellationToken cancellationToken)
    {
        foreach (var member in operation.Syntax.Ancestors())
        {
            var result = ClassifyStaticContext(member, operation, cancellationToken);
            if (result.HasValue)
                return result.Value;
        }

        return false;
    }

    // Returns:
    //   true  — definitively in a static context (stop searching, answer is yes)
    //   false — definitively NOT in a static context (stop searching, answer is no)
    //   null  — this ancestor is irrelevant or a non-static enclosing scope that doesn't
    //           by itself determine the answer; keep searching outward
    private static bool? ClassifyStaticContext(
        Microsoft.CodeAnalysis.SyntaxNode member,
        IOperation operation,
        CancellationToken ct)
    {
        var model = operation.SemanticModel;

        return member switch
        {
            LocalFunctionStatementSyntax lf when model?.GetDeclaredSymbol(lf, ct) is { IsStatic: true }
                => true,
            LambdaExpressionSyntax lam when model?.GetSymbolInfo(lam, ct).Symbol is { IsStatic: true }
                => true,
            AnonymousMethodExpressionSyntax am when model?.GetSymbolInfo(am, ct).Symbol is { IsStatic: true }
                => true,
            // Reaching a method/property declaration is the final enclosing scope — its IsStatic flag
            // is authoritative, so we return it as-is (true OR false; do not continue searching).
            MethodDeclarationSyntax md
                => model?.GetDeclaredSymbol(md, ct) is { IsStatic: true },
            PropertyDeclarationSyntax pd
                => model?.GetDeclaredSymbol(pd, ct) is { IsStatic: true },
            // Field initializers are in static context if the field is static; default to true for safety.
            FieldDeclarationSyntax => true,
            _ => null
        };
    }
}

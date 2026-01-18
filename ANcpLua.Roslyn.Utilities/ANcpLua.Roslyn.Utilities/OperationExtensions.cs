using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for working with <see cref="IOperation" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for navigating, analyzing, and querying the operation tree
///         in Roslyn analyzers. Operations represent the semantic interpretation of syntax nodes
///         and provide a language-agnostic way to analyze code behavior.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Tree navigation: <see cref="Ancestors" />, <see cref="Descendants" />,
///                 <see cref="FindAncestor{T}" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Context detection: <see cref="IsInsideLoop" />, <see cref="IsInsideTryBlock" />,
///                 <see cref="IsInExpressionTree" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Unwrapping: <see cref="UnwrapImplicitConversions" />, <see cref="UnwrapAllConversions" />,
///                 <see cref="UnwrapParenthesized" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Value analysis: <see cref="IsConstantZero" />, <see cref="IsConstantNull" />,
///                 <see cref="TryGetConstantValue{T}" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Assignment detection: <see cref="IsAssignmentTarget" />, <see cref="IsLeftSideOfAssignment" />
///                 , <see cref="IsPassedByRef" />
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="IOperation" />
/// <seealso cref="InvocationExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class OperationExtensions
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
    ///     The type of ancestor operation to check for. Must be a class implementing <see cref="IOperation" />
    ///     .
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
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent.Kind is OperationKind.NameOf)
                return true;
            parent = parent.Parent;
        }

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
    ///     <para>
    ///         This method handles both method argument scenarios (e.g., <c>SomeMethod(x => x == 0)</c>
    ///         where <c>SomeMethod</c> takes <c>Expression&lt;T&gt;</c>) and variable declaration scenarios
    ///         (e.g., <c>Expression&lt;Func&lt;int, bool&gt;&gt; expr = x => x == 0;</c>).
    ///     </para>
    /// </remarks>
    public static bool IsInExpressionTree(this IOperation operation, INamedTypeSymbol? expressionSymbol)
    {
        if (expressionSymbol is null)
            return false;

        foreach (var op in operation.Ancestors())
        {
            if (op is IArgumentOperation { Parameter.Type: { } paramType } &&
                IsConstructedFromExpressionType(paramType, expressionSymbol))
                return true;

            if (op is IConversionOperation { Type: { } convType } &&
                IsConstructedFromExpressionType(convType, expressionSymbol))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is constructed from the <c>Expression&lt;T&gt;</c> open generic type
    ///     or inherits from such a type.
    /// </summary>
    private static bool IsConstructedFromExpressionType(ITypeSymbol type, INamedTypeSymbol expressionSymbol)
    {
        // Check if the type itself is a constructed Expression<T>
        if (type is INamedTypeSymbol namedType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, expressionSymbol))
            return true;

        // Check if any base type is a constructed Expression<T>
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
    ///     This method walks up the syntax tree to find enclosing members and checks their static
    ///     modifier. For local functions and lambdas, it checks the <c>static</c> keyword.
    ///     Field declarations are assumed to be in a static context for safety.
    /// </remarks>
    public static bool IsInStaticContext(this IOperation operation, CancellationToken cancellationToken)
    {
        foreach (var member in operation.Syntax.Ancestors())
            if (member is LocalFunctionStatementSyntax localFunction)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(localFunction, cancellationToken);
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is LambdaExpressionSyntax lambdaExpression)
            {
                var symbol = operation.SemanticModel?.GetSymbolInfo(lambdaExpression, cancellationToken).Symbol;
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is AnonymousMethodExpressionSyntax anonymousMethod)
            {
                var symbol = operation.SemanticModel?.GetSymbolInfo(anonymousMethod, cancellationToken).Symbol;
                if (symbol is { IsStatic: true })
                    return true;
            }
            else if (member is MethodDeclarationSyntax methodDeclaration)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                return symbol is { IsStatic: true };
            }
            else if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                var symbol = operation.SemanticModel?.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
                return symbol is { IsStatic: true };
            }
            else if (member is FieldDeclarationSyntax)
            {
                return
                    true; // Field initializers are in static context if the field is static, but default to true for safety
            }

        return false;
    }

    /// <summary>
    ///     Unwraps implicit conversion operations to get the underlying operand.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>
    ///     The innermost operation after removing all enclosing implicit conversions,
    ///     or <paramref name="operation" /> itself if it is not an implicit conversion.
    /// </returns>
    /// <seealso cref="UnwrapAllConversions" />
    /// <seealso cref="GetActualType" />
    public static IOperation UnwrapImplicitConversions(this IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;
        return operation;
    }

    /// <summary>
    ///     Unwraps all conversion operations (both implicit and explicit) to get the underlying operand.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>
    ///     The innermost operation after removing all enclosing conversions,
    ///     or <paramref name="operation" /> itself if it is not a conversion.
    /// </returns>
    /// <seealso cref="UnwrapImplicitConversions" />
    /// <seealso cref="GetActualType" />
    public static IOperation UnwrapAllConversions(this IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;
        return operation;
    }

    /// <summary>
    ///     Unwraps parenthesized operations to get the inner operand.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>
    ///     The innermost operation after removing all enclosing parentheses,
    ///     or <paramref name="operation" /> itself if it is not parenthesized.
    /// </returns>
    public static IOperation UnwrapParenthesized(this IOperation operation)
    {
        while (operation is IParenthesizedOperation parenthesized)
            operation = parenthesized.Operand;
        return operation;
    }

    /// <summary>
    ///     Unwraps labeled operations to get the inner operation.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>
    ///     The innermost operation after removing all enclosing labels,
    ///     or <c>null</c> if the labeled operation has no inner operation.
    ///     Returns <paramref name="operation" /> if it is not a labeled operation.
    /// </returns>
    public static IOperation? UnwrapLabeledOperations(this IOperation operation)
    {
        if (operation is ILabeledOperation label)
            return label.Operation?.UnwrapLabeledOperations();
        return operation;
    }

    /// <summary>
    ///     Gets the actual type of an operation after unwrapping all conversions.
    /// </summary>
    /// <param name="operation">The operation whose actual type to get.</param>
    /// <returns>
    ///     The type of the innermost operation after removing all conversions,
    ///     or <c>null</c> if the operation has no type.
    /// </returns>
    /// <seealso cref="UnwrapAllConversions" />
    public static ITypeSymbol? GetActualType(this IOperation operation)
    {
        return operation.UnwrapAllConversions().Type;
    }

    /// <summary>
    ///     Determines whether the operation represents a constant zero value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is a compile-time constant with a value of zero
    ///     (as <see cref="int" />, <see cref="long" />, <see cref="uint" />, <see cref="ulong" />,
    ///     <see cref="float" />, <see cref="double" />, or <see cref="decimal" />);
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstantNull" />
    /// <seealso cref="IsConstant" />
    public static bool IsConstantZero(this IOperation operation)
    {
        return operation is { ConstantValue: { HasValue: true, Value: 0 or 0L or 0u or 0uL or 0f or 0d or 0m } };
    }

    /// <summary>
    ///     Determines whether the operation represents a constant <c>null</c> value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is a compile-time constant with a <c>null</c> value;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstantZero" />
    /// <seealso cref="IsConstant" />
    public static bool IsConstantNull(this IOperation operation)
    {
        return operation is { ConstantValue: { HasValue: true, Value: null } };
    }

    /// <summary>
    ///     Determines whether the operation represents a compile-time constant.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the constant value.
    ///     When this method returns <c>false</c>, contains <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation is a compile-time constant; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="TryGetConstantValue{T}" />
    /// <seealso cref="IsConstantZero" />
    /// <seealso cref="IsConstantNull" />
    public static bool IsConstant(this IOperation operation, out object? value)
    {
        if (operation.ConstantValue is { HasValue: true } constant)
        {
            value = constant.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Tries to get the constant value of the specified type from an operation.
    /// </summary>
    /// <typeparam name="T">The expected type of the constant value.</typeparam>
    /// <param name="operation">The operation to check.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the constant value of type <typeparamref name="T" />.
    ///     When this method returns <c>false</c>, contains the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation is a compile-time constant of type <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstant" />
    public static bool TryGetConstantValue<T>(this IOperation operation, out T value)
    {
        if (operation.ConstantValue is { HasValue: true, Value: T typedValue })
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    ///     Gets the method symbol that contains this operation.
    /// </summary>
    /// <param name="operation">The operation whose containing method to find.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The <see cref="IMethodSymbol" /> for the containing method, local function, accessor,
    ///     or lambda expression; or <c>null</c> if the operation is not within a method-like construct
    ///     or the semantic model is unavailable.
    /// </returns>
    /// <seealso cref="GetContainingType" />
    /// <seealso cref="GetContainingBlock" />
    public static IMethodSymbol? GetContainingMethod(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
            switch (syntax)
            {
                case MethodDeclarationSyntax method:
                    return operation.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
                case LocalFunctionStatementSyntax localFunction:
                    return operation.SemanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
                case AccessorDeclarationSyntax accessor:
                    return operation.SemanticModel.GetDeclaredSymbol(accessor, cancellationToken);
                case LambdaExpressionSyntax lambda:
                    return operation.SemanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol as IMethodSymbol;
            }

        return null;
    }

    /// <summary>
    ///     Gets the named type symbol that contains this operation.
    /// </summary>
    /// <param name="operation">The operation whose containing type to find.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The <see cref="INamedTypeSymbol" /> for the containing type declaration,
    ///     or <c>null</c> if the operation is not within a type or the semantic model is unavailable.
    /// </returns>
    /// <seealso cref="GetContainingMethod" />
    public static INamedTypeSymbol? GetContainingType(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel is null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
            if (syntax is TypeDeclarationSyntax typeDecl)
                return operation.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        return null;
    }

    /// <summary>
    ///     Gets the C# language version of the source containing this operation.
    /// </summary>
    /// <param name="operation">The operation whose language version to determine.</param>
    /// <returns>
    ///     The <see cref="LanguageVersion" /> of the source file, or <see cref="LanguageVersion.Default" />
    ///     if the syntax tree options are not C# parse options.
    /// </returns>
    public static LanguageVersion GetCSharpLanguageVersion(this IOperation operation)
    {
        if (operation.Syntax.SyntaxTree.Options is CSharpParseOptions options)
            return options.LanguageVersion;
        return LanguageVersion.Default;
    }

    /// <summary>
    ///     Enumerates the operation and all its descendants in depth-first order.
    /// </summary>
    /// <param name="operation">The root operation.</param>
    /// <returns>
    ///     An enumerable sequence containing <paramref name="operation" /> followed by all its descendants.
    /// </returns>
    /// <seealso cref="Descendants" />
    /// <seealso cref="DescendantsOfType{T}" />
    public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
    {
        yield return operation;
        foreach (var child in operation.ChildOperations)
        foreach (var descendant in child.DescendantsAndSelf())
            yield return descendant;
    }

    /// <summary>
    ///     Enumerates all descendants of the operation in depth-first order.
    /// </summary>
    /// <param name="operation">The root operation.</param>
    /// <returns>
    ///     An enumerable sequence of all descendant operations, not including <paramref name="operation" /> itself.
    /// </returns>
    /// <seealso cref="DescendantsAndSelf" />
    /// <seealso cref="DescendantsOfType{T}" />
    public static IEnumerable<IOperation> Descendants(this IOperation operation)
    {
        foreach (var child in operation.ChildOperations)
        foreach (var descendant in child.DescendantsAndSelf())
            yield return descendant;
    }

    /// <summary>
    ///     Enumerates all descendants of a specific type in depth-first order.
    /// </summary>
    /// <typeparam name="T">The type of operations to find. Must implement <see cref="IOperation" />.</typeparam>
    /// <param name="operation">The root operation.</param>
    /// <returns>
    ///     An enumerable sequence of all descendant operations of type <typeparamref name="T" />.
    /// </returns>
    /// <seealso cref="Descendants" />
    /// <seealso cref="ContainsOperation{T}" />
    public static IEnumerable<T> DescendantsOfType<T>(this IOperation operation) where T : IOperation
    {
        foreach (var descendant in operation.Descendants())
            if (descendant is T typed)
                yield return typed;
    }

    /// <summary>
    ///     Determines whether the operation contains any descendant of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of operation to search for. Must implement <see cref="IOperation" />.</typeparam>
    /// <param name="operation">The operation to search within.</param>
    /// <returns>
    ///     <c>true</c> if any descendant of type <typeparamref name="T" /> exists; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="DescendantsOfType{T}" />
    public static bool ContainsOperation<T>(this IOperation operation) where T : IOperation
    {
        return operation.DescendantsOfType<T>().Any();
    }

    /// <summary>
    ///     Determines whether the operation is inside a loop construct.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>for</c>, <c>foreach</c>, <c>while</c>,
    ///     or <c>do</c> loop; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideTryBlock" />
    /// <seealso cref="IsInsideLockStatement" />
    public static bool IsInsideLoop(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ILoopOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside a try block.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>try</c> statement; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This checks if the operation is anywhere within a try operation, including in the
    ///     try block itself, catch blocks, or finally blocks.
    /// </remarks>
    /// <seealso cref="IsInsideCatchBlock" />
    /// <seealso cref="IsInsideFinallyBlock" />
    public static bool IsInsideTryBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ITryOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside a catch block.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>catch</c> clause; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideTryBlock" />
    /// <seealso cref="IsInsideFinallyBlock" />
    public static bool IsInsideCatchBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ICatchClauseOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside a finally block.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>finally</c> block; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideTryBlock" />
    /// <seealso cref="IsInsideCatchBlock" />
    public static bool IsInsideFinallyBlock(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ITryOperation tryOp &&
                tryOp.Finally?.Operations.Any(op => op.Syntax.Contains(operation.Syntax)) is true)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside a lock statement.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>lock</c> statement; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideUsingStatement" />
    /// <seealso cref="IsInsideLoop" />
    public static bool IsInsideLockStatement(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is ILockOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether the operation is inside a using statement or declaration.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>using</c> statement or <c>using</c> declaration;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideLockStatement" />
    public static bool IsInsideUsingStatement(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (parent is IUsingOperation or IUsingDeclarationOperation)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Gets the containing block operation for this operation.
    /// </summary>
    /// <param name="operation">The operation whose containing block to find.</param>
    /// <returns>
    ///     The <see cref="IBlockOperation" /> that contains this operation,
    ///     or <c>null</c> if no containing block exists.
    /// </returns>
    /// <seealso cref="GetContainingMethod" />
    /// <seealso cref="FindAncestor{T}" />
    public static IBlockOperation? GetContainingBlock(this IOperation operation)
    {
        return operation.FindAncestor<IBlockOperation>();
    }

    /// <summary>
    ///     Determines whether the operation is the target of an assignment.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is the left-hand side of a simple assignment,
    ///     compound assignment, or increment/decrement operation; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsLeftSideOfAssignment" />
    /// <seealso cref="IsPassedByRef" />
    public static bool IsAssignmentTarget(this IOperation operation)
    {
        var parent = operation.Parent;
        if (parent is ISimpleAssignmentOperation assignment)
            return ReferenceEquals(assignment.Target, operation);
        if (parent is ICompoundAssignmentOperation compound)
            return ReferenceEquals(compound.Target, operation);
        if (parent is IIncrementOrDecrementOperation)
            return true;
        return false;
    }

    /// <summary>
    ///     Determines whether the operation is on the left side of an assignment.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation (after unwrapping implicit conversions) is the target
    ///     of an assignment operation; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method unwraps implicit conversions before checking, which is useful when
    ///     the left-hand side involves an implicit conversion (e.g., assigning to a property
    ///     of a different but compatible type).
    /// </remarks>
    /// <seealso cref="IsAssignmentTarget" />
    public static bool IsLeftSideOfAssignment(this IOperation operation)
    {
        var unwrapped = operation.UnwrapImplicitConversions();
        return unwrapped.Parent is IAssignmentOperation assignment && ReferenceEquals(assignment.Target, unwrapped);
    }

    /// <summary>
    ///     Determines whether the operation is passed by reference to a method.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is an argument to a parameter with <c>ref</c>, <c>out</c>,
    ///     <c>in</c>, or <c>ref readonly</c> modifier; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsAssignmentTarget" />
    public static bool IsPassedByRef(this IOperation operation)
    {
        if (operation.Parent is IArgumentOperation { Parameter: { RefKind: not RefKind.None } })
            return true;
        return false;
    }
}
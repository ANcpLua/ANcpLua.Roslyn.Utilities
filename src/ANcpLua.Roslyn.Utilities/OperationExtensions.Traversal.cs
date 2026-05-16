using Microsoft.CodeAnalysis.Operations;
using IOperation = Microsoft.CodeAnalysis.IOperation;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
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
        return operation.HasAncestor(static parent => parent is ILoopOperation);
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
        return operation.HasAncestor(static parent => parent is ITryOperation);
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
        return operation.HasAncestor(static parent => parent is ICatchClauseOperation);
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
        return operation.HasAncestor(static parent => parent.Parent is ITryOperation tryOperation
                                                      && ReferenceEquals(tryOperation.Finally, parent));
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
        return operation.HasAncestor(static parent => parent is ILockOperation);
    }

    /// <summary>
    ///     Determines whether the specified operation is a <c>using</c> statement or <c>using</c> declaration.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="operation" /> is an <see cref="IUsingOperation" /> or
    ///     <see cref="IUsingDeclarationOperation" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsInsideUsingStatement" />
    public static bool IsUsingStatement(this IOperation operation)
    {
        return operation is IUsingOperation or IUsingDeclarationOperation;
    }

    /// <summary>
    ///     Determines whether the operation is inside a using statement or declaration.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is within a <c>using</c> statement or <c>using</c> declaration;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsUsingStatement" />
    /// <seealso cref="IsInsideLockStatement" />
    public static bool IsInsideUsingStatement(this IOperation operation)
    {
        return operation.HasAncestor(static parent => parent is IUsingOperation or IUsingDeclarationOperation);
    }

    private static bool HasAncestor(this IOperation operation, Func<IOperation, bool> predicate)
    {
        var parent = operation.Parent;
        while (parent is not null)
        {
            if (predicate(parent))
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
    /// <seealso cref="FindAncestor{T}" />
    public static IBlockOperation? GetContainingBlock(this IOperation operation)
    {
        return operation.FindAncestor<IBlockOperation>();
    }
}

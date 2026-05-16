using Microsoft.CodeAnalysis.Operations;
using IOperation = Microsoft.CodeAnalysis.IOperation;
using ITypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
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
    ///     Unwraps parenthesized operations to get the underlying operand.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The innermost non-parenthesized operation.</returns>
    public static IOperation UnwrapParenthesized(this IOperation operation)
    {
        while (operation is IParenthesizedOperation paren)
            operation = paren.Operand;
        return operation;
    }

    /// <summary>
    ///     Unwraps labeled operations to access the labeled statement's body.
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>
    ///     The inner <see cref="ILabeledOperation.Operation" /> if <paramref name="operation" />
    ///     is an <see cref="ILabeledOperation" />; otherwise, <paramref name="operation" /> itself.
    ///     Returns <c>null</c> if a labeled operation has no body.
    /// </returns>
    public static IOperation? UnwrapLabeledOperations(this IOperation operation)
    {
        return operation is ILabeledOperation labeled ? labeled.Operation : operation;
    }

    /// <summary>
    ///     Gets the actual type of an operation, unwrapping implicit conversions first.
    /// </summary>
    /// <param name="operation">The operation whose actual type to retrieve.</param>
    /// <returns>
    ///     The <see cref="ITypeSymbol" /> of the operation after unwrapping implicit conversions,
    ///     or <c>null</c> if no type is associated with the operation.
    /// </returns>
    /// <remarks>
    ///     This is useful when an analyzer needs to determine the actual type of an expression,
    ///     bypassing any implicit conversions inserted by the compiler.
    /// </remarks>
    /// <seealso cref="UnwrapImplicitConversions" />
    public static ITypeSymbol? GetActualType(this IOperation operation)
    {
        return operation.UnwrapImplicitConversions().Type;
    }
}

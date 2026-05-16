using Microsoft.CodeAnalysis.Operations;
using IOperation = Microsoft.CodeAnalysis.IOperation;
using RefKind = Microsoft.CodeAnalysis.RefKind;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
    /// <summary>
    ///     Determines whether the operation is the target of an assignment.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is the left-hand side of a simple assignment,
    ///     compound assignment, or increment/decrement operation; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Pattern-switch form lets the three target shapes (simple assignment, compound assignment,
    ///     increment/decrement) appear as parallel arms instead of a sequential if-chain. Each arm
    ///     is independently readable.
    /// </remarks>
    /// <seealso cref="IsLeftSideOfAssignment" />
    /// <seealso cref="IsPassedByRef" />
    public static bool IsAssignmentTarget(this IOperation operation)
    {
        return operation.Parent switch
        {
            ISimpleAssignmentOperation assignment => ReferenceEquals(assignment.Target, operation),
            ICompoundAssignmentOperation compound => ReferenceEquals(compound.Target, operation),
            IIncrementOrDecrementOperation => true,
            _ => false
        };
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
        return operation.Parent is IArgumentOperation { Parameter: { RefKind: not RefKind.None } };
    }
}

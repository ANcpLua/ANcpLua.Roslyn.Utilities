using IOperation = Microsoft.CodeAnalysis.IOperation;
using OperationKind = Microsoft.CodeAnalysis.OperationKind;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class OperationExtensions
{
    /// <summary>
    ///     Determines whether the operation represents the numeric constant <c>0</c> in any built-in
    ///     numeric type (<see cref="int" />, <see cref="long" />, <see cref="uint" />, <see cref="ulong" />,
    ///     <see cref="float" />, <see cref="double" />, <see cref="decimal" />).
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="operation" />.<c>ConstantValue</c> is a numeric zero in any of the
    ///     supported types; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     The pattern explicitly lists each numeric-type zero because the C# pattern literal <c>0</c>
    ///     alone is <c>(int)0</c>, which would silently reject <c>0L</c>, <c>0.0f</c>, <c>0.0</c>,
    ///     <c>0m</c> — defeating the "check for zero in comparisons" use case the helper exists for.
    /// </remarks>
    /// <seealso cref="IsConstantNull" />
    /// <seealso cref="IsConstant{T}" />
    public static bool IsConstantZero(this IOperation operation)
    {
        return operation.ConstantValue is { HasValue: true, Value: 0 or 0L or 0u or 0uL or 0f or 0d or 0m };
    }

    /// <summary>
    ///     Determines whether the operation represents a constant <c>null</c> value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation has a constant value of <c>null</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsNull" />
    /// <seealso cref="IsConstantZero" />
    public static bool IsConstantNull(this IOperation operation)
    {
        return operation.ConstantValue is { HasValue: true, Value: null };
    }

    /// <summary>
    ///     Determines whether the operation is of the specified operation kind.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="kind">The <see cref="OperationKind" /> to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="operation" />.Kind equals <paramref name="kind" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool IsKind(this IOperation operation, OperationKind kind)
    {
        return operation.Kind == kind;
    }

    /// <summary>
    ///     Determines whether the operation is a literal <c>null</c> expression.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>
    ///     <c>true</c> if the operation is a literal <c>null</c>; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstantNull" />
    public static bool IsNull(this IOperation operation)
    {
        return operation.IsKind(OperationKind.Literal) && operation.ConstantValue.Value is null;
    }

    /// <summary>
    ///     Determines whether the operation has a specific constant value.
    /// </summary>
    /// <typeparam name="T">The type of the constant value to check for.</typeparam>
    /// <param name="operation">The operation to check.</param>
    /// <param name="value">The expected constant value.</param>
    /// <returns>
    ///     <c>true</c> if the operation has a constant value equal to <paramref name="value" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstantZero" />
    /// <seealso cref="IsConstantNull" />
    /// <seealso cref="TryGetConstantValue{T}" />
    public static bool IsConstant<T>(this IOperation operation, T value)
    {
        return operation.ConstantValue is { HasValue: true, Value: T typedValue }
               && EqualityComparer<T>.Default.Equals(typedValue, value);
    }

    /// <summary>
    ///     Attempts to extract a constant value from the operation.
    /// </summary>
    /// <param name="operation">The operation to extract the constant from.</param>
    /// <param name="value">
    ///     When this method returns, contains the constant value if extraction succeeded;
    ///     otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation has a constant value; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstant{T}" />
    /// <seealso cref="TryGetConstantValue{T}" />
    public static bool IsConstant(this IOperation operation, out object? value)
    {
        if (operation.ConstantValue.HasValue)
        {
            value = operation.ConstantValue.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Attempts to extract a typed constant value from the operation.
    /// </summary>
    /// <typeparam name="T">The expected type of the constant value.</typeparam>
    /// <param name="operation">The operation to extract the constant from.</param>
    /// <param name="value">
    ///     When this method returns, contains the typed constant value if extraction succeeded;
    ///     otherwise, the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation has a constant value of type <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsConstant{T}" />
    public static bool TryGetConstantValue<T>(this IOperation operation, [MaybeNullWhen(false)] out T value)
    {
        if (operation.ConstantValue is { HasValue: true, Value: T typedValue })
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }
}

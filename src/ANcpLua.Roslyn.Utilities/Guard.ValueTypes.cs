using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    /// <summary>
    ///     Validates that an enum value is defined and returns it.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated enum value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> is not a defined enum value.</exception>
    /// <example>
    ///     <code>
    /// public void SetStatus(Status status)
    /// {
    ///     _status = Guard.DefinedEnum(status);
    /// }
    /// </code>
    /// </example>
    public static T DefinedEnum<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, Enum
    {
#if NET5_0_OR_GREATER
        if (!Enum.IsDefined(value))
#else
        if (!Enum.IsDefined(typeof(T), value))
#endif
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Undefined enum value for {typeof(T).Name}: {value}");

        return value;
    }

    /// <summary>
    ///     Validates that a value type is not its default value and returns it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> equals <c>default(T)</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetId(Guid id)
    /// {
    ///     _id = Guard.NotDefault(id);
    /// }
    /// </code>
    /// </example>
    public static T NotDefault<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct
    {
        return EqualityComparer<T>.Default.Equals(value, default)
            ? throw new ArgumentException("Value cannot be default.", paramName)
            : value;
    }

    /// <summary>
    ///     Validates that a <see cref="Guid" /> is not empty and returns it.
    /// </summary>
    /// <param name="value">The Guid to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated Guid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is <see cref="Guid.Empty" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetUserId(Guid userId)
    /// {
    ///     _userId = Guard.NotEmpty(userId);
    /// }
    /// </code>
    /// </example>
    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return value == Guid.Empty
            ? throw new ArgumentException("Guid cannot be empty.", paramName)
            : value;
    }
}

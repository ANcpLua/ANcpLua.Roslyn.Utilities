using System.IO;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class Guard
{
    #region Value Type Validation

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

    #endregion

    #region Type Validation

    /// <summary>
    ///     Validates that a <see cref="Type" /> is not a <see cref="Nullable{T}" />.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type" /> is a <see cref="Nullable{T}" />.</exception>
    /// <example>
    ///     <code>
    /// public void Register(Type type)
    /// {
    ///     Guard.NotNullableType(type);
    ///     _registry.Add(type);
    /// }
    /// </code>
    /// </example>
    public static Type NotNullableType(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return Nullable.GetUnderlyingType(type) is not null
            ? throw new ArgumentException("Nullable types are not supported.", paramName)
            : type;
    }

    /// <summary>
    ///     Validates that a <see cref="Type" /> is assignable to the specified type.
    /// </summary>
    /// <typeparam name="T">The type that <paramref name="type" /> must be assignable to.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="type" /> is not assignable to <typeparamref name="T" />
    ///     .
    /// </exception>
    public static Type AssignableTo<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return !typeof(T).IsAssignableFrom(type)
            ? throw new ArgumentException($"Type {type.Name} is not assignable to {typeof(T).Name}.", paramName)
            : type;
    }

    /// <summary>
    ///     Validates that a <see cref="Type" /> is assignable from the specified type.
    /// </summary>
    /// <typeparam name="T">The type that must be assignable to <paramref name="type" />.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <typeparamref name="T" /> is not assignable to <paramref name="type" />
    ///     .
    /// </exception>
    public static Type AssignableFrom<T>(
        [NotNull] Type? type,
        [CallerArgumentExpression(nameof(type))]
        string? paramName = null)
    {
        NotNull(type, paramName);

        return !type.IsAssignableFrom(typeof(T))
            ? throw new ArgumentException($"Type {typeof(T).Name} is not assignable to {type.Name}.", paramName)
            : type;
    }

    #endregion
}

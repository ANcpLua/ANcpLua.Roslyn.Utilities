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
    ///     Validates that a string has exactly the specified length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="length">The required length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> does not have the required length.</exception>
    /// <example>
    ///     <code>
    /// public void SetCountryCode(string code)
    /// {
    ///     _code = Guard.HasLength(code, 2); // ISO country codes
    /// }
    /// </code>
    /// </example>
    public static string HasLength(
        [NotNull] string? value,
        int length,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length != length)
            throw new ArgumentException($"String must be exactly {length} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string has at least the specified minimum length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="value" /> is shorter than <paramref name="minLength" />
    ///     .
    /// </exception>
    /// <example>
    ///     <code>
    /// public void SetPassword(string password)
    /// {
    ///     _password = Guard.HasMinLength(password, 8);
    /// }
    /// </code>
    /// </example>
    public static string HasMinLength(
        [NotNull] string? value,
        int minLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length < minLength)
            throw new ArgumentException($"String must be at least {minLength} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string does not exceed the specified maximum length and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> exceeds <paramref name="maxLength" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetUsername(string username)
    /// {
    ///     _username = Guard.HasMaxLength(username, 50);
    /// }
    /// </code>
    /// </example>
    public static string HasMaxLength(
        [NotNull] string? value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length > maxLength)
            throw new ArgumentException($"String must not exceed {maxLength} characters. Actual: {value.Length}.",
                paramName);

        return value;
    }

    /// <summary>
    ///     Validates that a string length is within the specified range (inclusive) and returns it.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> length is outside the specified range.</exception>
    /// <example>
    ///     <code>
    /// public void SetDisplayName(string name)
    /// {
    ///     _name = Guard.HasLengthBetween(name, 3, 100);
    /// }
    /// </code>
    /// </example>
    public static string HasLengthBetween(
        [NotNull] string? value,
        int minLength,
        int maxLength,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentException(
                $"String length must be between {minLength} and {maxLength}. Actual: {value.Length}.", paramName);

        return value;
    }
}

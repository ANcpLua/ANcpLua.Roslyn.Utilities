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
    ///     Validates that a value is one of the allowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="allowed">The set of allowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not in <paramref name="allowed" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetProtocol(string protocol)
    /// {
    ///     _protocol = Guard.OneOf(protocol, "http", "https", "ftp");
    /// }
    /// </code>
    /// </example>
    public static T OneOf<T>(
        T value,
        T[] allowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        foreach (var item in allowed)
            if (EqualityComparer<T>.Default.Equals(value, item))
                return value;

        var allowedStr = string.Join(", ", allowed);
        throw new ArgumentException($"Value must be one of: [{allowedStr}]. Actual: {value}.", paramName);
    }

    /// <summary>
    ///     Validates that a value is one of the allowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="allowed">The set of allowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not in <paramref name="allowed" />.</exception>
    public static T OneOf<T>(
        T value,
        HashSet<T> allowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (allowed.Contains(value))
            return value;

        var allowedStr = string.Join(", ", allowed);
        throw new ArgumentException($"Value must be one of: [{allowedStr}]. Actual: {value}.", paramName);
    }

    /// <summary>
    ///     Validates that a value is not one of the disallowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="disallowed">The set of disallowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is in <paramref name="disallowed" />.</exception>
    /// <example>
    ///     <code>
    /// public void SetPort(int port)
    /// {
    ///     _port = Guard.NotOneOf(port, 0, 80, 443); // Reserved ports
    /// }
    /// </code>
    /// </example>
    public static T NotOneOf<T>(
        T value,
        T[] disallowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        foreach (var item in disallowed)
            if (EqualityComparer<T>.Default.Equals(value, item))
            {
                var disallowedStr = string.Join(", ", disallowed);
                throw new ArgumentException($"Value must not be one of: [{disallowedStr}]. Actual: {value}.",
                    paramName);
            }

        return value;
    }

    /// <summary>
    ///     Validates that a value is not one of the disallowed values and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="disallowed">The set of disallowed values.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is in <paramref name="disallowed" />.</exception>
    public static T NotOneOf<T>(
        T value,
        HashSet<T> disallowed,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (disallowed.Contains(value))
        {
            var disallowedStr = string.Join(", ", disallowed);
            throw new ArgumentException($"Value must not be one of: [{disallowedStr}]. Actual: {value}.", paramName);
        }

        return value;
    }
}

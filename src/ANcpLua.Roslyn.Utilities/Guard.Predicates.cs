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
    ///     Validates that a value is within the specified range (inclusive).
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement <see cref="IComparable{T}" />).</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="value" /> is less than <paramref name="min" /> or greater than <paramref name="max" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public void SetPage(int page)
    /// {
    ///     _page = Guard.InRange(page, 1, 100);
    /// }
    /// </code>
    /// </example>
    public static T InRange<T>(T value, T min, T max,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }

    /// <summary>
    ///     Validates that an index is within valid bounds for a collection.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="count">The total count of elements (exclusive upper bound).</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is negative or greater than or equal to <paramref name="count" />.
    /// </exception>
    /// <example>
    ///     <code>
    /// public T GetItem(int index)
    /// {
    ///     Guard.ValidIndex(index, _items.Count);
    ///     return _items[index];
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ValidIndex(int index, int count,
        [CallerArgumentExpression(nameof(index))]
        string? paramName = null)
    {
        // Single branch: (uint) cast makes negative values wrap to large positive,
        // so both index < 0 and index >= count are caught by one unsigned comparison.
        if ((uint)index >= (uint)count)
            ThrowIndexOutOfRange(index, count, paramName);
        return index;
    }

    /// <summary>
    ///     Validates that a condition is <c>true</c>.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The error message if the condition is <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="condition" /> is <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetAge(int age)
    /// {
    ///     Guard.That(age >= 0, "Age cannot be negative.");
    ///     _age = age;
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))]
        string? paramName = null)
    {
        if (!condition)
            ThrowArgument(message, paramName);
    }

    /// <summary>
    ///     Validates that a value matches a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate that must return <c>true</c> for the value to be valid.</param>
    /// <param name="message">The error message if the predicate returns <c>false</c>.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="predicate" /> returns <c>false</c>.</exception>
    /// <example>
    ///     <code>
    /// public void SetEmail(string email)
    /// {
    ///     _email = Guard.Satisfies(email, e => e.Contains('@'), "Invalid email format.");
    /// }
    /// </code>
    /// </example>
    public static T Satisfies<T>(
        T value,
        Func<T, bool> predicate,
        string message,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        return !predicate(value) ? throw new ArgumentException(message, paramName) : value;
    }
}

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
    ///     Validates that a collection contains no duplicate elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    /// <example>
    ///     <code>
    /// public void SetIds(IEnumerable&lt;int&gt; ids)
    /// {
    ///     Guard.NoDuplicates(ids);
    /// }
    /// </code>
    /// </example>
    public static void NoDuplicates<T>(
        IEnumerable<T> value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, null, paramName);
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements using a custom equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="comparer">The equality comparer to use for duplicate detection.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    /// <example>
    ///     <code>
    /// public void SetNames(IEnumerable&lt;string&gt; names)
    /// {
    ///     Guard.NoDuplicates(names, StringComparer.OrdinalIgnoreCase);
    /// }
    /// </code>
    /// </example>
    public static void NoDuplicates<T>(
        IEnumerable<T> value,
        IEqualityComparer<T> comparer,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, comparer, paramName);
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    public static IReadOnlyList<T> NoDuplicates<T>(
        [NotNull] IReadOnlyList<T>? value,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, null, paramName);
        return value;
    }

    /// <summary>
    ///     Validates that a collection contains no duplicate elements using a custom equality comparer and returns it.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="comparer">The equality comparer to use for duplicate detection.</param>
    /// <param name="paramName">
    ///     The name of the parameter (automatically captured via <see cref="CallerArgumentExpressionAttribute" />).
    /// </param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> contains duplicate elements.</exception>
    public static IReadOnlyList<T> NoDuplicates<T>(
        [NotNull] IReadOnlyList<T>? value,
        IEqualityComparer<T> comparer,
        [CallerArgumentExpression(nameof(value))]
        string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName ?? nameof(value));
        CheckNoDuplicates(value, comparer, paramName);
        return value;
    }
}

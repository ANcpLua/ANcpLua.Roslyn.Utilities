namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="ReadOnlySpan{T}" /> providing zero-allocation operations.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods provide LINQ-like functionality for spans without heap allocations,
///         making them ideal for performance-critical code paths in analyzers and source generators.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>All operations are performed in-place without allocating intermediate collections</description>
///         </item>
///         <item>
///             <description>Methods mirror the standard LINQ API for familiarity</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EnumerableExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SpanExtensions
{
    /// <summary>
    ///     Returns the element with the minimum value according to a key selector.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <typeparam name="TKey">
    ///     The type of the key returned by <paramref name="selector" />, which must implement
    ///     <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="span">The source span.</param>
    /// <param name="selector">A function to extract the comparison key from each element.</param>
    /// <returns>The element with the minimum key value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="span" /> is empty.</exception>
    /// <seealso cref="MaxBy{T,TKey}" />
    public static T MinBy<T, TKey>(this ReadOnlySpan<T> span, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        if (span.IsEmpty)
            throw new InvalidOperationException("Sequence contains no elements");

        var minItem = span[0];
        var minKey = selector(minItem);

        for (var i = 1; i < span.Length; i++)
        {
            var currentKey = selector(span[i]);
            if (currentKey.CompareTo(minKey) < 0)
            {
                minItem = span[i];
                minKey = currentKey;
            }
        }

        return minItem;
    }

    /// <summary>
    ///     Returns the element with the maximum value according to a key selector.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <typeparam name="TKey">
    ///     The type of the key returned by <paramref name="selector" />, which must implement
    ///     <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="span">The source span.</param>
    /// <param name="selector">A function to extract the comparison key from each element.</param>
    /// <returns>The element with the maximum key value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="span" /> is empty.</exception>
    /// <seealso cref="MinBy{T,TKey}" />
    public static T MaxBy<T, TKey>(this ReadOnlySpan<T> span, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        if (span.IsEmpty)
            throw new InvalidOperationException("Sequence contains no elements");

        var maxItem = span[0];
        var maxKey = selector(maxItem);

        for (var i = 1; i < span.Length; i++)
        {
            var currentKey = selector(span[i]);
            if (currentKey.CompareTo(maxKey) > 0)
            {
                maxItem = span[i];
                maxKey = currentKey;
            }
        }

        return maxItem;
    }

    /// <summary>
    ///     Finds the zero-based index of the first element in the span that matches the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span to search.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The zero-based index of the first element that matches the <paramref name="predicate" />,
    ///     or -1 if no element matches.
    /// </returns>
    /// <seealso cref="FirstOrDefault{T}" />
    public static int IndexOf<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        for (var i = 0; i < span.Length; i++)
            if (predicate(span[i]))
                return i;

        return -1;
    }

    /// <summary>
    ///     Determines whether any element of the span satisfies the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span to check.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     <c>true</c> if any element in <paramref name="span" /> passes the test in
    ///     <paramref name="predicate" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="All{T}" />
    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
            if (predicate(item))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether all elements of the span satisfy the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span to check.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     <c>true</c> if every element in <paramref name="span" /> passes the test in
    ///     <paramref name="predicate" />, or if the span is empty; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}" />
    public static bool All<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
            if (!predicate(item))
                return false;

        return true;
    }

    /// <summary>
    ///     Returns the count of elements in the span that satisfy the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span to count elements in.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The number of elements in <paramref name="span" /> that satisfy the
    ///     <paramref name="predicate" />.
    /// </returns>
    public static int Count<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        var count = 0;
        foreach (var item in span)
            if (predicate(item))
                count++;

        return count;
    }

    /// <summary>
    ///     Returns the first element in the span that matches the predicate, or a default value
    ///     if no element matches.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span to search.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The first element in <paramref name="span" /> that matches <paramref name="predicate" />,
    ///     or <c>default</c> if no element matches.
    /// </returns>
    /// <seealso cref="IndexOf{T}" />
    public static T? FirstOrDefault<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
            if (predicate(item))
                return item;

        return default;
    }
}

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class EnumerableExtensions
{
    /// <summary>
    ///     Returns the element with the minimum value according to a key selector,
    ///     or a default value if the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">
    ///     The type of the key returned by <paramref name="selector" />, which must implement
    ///     <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">A function to extract the comparison key from each element.</param>
    /// <returns>
    ///     The element with the minimum key value, or <c>default</c> if <paramref name="source" />
    ///     contains no elements.
    /// </returns>
    /// <seealso cref="MaxByOrDefault{T,TKey}" />
    public static T? MinByOrDefault<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        return ExtremeByOrDefault(source, selector, wantGreater: false);
    }

    /// <summary>
    ///     Returns the element with the maximum value according to a key selector,
    ///     or a default value if the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">
    ///     The type of the key returned by <paramref name="selector" />, which must implement
    ///     <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">A function to extract the comparison key from each element.</param>
    /// <returns>
    ///     The element with the maximum key value, or <c>default</c> if <paramref name="source" />
    ///     contains no elements.
    /// </returns>
    /// <seealso cref="MinByOrDefault{T,TKey}" />
    public static T? MaxByOrDefault<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        return ExtremeByOrDefault(source, selector, wantGreater: true);
    }

    // Shared min/max scanner. Previously MinByOrDefault and MaxByOrDefault each
    // duplicated 20 LOC of identical logic that differed only in the sign of the
    // comparison. Centralising the loop drops public method CC from ~4 to 1 and
    // removes the parallel-edit failure mode (fix found in one, missing in the other).
    private static T? ExtremeByOrDefault<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> selector,
        bool wantGreater)
        where TKey : IComparable<TKey>
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var bestItem = enumerator.Current;
        var bestKey = selector(bestItem);

        while (enumerator.MoveNext())
        {
            var currentKey = selector(enumerator.Current);
            var cmp = currentKey.CompareTo(bestKey);
            if (wantGreater ? cmp > 0 : cmp < 0)
            {
                bestItem = enumerator.Current;
                bestKey = currentKey;
            }
        }

        return bestItem;
    }

    /// <summary>
    ///     Splits the elements of a sequence into fixed-size chunks.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="batchSize">The maximum number of elements in each batch.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> of <see cref="IReadOnlyList{T}" /> where each inner list
    ///     contains up to <paramref name="batchSize" /> elements. The final batch may contain fewer elements.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="batchSize" /> is less than or equal to zero.
    /// </exception>
    public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    ///     Concatenates the string representations of the elements in a sequence using a separator.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="separator">The string to use as a separator between elements. Defaults to ", ".</param>
    /// <returns>
    ///     A string that consists of the elements of <paramref name="source" /> delimited by
    ///     <paramref name="separator" />.
    /// </returns>
    public static string JoinToString<T>(this IEnumerable<T> source, string separator = ", ")
    {
        Guard.NotNull(source);
        return string.Join(separator, source);
    }

    /// <summary>
    ///     Concatenates the string representations of the elements in a sequence using a character separator.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="values">The source sequence.</param>
    /// <param name="separator">The separator to use between elements.</param>
    /// <returns>The joined string.</returns>
    /// <seealso cref="JoinToString{T}" />
    public static string Join<T>(this IEnumerable<T> values, char separator)
    {
        Guard.NotNull(values);
        return string.Join(separator.ToString(), values);
    }

    /// <summary>
    ///     Returns all elements except the last <paramref name="count" /> elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="count">The number of elements to skip from the end.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> containing all elements of <paramref name="source" />
    ///     except for the last <paramref name="count" /> elements.
    /// </returns>
    /// <seealso cref="TakeLast{T}" />
    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
    {
        var queue = new Queue<T>(count + 1);
        foreach (var item in source)
        {
            queue.Enqueue(item);
            if (queue.Count > count)
                yield return queue.Dequeue();
        }
    }

    /// <summary>
    ///     Returns the last <paramref name="count" /> elements from the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="count">The number of elements to return from the end.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> containing the last <paramref name="count" /> elements
    ///     of <paramref name="source" />, or all elements if the sequence contains fewer than
    ///     <paramref name="count" /> elements.
    /// </returns>
    /// <seealso cref="SkipLast{T}" />
    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
    {
        var queue = new Queue<T>(count + 1);
        foreach (var item in source)
        {
            queue.Enqueue(item);
            if (queue.Count > count)
                queue.Dequeue();
        }

        return queue;
    }

    /// <summary>
    ///     Safely casts elements to a target type, filtering out <c>null</c> values and elements
    ///     that cannot be cast.
    /// </summary>
    /// <typeparam name="TResult">The target type to cast to (must be a reference type).</typeparam>
    /// <param name="source">The source sequence of objects.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{TResult}" /> containing only the elements of <paramref name="source" />
    ///     that can be successfully cast to <typeparamref name="TResult" />.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="Enumerable.Cast{TResult}(System.Collections.IEnumerable)" />, this method
    ///     does not throw an <see cref="InvalidCastException" /> for elements that cannot be cast.
    /// </remarks>
    public static IEnumerable<TResult> SafeCast<TResult>(this IEnumerable<object?> source) where TResult : class
    {
        foreach (var item in source)
            if (item is TResult typed)
                yield return typed;
    }
}

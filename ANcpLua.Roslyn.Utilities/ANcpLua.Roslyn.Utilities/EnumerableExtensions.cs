using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" /> and collections.
/// </summary>
/// <remarks>
///     Follows the "null = empty" pattern throughout for consistency.
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class EnumerableExtensions
{
    /// <summary>
    ///     Projects each element to an enumerable and flattens, returning empty if source is null.
    /// </summary>
    public static IEnumerable<TResult> SelectManyOrEmpty<TSource, TResult>(
        this IEnumerable<TSource>? source,
        Func<TSource, IEnumerable<TResult>> selector)
    {
        if (source is null)
            yield break;

        foreach (var item in source)
        foreach (var result in selector(item))
            yield return result;
    }

    /// <summary>
    ///     Returns the source enumerable or empty if null.
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) =>
        source ?? [];

    /// <summary>
    ///     Converts to ImmutableArray, returning empty if source is null.
    /// </summary>
    public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null ? ImmutableArray<T>.Empty : [..source];

    /// <summary>
    ///     Checks if the enumerable is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();

    /// <summary>
    ///     Checks if the enumerable has duplicate elements.
    /// </summary>
    public static bool HasDuplicates<T>(this IEnumerable<T> source)
    {
        var seen = new HashSet<T>();
        foreach (var item in source)
        {
            if (!seen.Add(item))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the enumerable has duplicate elements using a key selector.
    /// </summary>
    public static bool HasDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (!seen.Add(keySelector(item)))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Returns the single element, or default if empty or multiple exist.
    ///     Unlike SingleOrDefault, this does not throw on multiple elements.
    /// </summary>
    public static T? SingleOrDefaultIfMultiple<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var result = enumerator.Current;
        return enumerator.MoveNext() ? default : result;
    }

    /// <summary>
    ///     Filters null values and returns non-null elements.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        foreach (var item in source)
        {
            if (item is not null)
                yield return item;
        }
    }

    /// <summary>
    ///     Filters null values and returns non-null elements (for nullable value types).
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        foreach (var item in source)
        {
            if (item.HasValue)
                yield return item.Value;
        }
    }

    /// <summary>
    ///     Filters elements that don't match the predicate. Inverted Where.
    /// </summary>
    public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            if (!predicate(item))
                yield return item;
        }
    }

    /// <summary>
    ///     Returns elements with their index as a tuple.
    /// </summary>
    /// <example>
    ///     foreach (var (item, index) in items.Indexed())
    ///         Console.WriteLine($"{index}: {item}");
    /// </example>
    public static IEnumerable<(T Item, int Index)> Indexed<T>(this IEnumerable<T> source)
    {
        var index = 0;
        foreach (var item in source)
            yield return (item, index++);
    }

    /// <summary>
    ///     Returns consecutive pairs of elements.
    /// </summary>
    /// <example>
    ///     var deltas = values.ConsecutivePairs((prev, curr) => curr - prev);
    /// </example>
    public static IEnumerable<TResult> ConsecutivePairs<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TSource, TResult> selector)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        var previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return selector(previous, current);
            previous = current;
        }
    }

    /// <summary>
    ///     Returns consecutive pairs of elements as tuples.
    /// </summary>
    public static IEnumerable<(T Previous, T Current)> ConsecutivePairs<T>(this IEnumerable<T> source) =>
        source.ConsecutivePairs((prev, curr) => (prev, curr));

    /// <summary>
    ///     Finds the index of the first element matching the predicate.
    /// </summary>
    /// <returns>The index of the first match, or -1 if not found.</returns>
    public static int IndexOf<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Returns the first element matching the predicate, or default if none found.
    ///     More efficient than FirstOrDefault for IList.
    /// </summary>
    public static T? FirstOrDefaultFast<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return list[i];
        }

        return default;
    }

    /// <summary>
    ///     Partitions elements into two lists based on a predicate.
    /// </summary>
    /// <returns>A tuple of (matching, notMatching) lists.</returns>
    public static (List<T> Matching, List<T> NotMatching) Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in source)
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }

    /// <summary>
    ///     Returns distinct elements by a key selector.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
                yield return item;
        }
    }

    /// <summary>
    ///     Executes an action for each element (side-effect).
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    ///     Executes an action for each element with index.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
            action(item, index++);
    }

    /// <summary>
    ///     Returns the element with the minimum value according to a selector.
    ///     Returns default if the sequence is empty.
    /// </summary>
    public static T? MinByOrDefault<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var minItem = enumerator.Current;
        var minKey = selector(minItem);

        while (enumerator.MoveNext())
        {
            var currentKey = selector(enumerator.Current);
            if (currentKey.CompareTo(minKey) < 0)
            {
                minItem = enumerator.Current;
                minKey = currentKey;
            }
        }

        return minItem;
    }

    /// <summary>
    ///     Returns the element with the maximum value according to a selector.
    ///     Returns default if the sequence is empty.
    /// </summary>
    public static T? MaxByOrDefault<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var maxItem = enumerator.Current;
        var maxKey = selector(maxItem);

        while (enumerator.MoveNext())
        {
            var currentKey = selector(enumerator.Current);
            if (currentKey.CompareTo(maxKey) > 0)
            {
                maxItem = enumerator.Current;
                maxKey = currentKey;
            }
        }

        return maxItem;
    }

    /// <summary>
    ///     Batches elements into fixed-size chunks.
    /// </summary>
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
    ///     Aggregates elements into a string with a separator.
    /// </summary>
    public static string JoinToString<T>(this IEnumerable<T> source, string separator = ", ") =>
        string.Join(separator, source);

    /// <summary>
    ///     Returns all except the first N elements.
    /// </summary>
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
    ///     Returns only the last N elements.
    /// </summary>
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
    ///     Safely casts elements, filtering out nulls and invalid casts.
    /// </summary>
    public static IEnumerable<TResult> SafeCast<TResult>(this IEnumerable<object?> source) where TResult : class
    {
        foreach (var item in source)
        {
            if (item is TResult result)
                yield return result;
        }
    }
}

/// <summary>
///     Span-based extension methods for zero-allocation operations.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class SpanExtensions
{
    /// <summary>
    ///     Returns the element with the minimum value according to a selector.
    /// </summary>
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
    ///     Returns the element with the maximum value according to a selector.
    /// </summary>
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
    ///     Returns the index of the first element matching the predicate.
    /// </summary>
    public static int IndexOf<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (predicate(span[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Checks if any element matches the predicate.
    /// </summary>
    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (predicate(item))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if all elements match the predicate.
    /// </summary>
    public static bool All<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (!predicate(item))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Counts elements matching the predicate.
    /// </summary>
    public static int Count<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        var count = 0;
        foreach (var item in span)
        {
            if (predicate(item))
                count++;
        }

        return count;
    }

    /// <summary>
    ///     Returns the first element matching the predicate, or default.
    /// </summary>
    public static T? FirstOrDefault<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (predicate(item))
                return item;
        }

        return default;
    }
}

/// <summary>
///     ValueTuple extension methods for null checking.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class ValueTupleExtensions
{
    /// <summary>
    ///     Checks if any element in the tuple is not null.
    /// </summary>
    public static bool AnyNotNull<T1, T2>(this (T1?, T2?) tuple)
        => tuple.Item1 is not null || tuple.Item2 is not null;

    /// <summary>
    ///     Checks if any element in the tuple is not null.
    /// </summary>
    public static bool AnyNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
        => tuple.Item1 is not null || tuple.Item2 is not null || tuple.Item3 is not null;

    /// <summary>
    ///     Checks if all elements in the tuple are null.
    /// </summary>
    public static bool AllNull<T1, T2>(this (T1?, T2?) tuple)
        => tuple.Item1 is null && tuple.Item2 is null;

    /// <summary>
    ///     Checks if all elements in the tuple are null.
    /// </summary>
    public static bool AllNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
        => tuple.Item1 is null && tuple.Item2 is null && tuple.Item3 is null;

    /// <summary>
    ///     Checks if all elements in the tuple are not null.
    /// </summary>
    public static bool AllNotNull<T1, T2>(this (T1?, T2?) tuple)
        => tuple.Item1 is not null && tuple.Item2 is not null;

    /// <summary>
    ///     Checks if all elements in the tuple are not null.
    /// </summary>
    public static bool AllNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
        => tuple.Item1 is not null && tuple.Item2 is not null && tuple.Item3 is not null;

    /// <summary>
    ///     Enumerates the elements of a 2-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <example>
    ///     <code>
    /// var pair = ("first", "second");
    /// foreach (var item in pair.Enumerate())
    ///     Console.WriteLine(item);
    /// </code>
    /// </example>
    public static IEnumerable<T> Enumerate<T>(this (T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
    }

    /// <summary>
    ///     Enumerates the elements of a 3-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    public static IEnumerable<T> Enumerate<T>(this (T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
    }

    /// <summary>
    ///     Enumerates the elements of a 4-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    public static IEnumerable<T> Enumerate<T>(this (T, T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
        yield return tuple.Item4;
    }

    /// <summary>
    ///     Enumerates the elements of a 5-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    public static IEnumerable<T> Enumerate<T>(this (T, T, T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
        yield return tuple.Item4;
        yield return tuple.Item5;
    }
}

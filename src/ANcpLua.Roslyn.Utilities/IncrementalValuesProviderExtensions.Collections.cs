using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using InvalidOperationException = System.InvalidOperationException;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class IncrementalValuesProviderExtensions
{
    /// <summary>
    ///     Groups values by a key and projects each element using a selector.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method collects all values, groups them by the specified key, and returns
    ///         tuples containing the key and an <see cref="EquatableArray{T}" /> of projected elements.
    ///     </para>
    ///     <para>
    ///         The grouping is performed in-memory after collecting all values, which may impact
    ///         incremental caching for large datasets.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TElement">The type of the projected elements. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of source values to group.</param>
    /// <param name="keySelector">A function to extract the grouping key from each source value.</param>
    /// <param name="elementSelector">A function to project each source value into an element.</param>
    /// <param name="comparer">An optional equality comparer for keys. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples, each containing a key
    ///     and an <see cref="EquatableArray{T}" /> of elements with that key.
    /// </returns>
    /// <seealso
    ///     cref="GroupBy{TSource, TKey}(IncrementalValuesProvider{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey})" />
    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TElement> Elements)> GroupBy<TSource, TKey,
        TElement>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        comparer ??= EqualityComparer<TKey>.Default;
        return source.Collect().SelectMany((values, _) =>
        {
            // Preserve key-insertion order so generator output is deterministic.
            // Dictionary<,> enumeration order is not part of the contract (it happens
            // to follow insertion in the current .NET runtime, but a future runtime
            // is free to change that, and adding/removing entries can shift it today).
            var map = new Dictionary<TKey, ImmutableArray<TElement>.Builder>(comparer);
            var keys = new List<TKey>();
            foreach (var value in values)
            {
                var key = keySelector(value);
                if (!map.TryGetValue(key, out var builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();
                    map.Add(key, builder);
                    keys.Add(key);
                }

                builder.Add(elementSelector(value));
            }

            var result = ImmutableArray.CreateBuilder<(TKey, EquatableArray<TElement>)>(keys.Count);
            foreach (var key in keys)
                result.Add((key, map[key].ToImmutable().AsEquatableArray()));
            return result.MoveToImmutable();
        });
    }

    /// <summary>
    ///     Groups values by a key without projecting elements.
    /// </summary>
    /// <remarks>
    ///     This is a convenience overload that uses the source values directly as elements.
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of source values to group.</param>
    /// <param name="keySelector">A function to extract the grouping key from each source value.</param>
    /// <param name="comparer">An optional equality comparer for keys. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples, each containing a key
    ///     and an <see cref="EquatableArray{T}" /> of source values with that key.
    /// </returns>
    /// <seealso
    ///     cref="GroupBy{TSource, TKey, TElement}(IncrementalValuesProvider{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, IEqualityComparer{TKey})" />
    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TSource> Elements)> GroupBy<TSource, TKey>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : IEquatable<TKey>
        where TSource : IEquatable<TSource>
    {
        return source.GroupBy(keySelector, static x => x, comparer);
    }

    /// <summary>
    ///     Projects each value with its zero-based index in the collection.
    /// </summary>
    /// <remarks>
    ///     This method collects all values first, then projects each with its index.
    ///     The index is determined by the order in which values appear in the collected array.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to index.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples containing each value
    ///     paired with its zero-based index.
    /// </returns>
    public static IncrementalValuesProvider<(T Value, int Index)> WithIndex<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().SelectMany(static (values, _) =>
        {
            var result = ImmutableArray.CreateBuilder<(T, int)>(values.Length);
            for (var i = 0; i < values.Length; i++)
                result.Add((values[i], i));
            return result.MoveToImmutable();
        });
    }

    /// <summary>
    ///     Returns distinct values from the provider, removing duplicates.
    /// </summary>
    /// <remarks>
    ///     This method collects all values and returns only the first occurrence of each unique value.
    ///     The order of first occurrences is preserved.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to filter for uniqueness.</param>
    /// <param name="comparer">An optional equality comparer. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only distinct values.
    /// </returns>
    public static IncrementalValuesProvider<T> Distinct<T>(
        this IncrementalValuesProvider<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        return source.Collect().SelectMany((values, _) =>
        {
            var seen = new HashSet<T>(comparer);
            var result = ImmutableArray.CreateBuilder<T>();
            foreach (var value in values)
                if (seen.Add(value))
                    result.Add(value);

            return result.ToImmutable();
        });
    }

    /// <summary>
    ///     Combines a collected values provider with a single value provider.
    /// </summary>
    /// <remarks>
    ///     This is a convenience method that collects the left provider as an <see cref="EquatableArray{T}" />
    ///     and combines it with the right provider.
    /// </remarks>
    /// <typeparam name="TLeft">The type of the left values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TRight">The type of the right value.</typeparam>
    /// <param name="left">The provider of values to collect.</param>
    /// <param name="right">The single value provider to combine with.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing a tuple of the collected array and the right value.
    /// </returns>
    public static IncrementalValueProvider<(EquatableArray<TLeft> Left, TRight Right)> CombineWithCollected<TLeft,
        TRight>(
        this IncrementalValuesProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
        where TLeft : IEquatable<TLeft>
    {
        return left.CollectAsEquatableArray().Combine(right);
    }

    /// <summary>
    ///     Expressive alias for <c>IncrementalValueProvider.Combine</c>.
    /// </summary>
    public static IncrementalValueProvider<(TLeft Left, TRight Right)> CombineWith<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
    {
        return left.Combine(right);
    }

    /// <summary>
    ///     Expressive alias for <c>IncrementalValuesProvider.Combine</c>.
    /// </summary>
    public static IncrementalValuesProvider<(TLeft Left, TRight Right)> CombineWith<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
    {
        return left.Combine(right);
    }

    /// <summary>
    ///     Splits values into batches of a specified size.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values are collected and then split into fixed-size batches. The last batch may contain
    ///         fewer elements if the total count is not evenly divisible by the batch size.
    ///     </para>
    ///     <para>
    ///         An empty source produces no batches.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of values to batch.</param>
    /// <param name="batchSize">The maximum number of elements per batch. Must be positive.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of <see cref="EquatableArray{T}" /> batches.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize" /> is less than or equal to zero.</exception>
    public static IncrementalValuesProvider<EquatableArray<T>> Batch<T>(
        this IncrementalValuesProvider<T> source,
        int batchSize)
        where T : IEquatable<T>
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

        return source.Collect().SelectMany((values, _) =>
        {
            if (values.IsEmpty)
                return ImmutableArray<EquatableArray<T>>.Empty;

            var batches = ImmutableArray.CreateBuilder<EquatableArray<T>>();
            var batch = ImmutableArray.CreateBuilder<T>(batchSize);

            foreach (var value in values)
            {
                batch.Add(value);
                if (batch.Count == batchSize)
                {
                    batches.Add(batch.ToImmutable().AsEquatableArray());
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                batches.Add(batch.ToImmutable().AsEquatableArray());

            return batches.ToImmutable();
        });
    }

    /// <summary>
    ///     Returns a specified number of values from the start of the provider.
    /// </summary>
    /// <remarks>
    ///     If the source contains fewer values than requested, all values are returned.
    ///     If <paramref name="count" /> is zero or negative, no values are returned.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <param name="count">The number of values to take from the start.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing at most <paramref name="count" /> values.
    /// </returns>
    /// <seealso cref="Skip{T}(IncrementalValuesProvider{T}, int)" />
    public static IncrementalValuesProvider<T> Take<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source.Where(_ => false);

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? values : [..values.Take(count)]);
    }

    /// <summary>
    ///     Skips a specified number of values from the start of the provider.
    /// </summary>
    /// <remarks>
    ///     If the source contains fewer values than the skip count, an empty result is returned.
    ///     If <paramref name="count" /> is zero or negative, all values are returned.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <param name="count">The number of values to skip from the start.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing values after skipping the specified count.
    /// </returns>
    /// <seealso cref="Take{T}(IncrementalValuesProvider{T}, int)" />
    public static IncrementalValuesProvider<T> Skip<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source;

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? ImmutableArray<T>.Empty : [..values.Skip(count)]);
    }

    /// <summary>
    ///     Returns the count of values in the provider.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to count.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing the number of values.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<int> Count<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select(static (values, _) => values.Length);
    }

    /// <summary>
    ///     Determines whether the provider contains any values.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to check.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing <c>true</c> if the provider
    ///     contains at least one value; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T}, Func{T, bool})" />
    /// <seealso cref="Count{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select(static (values, _) => !values.IsEmpty);
    }

    /// <summary>
    ///     Determines whether any value in the provider satisfies a condition.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to check.</param>
    /// <param name="predicate">A function to test each value for a condition.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing <c>true</c> if any value
    ///     satisfies the predicate; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source,
        Func<T, bool> predicate)
    {
        return source.Collect().Select((values, _) =>
        {
            foreach (var value in values)
                if (predicate(value))
                    return true;

            return false;
        });
    }

    /// <summary>
    ///     Returns the first value in the provider, or a default value if the provider is empty.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing the first value,
    ///     or the default value of <typeparamref name="T" /> if the provider is empty.
    /// </returns>
    public static IncrementalValueProvider<T?> FirstOrDefault<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select(static (values, _) => values.IsEmpty ? default : values[0]);
    }
}

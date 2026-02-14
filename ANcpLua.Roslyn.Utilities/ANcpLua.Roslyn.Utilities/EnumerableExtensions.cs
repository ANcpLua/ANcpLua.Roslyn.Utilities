namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" /> and collections.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a comprehensive set of LINQ-style extension methods that complement
///         the standard library with null-safe operations and common patterns.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Follows the "null = empty" pattern throughout for consistency</description>
///         </item>
///         <item>
///             <description>All methods are designed to be allocation-efficient where possible</description>
///         </item>
///         <item>
///             <description>Provides safe alternatives to methods that throw on edge cases</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SpanExtensions" />
/// <seealso cref="ValueTupleExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class EnumerableExtensions
{
    /// <summary>
    ///     Projects each element to an enumerable and flattens the resulting sequences into one sequence,
    ///     returning an empty sequence if the source is <c>null</c>.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector" />.</typeparam>
    /// <param name="source">A sequence of values to project, or <c>null</c>.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{TResult}" /> whose elements are the result of invoking the
    ///     one-to-many transform function on each element of the input sequence, or an empty sequence
    ///     if <paramref name="source" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="OrEmpty{T}" />
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
    ///     Returns the source enumerable, or an empty sequence if the source is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence, or <c>null</c>.</param>
    /// <returns>
    ///     The original <paramref name="source" /> if it is not <c>null</c>;
    ///     otherwise, an empty <see cref="IEnumerable{T}" />.
    /// </returns>
    /// <seealso cref="ToImmutableArrayOrEmpty{T}" />
    /// <seealso cref="IsNullOrEmpty{T}" />
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) => source ?? [];

    /// <summary>
    ///     Converts the source sequence to an <see cref="ImmutableArray" />,
    ///     returning <see cref="ImmutableArray{T}.Empty" /> if the source is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence, or <c>null</c>.</param>
    /// <returns>
    ///     An <see cref="ImmutableArray{T}" /> containing the elements of <paramref name="source" />,
    ///     or <see cref="ImmutableArray{T}.Empty" /> if <paramref name="source" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="OrEmpty{T}" />
    public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? source) => source is null ? ImmutableArray<T>.Empty : [..source];

    /// <summary>
    ///     Determines whether the enumerable is <c>null</c> or contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="source" /> is <c>null</c> or contains no elements;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="OrEmpty{T}" />
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source is null || !source.Any();

    /// <summary>
    ///     Determines whether the enumerable contains duplicate elements using the default equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to check for duplicates.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="source" /> contains at least one duplicate element;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses a <see cref="HashSet{T}" /> internally for O(1) lookup performance,
    ///         resulting in O(n) time complexity where n is the number of elements.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Short-circuits on first duplicate found for optimal performance</description>
    ///         </item>
    ///         <item>
    ///             <description>Memory usage is O(k) where k is the number of unique elements seen before the first duplicate</description>
    ///         </item>
    ///         <item>
    ///             <description>Uses <see cref="EqualityComparer{T}.Default" /> for element comparison</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for duplicate method names in a type
    /// var methodNames = typeSymbol.GetMembers()
    ///     .OfType&lt;IMethodSymbol&gt;()
    ///     .Select(m => m.Name);
    ///
    /// if (methodNames.HasDuplicates())
    /// {
    ///     // Type has overloaded methods
    /// }
    ///
    /// // Check for duplicate values in a collection
    /// var numbers = new[] { 1, 2, 3, 2, 4 };
    /// bool hasDupes = numbers.HasDuplicates(); // true
    /// </code>
    /// </example>
    /// <seealso cref="HasDuplicates{T,TKey}(IEnumerable{T},Func{T,TKey})" />
    /// <seealso cref="DistinctBy{T,TKey}" />
    public static bool HasDuplicates<T>(this IEnumerable<T> source)
    {
        var seen = new HashSet<T>();
        foreach (var item in source)
            if (!seen.Add(item))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the enumerable contains duplicate elements based on a key selector.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <param name="source">The source sequence to check for duplicates.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="source" /> contains elements with duplicate keys;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses a <see cref="HashSet{T}" /> of keys internally for O(1) lookup,
    ///         resulting in O(n) time complexity. The key selector is called once per element.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Short-circuits on first duplicate key found</description>
    ///         </item>
    ///         <item>
    ///             <description>Useful when comparing complex objects by a specific property</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Key comparison uses <see cref="EqualityComparer{T}.Default" /> for
    ///                 <typeparamref name="TKey" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for duplicate parameter names in a method
    /// if (methodSymbol.Parameters.HasDuplicates(p => p.Name))
    /// {
    ///     // Report diagnostic: duplicate parameter names
    /// }
    ///
    /// // Check for duplicate file paths (case-insensitive)
    /// var files = new[] { "File.txt", "file.TXT", "other.cs" };
    /// bool hasDupes = files.HasDuplicates(f => f.ToLowerInvariant()); // true
    /// </code>
    /// </example>
    /// <seealso cref="HasDuplicates{T}(IEnumerable{T})" />
    /// <seealso cref="DistinctBy{T,TKey}" />
    public static bool HasDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (!seen.Add(keySelector(item)))
                return true;

        return false;
    }

    /// <summary>
    ///     Returns the single element of the sequence, or a default value if the sequence is empty
    ///     or contains more than one element.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>
    ///     The single element in <paramref name="source" />, or <c>default</c> if the sequence
    ///     is empty or contains more than one element.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})" />,
    ///     this method does not throw an exception when the sequence contains multiple elements.
    /// </remarks>
    public static T? SingleOrDefaultIfMultiple<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;

        var result = enumerator.Current;
        return enumerator.MoveNext() ? default : result;
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a sequence of reference types.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence (must be a reference type).</typeparam>
    /// <param name="source">The source sequence containing potentially <c>null</c> elements.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> that contains elements from <paramref name="source" />
    ///     that are not <c>null</c>.
    /// </returns>
    /// <seealso cref="WhereNotNull{T}(IEnumerable{T?})" />
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        foreach (var item in source)
            if (item is not null)
                yield return item;
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a sequence of nullable value types.
    /// </summary>
    /// <typeparam name="T">The underlying value type of the nullable elements.</typeparam>
    /// <param name="source">The source sequence containing potentially <c>null</c> elements.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> that contains the unwrapped values from
    ///     <paramref name="source" /> where the nullable has a value.
    /// </returns>
    /// <seealso cref="WhereNotNull{T}(IEnumerable{T?})" />
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        foreach (var item in source)
            if (item.HasValue)
                yield return item.Value;
    }

    /// <summary>
    ///     Filters elements that do not match the predicate. This is the inverse of
    ///     <see cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})" />.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to filter.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> that contains elements from <paramref name="source" />
    ///     for which <paramref name="predicate" /> returns <c>false</c>.
    /// </returns>
    public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
            if (!predicate(item))
                yield return item;
    }

    /// <summary>
    ///     Returns elements paired with their zero-based index in the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> of tuples where each tuple contains the element
    ///     and its zero-based index in the original sequence.
    /// </returns>
    /// <example>
    ///     <code>
    ///     foreach (var (item, index) in items.Indexed())
    ///         Console.WriteLine($"{index}: {item}");
    ///     </code>
    /// </example>
    /// <seealso cref="ForEach{T}(IEnumerable{T},Action{T,int})" />
    public static IEnumerable<(T Item, int Index)> Indexed<T>(this IEnumerable<T> source)
    {
        var index = 0;
        foreach (var item in source)
            yield return (item, index++);
    }

    /// <summary>
    ///     Projects consecutive pairs of elements into a new form.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the elements in the result sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">
    ///     A transform function that receives the previous and current element
    ///     and returns a projected value.
    /// </param>
    /// <returns>
    ///     An <see cref="IEnumerable{TResult}" /> containing the projected values from
    ///     consecutive element pairs. For a sequence of n elements, returns n-1 results.
    /// </returns>
    /// <example>
    ///     <code>
    ///     var deltas = values.ConsecutivePairs((prev, curr) => curr - prev);
    ///     </code>
    /// </example>
    /// <seealso cref="ConsecutivePairs{T}(IEnumerable{T})" />
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
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> of tuples containing consecutive element pairs.
    ///     For a sequence of n elements, returns n-1 tuples.
    /// </returns>
    /// <seealso cref="ConsecutivePairs{TSource,TResult}(IEnumerable{TSource},Func{TSource,TSource,TResult})" />
    public static IEnumerable<(T Previous, T Current)> ConsecutivePairs<T>(this IEnumerable<T> source)
    {
        return source.ConsecutivePairs(static (prev, curr) => (prev, curr));
    }

    /// <summary>
    ///     Finds the zero-based index of the first element in the list that matches the predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The zero-based index of the first element that matches the <paramref name="predicate" />,
    ///     or -1 if no element matches.
    /// </returns>
    /// <seealso cref="FirstOrDefaultFast{T}" />
    public static int IndexOf<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
            if (predicate(list[i]))
                return i;

        return -1;
    }

    /// <summary>
    ///     Returns the first element in the list that matches the predicate, or a default value
    ///     if no element matches.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The first element in <paramref name="list" /> that matches <paramref name="predicate" />,
    ///     or <c>default</c> if no element matches.
    /// </returns>
    /// <remarks>
    ///     This method is more efficient than
    ///     <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})" />
    ///     for <see cref="IList{T}" /> implementations as it uses indexed access instead of enumeration.
    /// </remarks>
    /// <seealso cref="IndexOf{T}" />
    public static T? FirstOrDefaultFast<T>(this IEnumerable<T> list, Func<T, bool> predicate)
    {
        foreach (var t in list)
            if (predicate(t))
                return t;

        return default;
    }

    /// <summary>
    ///     Partitions elements into two lists based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to partition.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     A tuple containing two lists: <c>Matching</c> contains elements for which
    ///     <paramref name="predicate" /> returns <c>true</c>, and <c>NotMatching</c>
    ///     contains elements for which it returns <c>false</c>.
    /// </returns>
    public static (List<T> Matching, List<T> NotMatching) Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in source)
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);

        return (matching, notMatching);
    }

    /// <summary>
    ///     Returns distinct elements from a sequence based on a key selector function.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> that contains distinct elements from
    ///     <paramref name="source" /> based on the keys.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses a <see cref="HashSet{T}" /> internally to track seen keys,
    ///         providing O(n) time complexity with O(k) memory where k is distinct key count.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Returns the first element for each unique key (preserves order)</description>
    ///         </item>
    ///         <item>
    ///             <description>Uses deferred execution - the sequence is enumerated lazily</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Key comparison uses <see cref="EqualityComparer{T}.Default" /> for
    ///                 <typeparamref name="TKey" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Get one method per unique name (first overload only)
    /// var uniqueMethods = typeSymbol.GetMembers()
    ///     .OfType&lt;IMethodSymbol&gt;()
    ///     .DistinctBy(m => m.Name);
    ///
    /// // Remove duplicate diagnostics by location
    /// var uniqueDiagnostics = diagnostics.DistinctBy(d => d.Location);
    /// </code>
    /// </example>
    /// <seealso cref="HasDuplicates{T,TKey}(IEnumerable{T},Func{T,TKey})" />
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (seen.Add(keySelector(item)))
                yield return item;
    }

    /// <summary>
    ///     Executes the specified action on each element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="action">The action to perform on each element.</param>
    /// <remarks>
    ///     This method is intended for side effects. For transformations,
    ///     use <see cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})" /> instead.
    /// </remarks>
    /// <seealso cref="ForEach{T}(IEnumerable{T},Action{T,int})" />
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    ///     Executes the specified action on each element of the sequence, providing the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="action">The action to perform on each element and its zero-based index.</param>
    /// <remarks>
    ///     This method is intended for side effects. For transformations with index,
    ///     use <see cref="Indexed{T}" /> instead.
    /// </remarks>
    /// <seealso cref="ForEach{T}(IEnumerable{T},Action{T})" />
    /// <seealso cref="Indexed{T}" />
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
            action(item, index++);
    }

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
    public static string JoinToString<T>(this IEnumerable<T> source, string separator = ", ") => string.Join(separator, source);

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
            if (item is TResult result)
                yield return result;
    }
}

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

/// <summary>
///     Extension methods for value tuples providing null-checking and enumeration capabilities.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods simplify common patterns when working with tuples that may contain
///         nullable elements, and provide convenient enumeration of tuple elements.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Null-checking methods work with tuples of 2 or 3 elements</description>
///         </item>
///         <item>
///             <description>Enumeration methods support tuples from 2 to 5 elements of the same type</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EnumerableExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ValueTupleExtensions
{
    /// <summary>
    ///     Determines whether any element in the 2-tuple is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if at least one element of the <paramref name="tuple" /> is not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AllNull{T1,T2}" />
    /// <seealso cref="AllNotNull{T1,T2}" />
    public static bool AnyNotNull<T1, T2>(this (T1?, T2?) tuple) => tuple.Item1 is not null || tuple.Item2 is not null;

    /// <summary>
    ///     Determines whether any element in the 3-tuple is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if at least one element of the <paramref name="tuple" /> is not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AllNull{T1,T2,T3}" />
    /// <seealso cref="AllNotNull{T1,T2,T3}" />
    public static bool AnyNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple) => tuple.Item1 is not null || tuple.Item2 is not null || tuple.Item3 is not null;

    /// <summary>
    ///     Determines whether all elements in the 2-tuple are <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2}" />
    /// <seealso cref="AllNotNull{T1,T2}" />
    public static bool AllNull<T1, T2>(this (T1?, T2?) tuple) => tuple.Item1 is null && tuple.Item2 is null;

    /// <summary>
    ///     Determines whether all elements in the 3-tuple are <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2,T3}" />
    /// <seealso cref="AllNotNull{T1,T2,T3}" />
    public static bool AllNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple) => tuple.Item1 is null && tuple.Item2 is null && tuple.Item3 is null;

    /// <summary>
    ///     Determines whether all elements in the 2-tuple are not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2}" />
    /// <seealso cref="AllNull{T1,T2}" />
    public static bool AllNotNull<T1, T2>(this (T1?, T2?) tuple) => tuple.Item1 is not null && tuple.Item2 is not null;

    /// <summary>
    ///     Determines whether all elements in the 3-tuple are not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2,T3}" />
    /// <seealso cref="AllNull{T1,T2,T3}" />
    public static bool AllNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple) => tuple.Item1 is not null && tuple.Item2 is not null && tuple.Item3 is not null;

    /// <summary>
    ///     Enumerates the elements of a 2-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <example>
    ///     <code>
    ///     var pair = ("first", "second");
    ///     foreach (var item in pair.Enumerate())
    ///         Console.WriteLine(item);
    ///     </code>
    /// </example>
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T})" />
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
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T})" />
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T})" />
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
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T})" />
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T, T})" />
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
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T})" />
    public static IEnumerable<T> Enumerate<T>(this (T, T, T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
        yield return tuple.Item4;
        yield return tuple.Item5;
    }
}
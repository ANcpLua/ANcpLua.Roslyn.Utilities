namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" /> and collections.
/// </summary>
/// <remarks>
///     <para>
///         A comprehensive set of LINQ-style extensions following the "null = empty" pattern. The
///         implementation is split across partial files by responsibility:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This file: null-safe operations + null-filtering (NotNull / WhereNotNull / WhereNot)</description>
///         </item>
///         <item>
///             <description><c>EnumerableExtensions.Single.cs</c> — Only / OnlyOrDefault / SingleOrDefaultIfMultiple</description>
///         </item>
///         <item>
///             <description><c>EnumerableExtensions.Duplicates.cs</c> — HasDuplicates / DistinctBy / Partition</description>
///         </item>
///         <item>
///             <description><c>EnumerableExtensions.Indexing.cs</c> — Indexed / ConsecutivePairs / IndexOf / ForEach</description>
///         </item>
///         <item>
///             <description><c>EnumerableExtensions.Aggregation.cs</c> — MinBy / MaxBy / Batch / Join / SkipLast / TakeLast / SafeCast</description>
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
    static partial class EnumerableExtensions
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
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? [];
    }

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
    public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null ? ImmutableArray<T>.Empty : [..source];
    }

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
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a sequence of reference types.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence containing potentially <c>null</c> elements.</param>
    /// <returns>An enumerable containing non-null elements from <paramref name="source" />.</returns>
    /// <seealso cref="WhereNotNull{T}(IEnumerable{T?})" />
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : class
    {
        Guard.NotNull(source);
        return source.WhereNotNull();
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a sequence of nullable value types.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The source sequence containing potentially <c>null</c> elements.</param>
    /// <returns>An enumerable containing present values from <paramref name="source" />.</returns>
    /// <seealso cref="WhereNotNull{T}(IEnumerable{T?})" />
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        Guard.NotNull(source);
        return source.WhereNotNull();
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
    /// <seealso cref="NotNull{T}(IEnumerable{T?})" />
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
    /// <seealso cref="NotNull{T}(IEnumerable{T?})" />
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
}

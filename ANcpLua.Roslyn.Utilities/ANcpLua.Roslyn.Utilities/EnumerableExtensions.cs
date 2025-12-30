using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" />.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     Concatenates strings and cleans up line breaks at the beginning and end of the resulting string.
    ///     Returns " " if collection is empty (to use with
    ///     <see cref="StringExtensions.RemoveBlankLinesWhereOnlyWhitespaces(string)" />).
    /// </summary>
    /// <param name="values">The string values to concatenate.</param>
    /// <returns>The concatenated string.</returns>
    public static string Inject(this IEnumerable<string> values)
    {
        var text = string.Concat(values)
            .TrimStart('\r', '\n')
            .TrimEnd('\r', '\n');
        return string.IsNullOrWhiteSpace(text) ? " " : text;
    }

    /// <summary>
    ///     Projects each element to an enumerable and flattens, returning empty if source is null.
    ///     Null-safe version of SelectMany.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TResult">The result element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="selector">The projection function.</param>
    /// <returns>The flattened enumerable.</returns>
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
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>The source or empty enumerable.</returns>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? [];
    }

    /// <summary>
    ///     Converts to ImmutableArray, returning empty if source is null.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>The immutable array.</returns>
    public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null ? [] : [.. source];
    }

    /// <summary>
    ///     Checks if the enumerable has duplicate elements.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>True if duplicates exist.</returns>
    public static bool HasDuplicates<T>(this IEnumerable<T> source)
    {
        var seen = new HashSet<T>();
        foreach (var item in source)
            if (!seen.Add(item))
                return true;

        return false;
    }

    /// <summary>
    ///     Checks if the enumerable has duplicate elements using a key selector.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">The key selector function.</param>
    /// <returns>True if duplicates exist.</returns>
    public static bool HasDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (!seen.Add(keySelector(item)))
                return true;

        return false;
    }

    /// <summary>
    ///     Returns distinct elements by a key selector.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">The key selector function.</param>
    /// <returns>The distinct elements.</returns>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (seen.Add(keySelector(item)))
                yield return item;
    }
}
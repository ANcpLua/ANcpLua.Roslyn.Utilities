using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" />.
/// </summary>
public static class EnumerableExtensions
{
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
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source) => source ?? [];

    /// <summary>
    ///     Converts to ImmutableArray, returning empty if source is null.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>The immutable array.</returns>
    public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null ? [] : [.. source];

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
        {
            if (!seen.Add(item))
                return true;
        }

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
    ///     Finds the index of the first element matching the predicate.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="predicate">The predicate function.</param>
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
}

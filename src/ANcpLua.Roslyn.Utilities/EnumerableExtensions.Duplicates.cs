namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class EnumerableExtensions
{
    /// <summary>
    ///     Determines whether the enumerable contains duplicate elements using the default equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to check for duplicates.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="source" /> contains at least one duplicate element; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         O(n) time using a <see cref="HashSet{T}" /> for O(1) lookup; short-circuits on the first
    ///         duplicate. Memory is O(k) where k is the number of unique elements seen before that
    ///         first duplicate.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HasDuplicates{T,TKey}(IEnumerable{T},Func{T,TKey})" />
    /// <seealso cref="DistinctBy{T,TKey}" />
    public static bool HasDuplicates<T>(this IEnumerable<T> source)
    {
        return ScanForDuplicate<T, T>(source, static x => x);
    }

    /// <summary>
    ///     Determines whether the enumerable contains duplicate elements based on a key selector.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <param name="source">The source sequence to check for duplicates.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="source" /> contains elements with duplicate keys; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="HasDuplicates{T}(IEnumerable{T})" />
    /// <seealso cref="DistinctBy{T,TKey}" />
    public static bool HasDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        return ScanForDuplicate(source, keySelector);
    }

    // Shared scanner. Both HasDuplicates overloads delegate here, so each public
    // method stays at CC=1 and the seen-hashset logic lives in one place.
    private static bool ScanForDuplicate<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (!seen.Add(keySelector(item)))
                return true;

        return false;
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
    ///     <paramref name="source" /> based on the keys. Preserves order; the first occurrence of each
    ///     unique key is yielded.
    /// </returns>
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
            (predicate(item) ? matching : notMatching).Add(item);

        return (matching, notMatching);
    }
}

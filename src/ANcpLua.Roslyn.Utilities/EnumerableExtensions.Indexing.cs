namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class EnumerableExtensions
{
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
    /// <seealso cref="IndexOf{T}" />
    public static T? FirstOrDefaultFast<T>(this IEnumerable<T> list, Func<T, bool> predicate)
    {
        foreach (var t in list)
            if (predicate(t))
                return t;

        return default;
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
    ///     For transformations with index, use <see cref="Indexed{T}" /> instead.
    /// </remarks>
    /// <seealso cref="ForEach{T}(IEnumerable{T},Action{T})" />
    /// <seealso cref="Indexed{T}" />
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
            action(item, index++);
    }
}

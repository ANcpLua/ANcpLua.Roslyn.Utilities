namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="List{T}" /> that provide allocation-free patterns
///     by using context parameters instead of closures.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods mirror the standard <see cref="List{T}" /> methods but accept
///         an additional context parameter. This design pattern eliminates closure allocations
///         that would otherwise occur when capturing local variables in lambda expressions.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 All methods accept a <c>context</c> parameter that is passed to the predicate,
///                 allowing the use of <c>static</c> lambdas.
///             </description>
///         </item>
///         <item>
///             <description>
///                 These methods are particularly useful in hot paths such as Roslyn analyzers
///                 and source generators where allocation pressure must be minimized.
///             </description>
///         </item>
///         <item>
///             <description>
///                 The context pattern enables the compiler to avoid generating a closure class,
///                 reducing both memory allocations and garbage collection overhead.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class ListExtensions
{
    /// <summary>
    ///     Removes all elements that match the conditions defined by the specified predicate.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the match predicate.</typeparam>
    /// <param name="list">The list to remove elements from.</param>
    /// <param name="context">Context data passed to the <paramref name="match" /> predicate.</param>
    /// <param name="match">
    ///     The predicate that determines whether an element should be removed.
    ///     Receives the <paramref name="context" /> as the first argument and the element as the second.
    /// </param>
    /// <returns>The number of elements removed from the <paramref name="list" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This method uses an in-place compaction algorithm that maintains relative order
    ///         of the remaining elements. Elements matching the predicate are removed by
    ///         shifting non-matching elements to fill the gaps.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Without context (causes closure allocation):
    /// list.RemoveAll(item => item.Category == category);
    ///
    /// // With context (allocation-free):
    /// list.RemoveAll(category, static (ctx, item) => item.Category == ctx);
    /// </code>
    /// </example>
    /// <seealso cref="Find{T,TContext}" />
    /// <seealso cref="FindIndex{T,TContext}" />
    /// <seealso cref="Exists{T,TContext}" />
    public static int RemoveAll<T, TContext>(
        this List<T> list,
        TContext context,
        Func<TContext, T, bool> match)
    {
        var freeIndex = 0;
        var count = list.Count;

        // Find first element to remove
        while (freeIndex < count && !match(context, list[freeIndex]))
            freeIndex++;

        if (freeIndex >= count)
            return 0;

        // Compact remaining elements
        var current = freeIndex + 1;
        while (current < count)
        {
            // Skip elements to remove
            while (current < count && match(context, list[current]))
                current++;

            // Move non-matching element to free slot
            if (current < count)
                list[freeIndex++] = list[current++];
        }

        var removedCount = count - freeIndex;
        list.RemoveRange(freeIndex, removedCount);
        return removedCount;
    }

    /// <summary>
    ///     Finds the first element that matches the conditions defined by the specified predicate.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the match predicate.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="context">Context data passed to the <paramref name="match" /> predicate.</param>
    /// <param name="match">
    ///     The predicate that determines whether an element matches.
    ///     Receives the <paramref name="context" /> as the first argument and the element as the second.
    /// </param>
    /// <returns>
    ///     The first element that matches the predicate, or the default value of <typeparamref name="T" />
    ///     if no match is found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The search is performed in order from the beginning of the list. The method returns
    ///         immediately upon finding the first matching element.
    ///     </para>
    /// </remarks>
    /// <seealso cref="FindIndex{T,TContext}" />
    /// <seealso cref="Exists{T,TContext}" />
    /// <seealso cref="RemoveAll{T,TContext}" />
    public static T? Find<T, TContext>(
        this List<T> list,
        TContext context,
        Func<TContext, T, bool> match)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (match(context, list[i]))
                return list[i];
        }

        return default;
    }

    /// <summary>
    ///     Finds the index of the first element that matches the conditions defined by the specified predicate.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the match predicate.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="context">Context data passed to the <paramref name="match" /> predicate.</param>
    /// <param name="match">
    ///     The predicate that determines whether an element matches.
    ///     Receives the <paramref name="context" /> as the first argument and the element as the second.
    /// </param>
    /// <returns>
    ///     The zero-based index of the first element that matches the predicate,
    ///     or <c>-1</c> if no match is found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The search is performed in order from index 0. The method returns
    ///         immediately upon finding the first matching element.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Find{T,TContext}" />
    /// <seealso cref="Exists{T,TContext}" />
    /// <seealso cref="RemoveAll{T,TContext}" />
    public static int FindIndex<T, TContext>(
        this List<T> list,
        TContext context,
        Func<TContext, T, bool> match)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (match(context, list[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Determines whether any element matches the conditions defined by the specified predicate.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the match predicate.</typeparam>
    /// <param name="list">The list to search.</param>
    /// <param name="context">Context data passed to the <paramref name="match" /> predicate.</param>
    /// <param name="match">
    ///     The predicate that determines whether an element matches.
    ///     Receives the <paramref name="context" /> as the first argument and the element as the second.
    /// </param>
    /// <returns>
    ///     <c>true</c> if any element in the <paramref name="list" /> matches the predicate;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The search is performed in order from the beginning of the list. The method returns
    ///         <c>true</c> immediately upon finding the first matching element.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Find{T,TContext}" />
    /// <seealso cref="FindIndex{T,TContext}" />
    /// <seealso cref="RemoveAll{T,TContext}" />
    public static bool Exists<T, TContext>(
        this List<T> list,
        TContext context,
        Func<TContext, T, bool> match)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (match(context, list[i]))
                return true;
        }

        return false;
    }
}

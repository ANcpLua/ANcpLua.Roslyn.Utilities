namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="List{T}" /> with allocation-free patterns.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    ///     Removes all elements that match the conditions defined by the specified predicate.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the match predicate.</typeparam>
    /// <param name="list">The list to remove elements from.</param>
    /// <param name="context">Context data passed to the match predicate.</param>
    /// <param name="match">The predicate that determines whether an element should be removed.</param>
    /// <returns>The number of elements removed from the list.</returns>
    /// <example>
    ///     <code>
    /// // Without context (causes closure allocation):
    /// list.RemoveAll(item => item.Category == category);
    ///
    /// // With context (allocation-free):
    /// list.RemoveAll(category, static (ctx, item) => item.Category == ctx);
    /// </code>
    /// </example>
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
    /// <param name="context">Context data passed to the match predicate.</param>
    /// <param name="match">The predicate that determines whether an element matches.</param>
    /// <returns>The first matching element, or default if no match is found.</returns>
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
    /// <param name="context">Context data passed to the match predicate.</param>
    /// <param name="match">The predicate that determines whether an element matches.</param>
    /// <returns>The index of the first matching element, or -1 if no match is found.</returns>
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
    /// <param name="context">Context data passed to the match predicate.</param>
    /// <param name="match">The predicate that determines whether an element matches.</param>
    /// <returns>True if any element matches; otherwise, false.</returns>
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

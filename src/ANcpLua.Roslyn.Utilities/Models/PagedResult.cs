namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Cursor-paginated response wrapper.
///     Represents a single page of results with an optional continuation cursor.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The items in this page of results.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="Cursor">An opaque continuation token for the next page, or <see langword="null" /> if this is the last page.</param>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        string? Cursor);

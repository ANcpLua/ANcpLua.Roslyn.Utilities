namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Quoting for SQL delimited (double-quoted) identifiers.
/// </summary>
/// <remarks>
///     Follows the ANSI SQL rule shared by DuckDB, PostgreSQL, and standard SQL: a delimited identifier is
///     wrapped in double quotes and any embedded double quote is doubled (<c>"</c> becomes <c>""</c>).
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SqlIdentifier
{
    /// <summary>
    ///     Wraps <paramref name="identifier" /> in double quotes, doubling any embedded double quote.
    /// </summary>
    /// <param name="identifier">The raw identifier (e.g. a table or column name).</param>
    /// <returns>The quoted identifier, e.g. <c>col</c> becomes <c>"col"</c> and <c>a"b</c> becomes <c>"a""b"</c>.</returns>
    public static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}

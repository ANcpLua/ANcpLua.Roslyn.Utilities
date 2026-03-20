using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Parses SQL statements to extract the primary operation type per OTel semantic conventions.
/// </summary>
/// <remarks>
///     <para>
///         Handles common SQL patterns including:
///     </para>
///     <list type="bullet">
///         <item><description>Single-line comments (<c>-- ...</c>)</description></item>
///         <item><description>Block comments (<c>/* ... */</c>), including nested</description></item>
///         <item><description>Common Table Expressions (<c>WITH ... SELECT/INSERT/UPDATE/DELETE</c>)</description></item>
///         <item><description>Leading whitespace</description></item>
///     </list>
///     <para>
///         Uses <see cref="ReadOnlySpan{T}" />-based parsing for zero-allocation where possible.
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SqlOperationParser
{
    /// <summary>
    ///     Attempts to extract the SQL operation from a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to parse.</param>
    /// <returns>
    ///     The operation name (e.g. <c>"SELECT"</c>, <c>"INSERT"</c>, <c>"UPDATE"</c>, <c>"DELETE"</c>)
    ///     or <c>null</c> if the statement is empty or unparseable.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? TryParse(string? sql) =>
        string.IsNullOrWhiteSpace(sql) ? null : TryParseCore(sql.AsSpan());

    /// <summary>
    ///     Attempts to extract the primary table/collection name from a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to parse.</param>
    /// <returns>
    ///     The collection (table) name or <c>null</c> if the statement is empty or unparseable.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? TryParseCollectionName(string? sql) =>
        string.IsNullOrWhiteSpace(sql) ? null : TryParseCollectionNameCore(sql.AsSpan());

    /// <summary>
    ///     Core parsing logic using <see cref="ReadOnlySpan{T}" /> for zero-allocation.
    /// </summary>
    private static string? TryParseCore(ReadOnlySpan<char> sql)
    {
        // Skip leading whitespace and comments
        sql = SkipWhitespaceAndComments(sql);

        if (sql.IsEmpty)
            return null;

        // Check for CTE (WITH ... AS ...)
        if (StartsWithKeyword(sql, "WITH"))
        {
            // Find the actual operation after the CTE
            var afterCte = SkipCte(sql);
            if (!afterCte.IsEmpty)
                sql = afterCte;
        }

        // Match known SQL operations
        return MatchOperation(sql);
    }

    /// <summary>
    ///     Skips leading whitespace and SQL comments.
    /// </summary>
    private static ReadOnlySpan<char> SkipWhitespaceAndComments(ReadOnlySpan<char> sql)
    {
        while (!sql.IsEmpty)
        {
            // Skip whitespace
            sql = sql.TrimStart();

            if (sql.IsEmpty)
                return sql;

            // Check for single-line comment: --
            if (sql.Length >= 2 && sql[0] == '-' && sql[1] == '-')
            {
                var newlineIndex = sql.IndexOfAny('\r', '\n');
                sql = newlineIndex < 0 ? ReadOnlySpan<char>.Empty : sql[(newlineIndex + 1)..];
                continue;
            }

            // Check for block comment: /* ... */
            if (sql.Length >= 2 && sql[0] == '/' && sql[1] == '*')
            {
                var endIndex = IndexOfBlockCommentEnd(sql, 2);
                sql = endIndex < 0 ? ReadOnlySpan<char>.Empty : sql[(endIndex + 2)..];
                continue;
            }

            // No more comments to skip
            break;
        }

        return sql;
    }

    /// <summary>
    ///     Finds the end of a block comment, handling nested comments.
    /// </summary>
    private static int IndexOfBlockCommentEnd(ReadOnlySpan<char> sql, int start)
    {
        var depth = 1;
        for (var i = start; i < sql.Length - 1; i++)
        {
            if (sql[i] == '/' && sql[i + 1] == '*')
            {
                depth++;
                i++; // Skip the '*'
            }
            else if (sql[i] == '*' && sql[i + 1] == '/')
            {
                depth--;
                if (depth is 0)
                    return i;
                i++; // Skip the '/'
            }
        }

        return -1; // Unterminated comment
    }

    /// <summary>
    ///     Skips a Common Table Expression (<c>WITH ... AS (...)</c>) to find the main operation.
    /// </summary>
    private static ReadOnlySpan<char> SkipCte(ReadOnlySpan<char> sql)
    {
        // Skip "WITH" keyword
        sql = sql[4..];
        sql = SkipWhitespaceAndComments(sql);

        // Handle RECURSIVE keyword
        if (StartsWithKeyword(sql, "RECURSIVE"))
        {
            sql = sql[9..];
            sql = SkipWhitespaceAndComments(sql);
        }

        // CTEs can have multiple definitions separated by commas
        // WITH cte1 AS (...), cte2 AS (...) SELECT/INSERT/UPDATE/DELETE
        while (!sql.IsEmpty)
        {
            // Skip CTE name
            sql = SkipIdentifier(sql);
            sql = SkipWhitespaceAndComments(sql);

            // Optional column list: (col1, col2)
            if (!sql.IsEmpty && sql[0] == '(')
            {
                sql = SkipParenthesizedBlock(sql);
                sql = SkipWhitespaceAndComments(sql);
            }

            // Expect "AS"
            if (!StartsWithKeyword(sql, "AS"))
                return sql; // Malformed, return what we have

            sql = sql[2..];
            sql = SkipWhitespaceAndComments(sql);

            // Skip the CTE body: (SELECT ...)
            if (!sql.IsEmpty && sql[0] == '(')
            {
                sql = SkipParenthesizedBlock(sql);
                sql = SkipWhitespaceAndComments(sql);
            }

            // Check for comma (another CTE) or end
            if (sql.IsEmpty || sql[0] != ',')
                break;

            sql = sql[1..]; // Skip comma
            sql = SkipWhitespaceAndComments(sql);
        }

        return sql;
    }

    /// <summary>
    ///     Skips an identifier (table name, column name, etc.).
    /// </summary>
    private static ReadOnlySpan<char> SkipIdentifier(ReadOnlySpan<char> sql)
    {
        if (sql.IsEmpty)
            return sql;

        // Handle quoted identifiers
        if (sql[0] is '"' or '[' or '`')
        {
            var closeChar = sql[0] switch
            {
                '"' => '"',
                '[' => ']',
                '`' => '`',
                _ => sql[0]
            };

            var endIndex = sql[1..].IndexOf(closeChar);
            return endIndex < 0 ? ReadOnlySpan<char>.Empty : sql[(endIndex + 2)..];
        }

        // Unquoted identifier: letters, digits, underscores
        var i = 0;
        while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_'))
            i++;

        return sql[i..];
    }

    /// <summary>
    ///     Skips a parenthesized block, handling nested parentheses.
    /// </summary>
    private static ReadOnlySpan<char> SkipParenthesizedBlock(ReadOnlySpan<char> sql)
    {
        if (sql.IsEmpty || sql[0] != '(')
            return sql;

        var depth = 1;
        for (var i = 1; i < sql.Length; i++)
        {
            switch (sql[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    if (depth is 0)
                        return sql[(i + 1)..];
                    break;
                case '\'': // Skip string literals
                    i = SkipStringLiteral(sql, i);
                    break;
            }
        }

        return ReadOnlySpan<char>.Empty; // Unterminated
    }

    /// <summary>
    ///     Skips a string literal, handling escaped quotes.
    /// </summary>
    private static int SkipStringLiteral(ReadOnlySpan<char> sql, int start)
    {
        for (var i = start + 1; i < sql.Length; i++)
        {
            if (sql[i] == '\'')
            {
                // Check for escaped quote ('')
                if (i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    i++; // Skip the escaped quote
                    continue;
                }

                return i;
            }
        }

        return sql.Length - 1;
    }

    /// <summary>
    ///     Checks if the SQL starts with a keyword (case-insensitive, word boundary).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool StartsWithKeyword(ReadOnlySpan<char> sql, ReadOnlySpan<char> keyword)
    {
        if (sql.Length < keyword.Length)
            return false;

        if (!sql.Slice(0, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        // Ensure word boundary (not followed by letter/digit/underscore)
        if (sql.Length > keyword.Length)
        {
            var nextChar = sql[keyword.Length];
            if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Extracts the primary table name from a SQL statement.
    ///     Handles <c>SELECT...FROM</c>, <c>INSERT INTO</c>, <c>UPDATE</c>, <c>DELETE FROM</c> patterns.
    /// </summary>
    private static string? TryParseCollectionNameCore(ReadOnlySpan<char> sql)
    {
        sql = SkipWhitespaceAndComments(sql);

        if (sql.IsEmpty)
            return null;

        // Handle CTE
        if (StartsWithKeyword(sql, "WITH"))
        {
            var afterCte = SkipCte(sql);
            if (!afterCte.IsEmpty)
                sql = afterCte;
        }

        // SELECT ... FROM <table>
        if (StartsWithKeyword(sql, "SELECT"))
        {
            var fromIdx = FindKeyword(sql, "FROM");
            if (fromIdx < 0)
                return null;

            var afterFrom = SkipWhitespaceAndComments(sql[(fromIdx + 4)..]);
            return ExtractIdentifier(afterFrom);
        }

        // INSERT INTO <table>
        if (StartsWithKeyword(sql, "INSERT"))
        {
            sql = sql[6..];
            sql = SkipWhitespaceAndComments(sql);
            if (StartsWithKeyword(sql, "INTO"))
            {
                sql = sql[4..];
                sql = SkipWhitespaceAndComments(sql);
            }

            return ExtractIdentifier(sql);
        }

        // UPDATE <table>
        if (StartsWithKeyword(sql, "UPDATE"))
        {
            sql = sql[6..];
            sql = SkipWhitespaceAndComments(sql);
            return ExtractIdentifier(sql);
        }

        // DELETE FROM <table>
        if (StartsWithKeyword(sql, "DELETE"))
        {
            sql = sql[6..];
            sql = SkipWhitespaceAndComments(sql);
            if (StartsWithKeyword(sql, "FROM"))
            {
                sql = sql[4..];
                sql = SkipWhitespaceAndComments(sql);
            }

            return ExtractIdentifier(sql);
        }

        return null;
    }

    /// <summary>
    ///     Finds a keyword in SQL (case-insensitive, at word boundary), skipping parenthesized subqueries.
    /// </summary>
    private static int FindKeyword(ReadOnlySpan<char> sql, ReadOnlySpan<char> keyword)
    {
        var depth = 0;
        for (var i = 0; i <= sql.Length - keyword.Length; i++)
        {
            switch (sql[i])
            {
                case '(':
                    depth++;
                    continue;
                case ')':
                    depth--;
                    continue;
                case '\'':
                    i = SkipStringLiteral(sql, i);
                    continue;
            }

            if (depth > 0)
                continue;

            if (StartsWithKeyword(sql[i..], keyword))
            {
                // Ensure word boundary before keyword
                if (i > 0 && (char.IsLetterOrDigit(sql[i - 1]) || sql[i - 1] == '_'))
                    continue;

                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Extracts an identifier (table name) from the current position.
    /// </summary>
    private static string? ExtractIdentifier(ReadOnlySpan<char> sql)
    {
        if (sql.IsEmpty)
            return null;

        // Handle quoted identifiers
        if (sql[0] is '"' or '[' or '`')
        {
            var closeChar = sql[0] switch
            {
                '"' => '"',
                '[' => ']',
                _ => '`'
            };

            var endIndex = sql[1..].IndexOf(closeChar);
            return endIndex > 0 ? sql.Slice(1, endIndex).ToString() : null;
        }

        // Unquoted: letters, digits, underscores, dots (for schema.table)
        var i = 0;
        while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] is '_' or '.'))
            i++;

        return i > 0 ? sql.Slice(0, i).ToString() : null;
    }

    /// <summary>
    ///     Matches the SQL operation at the current position.
    /// </summary>
    private static string? MatchOperation(ReadOnlySpan<char> sql)
    {
        // Order by frequency in typical workloads
        if (StartsWithKeyword(sql, "SELECT")) return "SELECT";
        if (StartsWithKeyword(sql, "INSERT")) return "INSERT";
        if (StartsWithKeyword(sql, "UPDATE")) return "UPDATE";
        if (StartsWithKeyword(sql, "DELETE")) return "DELETE";
        if (StartsWithKeyword(sql, "CREATE")) return "CREATE";
        if (StartsWithKeyword(sql, "DROP")) return "DROP";
        if (StartsWithKeyword(sql, "ALTER")) return "ALTER";
        if (StartsWithKeyword(sql, "TRUNCATE")) return "TRUNCATE";
        if (StartsWithKeyword(sql, "MERGE")) return "MERGE";
        if (StartsWithKeyword(sql, "CALL")) return "CALL";
        if (StartsWithKeyword(sql, "EXEC")) return "EXECUTE";
        if (StartsWithKeyword(sql, "EXECUTE")) return "EXECUTE";
        if (StartsWithKeyword(sql, "BEGIN")) return "BEGIN";
        if (StartsWithKeyword(sql, "COMMIT")) return "COMMIT";

        return StartsWithKeyword(sql, "ROLLBACK") ? "ROLLBACK" : null;
    }
}

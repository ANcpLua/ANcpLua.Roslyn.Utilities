namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    private const char DoubleQuoteChar = '"';
    private const char SingleQuoteChar = '\'';

    // ========== Double-quoting ==========

    /// <summary>
    ///     Wraps the string in double quotes if it contains a space (default trigger).
    /// </summary>
    /// <param name="str">The string to conditionally quote.</param>
    /// <returns>The original string if already quoted or no disallowed chars found; otherwise double-quoted.</returns>
    public static string DoubleQuoteIfNeeded(this string? str)
    {
        return str.DoubleQuoteIfNeeded(' ');
    }

    /// <summary>
    ///     Wraps the string in double quotes if it contains any of the specified disallowed characters.
    /// </summary>
    /// <param name="str">The string to conditionally quote.</param>
    /// <param name="disallowed">Characters that trigger quoting.</param>
    /// <returns>The original string if already quoted or no disallowed chars found; otherwise double-quoted.</returns>
    public static string DoubleQuoteIfNeeded(this string? str, params char[] disallowed)
    {
        return QuoteIfNeeded(str, DoubleQuoteChar, disallowed);
    }

    /// <summary>
    ///     Wraps the string in double quotes, escaping any existing double quotes.
    /// </summary>
    /// <param name="str">The string to quote.</param>
    /// <returns>The double-quoted string.</returns>
    public static string DoubleQuote(this string? str)
    {
        return Quote(str, DoubleQuoteChar);
    }

    // ========== Single-quoting ==========

    /// <summary>
    ///     Wraps the string in single quotes if it contains a space (default trigger).
    /// </summary>
    /// <param name="str">The string to conditionally quote.</param>
    /// <returns>The original string if already quoted or no disallowed chars found; otherwise single-quoted.</returns>
    public static string SingleQuoteIfNeeded(this string? str)
    {
        return str.SingleQuoteIfNeeded(' ');
    }

    /// <summary>
    ///     Wraps the string in single quotes if it contains any of the specified disallowed characters.
    /// </summary>
    /// <param name="str">The string to conditionally quote.</param>
    /// <param name="disallowed">Characters that trigger quoting.</param>
    /// <returns>The original string if already quoted or no disallowed chars found; otherwise single-quoted.</returns>
    public static string SingleQuoteIfNeeded(this string? str, params char[] disallowed)
    {
        return QuoteIfNeeded(str, SingleQuoteChar, disallowed);
    }

    /// <summary>
    ///     Wraps the string in single quotes, escaping any existing single quotes.
    /// </summary>
    /// <param name="str">The string to quote.</param>
    /// <returns>The single-quoted string.</returns>
    public static string SingleQuote(this string? str)
    {
        return Quote(str, SingleQuoteChar);
    }

    /// <summary>
    ///     Checks whether the string is wrapped in double quotes.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <returns><c>true</c> if the string starts and ends with a double quote.</returns>
    public static bool IsDoubleQuoted([NotNullWhen(true)] this string? str)
    {
        return IsQuoted(str, DoubleQuoteChar);
    }

    /// <summary>
    ///     Checks whether the string is wrapped in single quotes.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <returns><c>true</c> if the string starts and ends with a single quote.</returns>
    public static bool IsSingleQuoted([NotNullWhen(true)] this string? str)
    {
        return IsQuoted(str, SingleQuoteChar);
    }

    // ========== Graph label escaping ==========

    /// <summary>
    ///     Escapes a string for use as a label in Graphviz DOT format.
    /// </summary>
    /// <param name="label">The label text to escape.</param>
    /// <returns>The escaped label safe for use inside DOT double-quoted strings.</returns>
    /// <remarks>
    ///     Escapes backslashes, double quotes, and newlines which are special characters in DOT labels.
    /// </remarks>
    public static string EscapeDotLabel(this string label)
    {
        return label
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");
    }

    /// <summary>
    ///     Escapes a string for use as a label in Mermaid diagram format.
    /// </summary>
    /// <param name="label">The label text to escape.</param>
    /// <returns>The escaped label safe for use in Mermaid node and edge labels.</returns>
    /// <remarks>
    ///     Encodes double quotes as HTML entities and newlines as HTML line breaks,
    ///     which Mermaid renderers interpret correctly.
    /// </remarks>
    public static string EscapeMermaidLabel(this string label)
    {
        return label
            .Replace("\"", "&quot;")
            .Replace("\n", "<br/>");
    }

    // ========== Private quote primitives ==========

    private static string QuoteIfNeeded(string? str, char quote, params char[] disallowed)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        if (IsQuoted(str, quote) || str!.AsSpan().IndexOfAny(disallowed) < 0)
            return str!;

        return Quote(str, quote);
    }

    private static string Quote(string? str, char quote)
    {
        var escaped = str?.Replace(quote.ToString(), "\\" + quote);
        return string.Concat(quote, escaped, quote);
    }

    private static bool IsQuoted([NotNullWhen(true)] string? str, char quote)
    {
        return str is [var first, .., var last] && first == quote && last == quote;
    }
}

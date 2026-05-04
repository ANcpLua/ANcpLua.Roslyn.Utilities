namespace ANcpLua.Roslyn.Utilities.Text;

/// <summary>Small markdown-aware string helpers — table-cell truncation, pipe escaping — for LLM-facing response formatters.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class MarkdownText
{
    /// <summary>
    ///     Truncates <paramref name="text" /> to at most <paramref name="maxLength" /> characters,
    ///     appending <c>"…"</c> (single-char ellipsis) when trimmed so the total stays within <paramref name="maxLength" />.
    ///     Null/empty input returns an empty string.
    /// </summary>
    public static string TruncateCell(string? text, int maxLength = 50)
    {
        if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be at least 1.");
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text!.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
    }

    /// <summary>Escapes <c>|</c> and newlines so <paramref name="text" /> can be safely embedded in a markdown table cell.</summary>
    public static string EscapeCell(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text!
            .Replace("\\", "\\\\")
            .Replace("|", "\\|")
            .Replace("\r\n", " ")
            .Replace('\n', ' ')
            .Replace('\r', ' ');
    }
}

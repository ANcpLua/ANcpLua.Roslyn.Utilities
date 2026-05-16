using System.Text.RegularExpressions;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    private static readonly char[] s_newLineSeparator = ['\n'];

    private static readonly Regex s_whitespaceRegexInstance = new(@"\s+", RegexOptions.Compiled);

    // Pre-compiled regexes used by CleanWhiteSpace. Hoisting to static readonly
    // amortises the compilation cost across source-generator runs.
    private static readonly Regex s_trailingWhitespaceRegex = new(@"[ \t]+(\r?\n)", RegexOptions.Compiled);
    private static readonly Regex s_collapseEmptyLinesRegex = new(@"(\r?\n){3,}", RegexOptions.Compiled);
    private static readonly Regex s_emptyAfterOpenRegex = new(@"([\{\]])(\r?\n){2}", RegexOptions.Compiled);
    private static readonly Regex s_emptyBeforeCloseRegex = new(@"(\r?\n){2}([\}])", RegexOptions.Compiled);

    private static Regex WhitespaceRegex()
    {
        return s_whitespaceRegexInstance;
    }

    /// <summary>
    ///     Removes lines that contain only whitespace characters while preserving empty lines.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>
    ///     The text with whitespace-only lines removed. Lines that are completely empty
    ///     (zero length) are preserved.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="text" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         A "blank line" is defined as a line containing one or more whitespace characters
    ///         but no other content. Empty lines (containing no characters) are not considered blank
    ///         and are preserved in the output.
    ///     </para>
    /// </remarks>
    /// <seealso cref="CleanWhiteSpace" />
    /// <seealso cref="NormalizeLineEndings" />
    public static string TrimBlankLines(this string text)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));
        var lines = text.NormalizeLineEndings().Split(s_newLineSeparator, StringSplitOptions.None);
        var result = new StringBuilder();
        var first = true;

        foreach (var line in lines)
            if (!IsBlankLine(line))
            {
                if (!first)
                    result.Append('\n');
                result.Append(line);
                first = false;
            }

        return result.ToString();
    }

    private static bool IsBlankLine(string line)
    {
        if (line.Length is 0)
            return false;

        foreach (var c in line)
            if (!char.IsWhiteSpace(c))
                return false;

        return true;
    }

    /// <summary>
    ///     Normalizes all line endings in a string to a consistent format.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <param name="newLine">
    ///     The line ending sequence to use. If <c>null</c>, uses <c>\n</c> (Unix-style).
    /// </param>
    /// <returns>
    ///     The text with all line endings (<c>\r\n</c>, <c>\r</c>, and <c>\n</c>)
    ///     replaced with the specified <paramref name="newLine" /> sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="text" /> is <c>null</c>.
    /// </exception>
    /// <seealso cref="TrimBlankLines" />
    /// <seealso cref="CleanWhiteSpace" />
    public static string NormalizeLineEndings(this string text, string? newLine = null)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));

        var newText = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        if (newLine is not null) newText = newText.Replace("\n", newLine);

        return newText;
    }

    /// <summary>
    ///     Cleans whitespace in generated source code for consistent formatting.
    /// </summary>
    /// <param name="source">The generated source code to clean.</param>
    /// <returns>The cleaned source code with normalized whitespace.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="source" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     <para>Performs the following cleanup operations:</para>
    ///     <list type="bullet">
    ///         <item><description>Strips trailing whitespace (spaces and tabs) from all lines</description></item>
    ///         <item><description>Collapses 3 or more consecutive empty lines to exactly 2</description></item>
    ///         <item><description>Removes empty lines immediately after <c>{</c> or <c>]</c></description></item>
    ///         <item><description>Removes empty lines immediately before <c>}</c></description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="TrimBlankLines" />
    /// <seealso cref="NormalizeLineEndings" />
    public static string CleanWhiteSpace(this string source)
    {
        source = source ?? throw new ArgumentNullException(nameof(source));

        source = s_trailingWhitespaceRegex.Replace(source, "$1");
        source = s_collapseEmptyLinesRegex.Replace(source, "$1$1");
        source = s_emptyAfterOpenRegex.Replace(source, "$1$2");
        source = s_emptyBeforeCloseRegex.Replace(source, "$1$2");

        return source;
    }

    /// <summary>
    ///     Normalizes whitespace by collapsing all whitespace sequences (including newlines) into a single space.
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <returns>
    ///     A string with all whitespace collapsed to single spaces and trimmed.
    ///     Returns <see cref="string.Empty" /> if input is null or whitespace.
    /// </returns>
    public static string NormalizeWhitespace(this string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : WhitespaceRegex().Replace(input, " ").Trim();
    }
}

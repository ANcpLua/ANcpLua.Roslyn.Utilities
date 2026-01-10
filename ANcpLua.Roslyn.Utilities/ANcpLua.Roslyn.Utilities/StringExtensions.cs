using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for string manipulation in source generators.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods commonly needed when generating source code,
///         including line splitting, casing transformations, and whitespace normalization.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Zero-allocation line enumeration via <see cref="SplitLines(string)" /> and
///                 <see cref="LineSplitEnumerator" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Identifier casing with C# keyword handling via <see cref="ToPropertyName" /> and
///                 <see cref="ToParameterName" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Whitespace cleanup for generated code via <see cref="TrimBlankLines" />,
///                 <see cref="NormalizeLineEndings" />, and <see cref="CleanWhiteSpace" />
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class StringExtensions
{
    private static readonly char[] NewLineSeparator = ['\n'];

    /// <summary>
    ///     Splits a string into lines without allocating intermediate string arrays.
    /// </summary>
    /// <param name="str">The string to split into lines.</param>
    /// <returns>
    ///     A <see cref="LineSplitEnumerator" /> that can be used in a foreach loop to enumerate lines.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method returns a ref struct enumerator that avoids heap allocations,
    ///         making it ideal for performance-critical scenarios in source generators.
    ///     </para>
    ///     <para>
    ///         The enumerator handles both <c>\n</c> and <c>\r\n</c> line endings correctly.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SplitLines(ReadOnlySpan{char})" />
    /// <seealso cref="LineSplitEnumerator" />
    /// <seealso cref="LineSplitEntry" />
    public static LineSplitEnumerator SplitLines(this string str) => new(str.AsSpan());

    /// <summary>
    ///     Splits a character span into lines without allocating intermediate string arrays.
    /// </summary>
    /// <param name="str">The span of characters to split into lines.</param>
    /// <returns>
    ///     A <see cref="LineSplitEnumerator" /> that can be used in a foreach loop to enumerate lines.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This overload accepts a <see cref="ReadOnlySpan{T}" /> directly for scenarios
    ///         where you already have a span and want to avoid allocating a string.
    ///     </para>
    ///     <para>
    ///         The enumerator handles both <c>\n</c> and <c>\r\n</c> line endings correctly.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SplitLines(string)" />
    /// <seealso cref="LineSplitEnumerator" />
    /// <seealso cref="LineSplitEntry" />
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str) => new(str);

    /// <summary>
    ///     Converts a string to PascalCase by making the first character uppercase.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>
    ///     The input string with its first character converted to uppercase using invariant culture rules.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="input" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="input" /> is an empty string.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method is useful for converting field names to property names in source generators.
    ///         For example, <c>"firstName"</c> becomes <c>"FirstName"</c>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ToParameterName" />
    public static string ToPropertyName(this string input)
    {
        return input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
#if NET6_0_OR_GREATER
            _ => string.Concat(input[0].ToString().ToUpper(CultureInfo.InvariantCulture), input.AsSpan(1)),
#else
            _ => input[0].ToString().ToUpper(CultureInfo.InvariantCulture) + input[1..],
#endif
        };
    }

    /// <summary>
    ///     Converts a string to camelCase by making the first character lowercase,
    ///     and escapes C# reserved keywords with the <c>@</c> prefix.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>
    ///     The input string converted to a valid C# parameter name. If the resulting name
    ///     is a C# keyword, it is prefixed with <c>@</c> (e.g., <c>"class"</c> becomes <c>"@class"</c>).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="input" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="input" /> is an empty string.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method handles all C# reserved keywords as defined in the C# language specification,
    ///         ensuring the returned string is always a valid identifier.
    ///     </para>
    ///     <para>
    ///         For example:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><c>"FirstName"</c> becomes <c>"firstName"</c></description></item>
    ///         <item><description><c>"Class"</c> becomes <c>"@class"</c></description></item>
    ///         <item><description><c>"Object"</c> becomes <c>"@object"</c></description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ToPropertyName" />
    public static string ToParameterName(this string input)
    {
        input = input ?? throw new ArgumentNullException(nameof(input));

#pragma warning disable CA1308
        return input.ToLowerInvariant() switch
#pragma warning restore CA1308
        {
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),

            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
            "abstract" => "@abstract",
            "as" => "@as",
            "base" => "@base",
            "bool" => "@bool",
            "break" => "@break",
            "byte" => "@byte",
            "case" => "@case",
            "catch" => "@catch",
            "char" => "@char",
            "checked" => "@checked",
            "class" => "@class",
            "const" => "@const",
            "continue" => "@continue",
            "decimal" => "@decimal",
            "default" => "@default",
            "delegate" => "@delegate",
            "do" => "@do",
            "double" => "@double",
            "else" => "@else",
            "enum" => "@enum",
            "event" => "@event",
            "explicit" => "@explicit",
            "extern" => "@extern",
            "false" => "@false",
            "finally" => "@finally",
            "fixed" => "@fixed",
            "float" => "@float",
            "for" => "@for",
            "foreach" => "@foreach",
            "goto" => "@goto",
            "if" => "@if",
            "implicit" => "@implicit",
            "in" => "@in",
            "int" => "@int",
            "interface" => "@interface",
            "internal" => "@internal",
            "is" => "@is",
            "lock" => "@lock",
            "long" => "@long",
            "namespace" => "@namespace",
            "new" => "@new",
            "null" => "@null",
            "object" => "@object",
            "operator" => "@operator",
            "out" => "@out",
            "override" => "@override",
            "params" => "@params",
            "private" => "@private",
            "protected" => "@protected",
            "public" => "@public",
            "readonly" => "@readonly",
            "ref" => "@ref",
            "return" => "@return",
            "sbyte" => "@sbyte",
            "sealed" => "@sealed",
            "short" => "@short",
            "sizeof" => "@sizeof",
            "stackalloc" => "@stackalloc",
            "static" => "@static",
            "string" => "@string",
            "struct" => "@struct",
            "switch" => "@switch",
            "this" => "@this",
            "throw" => "@throw",
            "true" => "@true",
            "try" => "@try",
            "typeof" => "@typeof",
            "uint" => "@uint",
            "ulong" => "@ulong",
            "unchecked" => "@unchecked",
            "unsafe" => "@unsafe",
            "ushort" => "@ushort",
            "using" => "@using",
            "virtual" => "@virtual",
            "void" => "@void",
            "volatile" => "@volatile",
            "while" => "@while",

#pragma warning disable CA1308 // Normalize strings to uppercase
#if NET6_0_OR_GREATER
            _ => string.Concat(input[0].ToString().ToLower(CultureInfo.InvariantCulture), input.AsSpan(1)),
#else
            _ => input[0].ToString().ToLower(CultureInfo.InvariantCulture) + input[1..],
#endif
#pragma warning restore CA1308 // Normalize strings to uppercase
        };
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
    ///         This method is useful for cleaning generated code where conditional sections
    ///         may leave lines containing only spaces or tabs.
    ///     </para>
    ///     <para>
    ///         A "blank line" is defined as a line containing one or more whitespace characters
    ///         but no other content. Empty lines (containing no characters) are not considered blank
    ///         and are preserved in the output.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var cleaned = """
    ///     public class Foo
    ///     {
    ///
    ///         public int Value { get; }
    ///     }
    ///     """.TrimBlankLines();
    /// // Result: lines with only spaces are removed, but true empty lines are kept
    /// </code>
    /// </example>
    /// <seealso cref="CleanWhiteSpace" />
    /// <seealso cref="NormalizeLineEndings" />
    public static string TrimBlankLines(this string text)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));

        var lines = text.NormalizeLineEndings().Split(NewLineSeparator, StringSplitOptions.None);
        var result = new StringBuilder();
        var first = true;

        foreach (var line in lines)
        {
            if (!IsBlankLine(line))
            {
                if (!first)
                    result.Append('\n');
                result.Append(line);
                first = false;
            }
        }

        return result.ToString();

        static bool IsBlankLine(string line)
        {
            if (line.Length is 0)
                return false;

            foreach (var c in line)
            {
                if (!char.IsWhiteSpace(c))
                    return false;
            }

            return true;
        }
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
    /// <remarks>
    ///     <para>
    ///         This method first converts all line endings to <c>\n</c>, then optionally
    ///         converts them to the specified <paramref name="newLine" /> sequence.
    ///     </para>
    ///     <para>
    ///         This is particularly useful for source generators that need to produce
    ///         consistent output across different platforms.
    ///     </para>
    /// </remarks>
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
    ///     <para>
    ///         This method is designed to produce clean, consistent output from source generators
    ///         that may have accumulated extra whitespace during conditional code generation.
    ///     </para>
    /// </remarks>
    /// <seealso cref="TrimBlankLines" />
    /// <seealso cref="NormalizeLineEndings" />
    public static string CleanWhiteSpace(this string source)
    {
        source = source ?? throw new ArgumentNullException(nameof(source));

        // Strip trailing whitespace from lines
        source = Regex.Replace(source, @"[ \t]+(\r?\n)", "$1");

        // Collapse 3+ empty lines to 2
        source = Regex.Replace(source, @"(\r?\n){3,}", "$1$1");

        // Remove empty line after { or ]
        source = Regex.Replace(source, @"([\{\]])(\r?\n){2}", "$1$2");

        // Remove empty line before }
        source = Regex.Replace(source, @"(\r?\n){2}([\}])", "$1$2");

        return source;
    }

    /// <summary>
    ///     A zero-allocation enumerator for splitting strings into lines.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This ref struct provides efficient line-by-line iteration over a string
    ///         without allocating intermediate string arrays. It correctly handles
    ///         <c>\n</c>, <c>\r</c>, and <c>\r\n</c> line endings.
    ///     </para>
    ///     <para>
    ///         Use with the <see cref="StringExtensions.SplitLines(string)" /> extension method:
    ///     </para>
    ///     <code>
    /// foreach (var entry in text.SplitLines())
    /// {
    ///     ReadOnlySpan&lt;char&gt; line = entry.Line;
    ///     ReadOnlySpan&lt;char&gt; separator = entry.Separator;
    /// }
    /// </code>
    /// </remarks>
    /// <seealso cref="SplitLines(string)" />
    /// <seealso cref="SplitLines(ReadOnlySpan{char})" />
    /// <seealso cref="LineSplitEntry" />
    [StructLayout(LayoutKind.Auto)]
#pragma warning disable CA1034 // Nested types should not be visible - ref structs require this pattern
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LineSplitEnumerator" /> struct.
        /// </summary>
        /// <param name="str">The span of characters to enumerate lines from.</param>
        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        /// <summary>
        ///     Returns this enumerator instance for use with foreach.
        /// </summary>
        /// <returns>This enumerator instance.</returns>
        /// <remarks>
        ///     This method enables the use of this enumerator directly in a foreach statement.
        /// </remarks>
        public readonly LineSplitEnumerator GetEnumerator() => this;

        /// <summary>
        ///     Advances the enumerator to the next line in the span.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the enumerator was successfully advanced to the next line;
        ///     <c>false</c> if the enumerator has passed the end of the span.
        /// </returns>
        public bool MoveNext()
        {
            if (_str.Length is 0)
                return false;

            var span = _str;
            var index = span.IndexOfAny('\r', '\n');
            if (index == -1)
            {
                _str = ReadOnlySpan<char>.Empty;
                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            if (index < span.Length - 1 && span[index] == '\r')
            {
                var next = span[index + 1];
                if (next == '\n')
                {
                    Current = new LineSplitEntry(span[..index], span.Slice(index, 2));
                    _str = span[(index + 2)..];
                    return true;
                }
            }

            Current = new LineSplitEntry(span[..index], span.Slice(index, 1));
            _str = span[(index + 1)..];
            return true;
        }

        /// <summary>
        ///     Gets the current line entry at the enumerator's position.
        /// </summary>
        /// <value>
        ///     A <see cref="LineSplitEntry" /> containing the current line content and its separator.
        /// </value>
        public LineSplitEntry Current { get; private set; }
    }

    /// <summary>
    ///     Represents a single line and its trailing line separator from a line split operation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This ref struct contains both the line content (excluding the line ending)
    ///         and the separator that terminated the line (<c>\n</c>, <c>\r</c>, or <c>\r\n</c>).
    ///     </para>
    ///     <para>
    ///         For the last line in a string that doesn't end with a line terminator,
    ///         <see cref="Separator" /> will be empty.
    ///     </para>
    ///     <para>
    ///         This struct can be implicitly converted to <see cref="ReadOnlySpan{T}" /> of <see cref="char" />,
    ///         returning only the line content.
    ///     </para>
    /// </remarks>
    /// <seealso cref="LineSplitEnumerator" />
    /// <seealso cref="SplitLines(string)" />
    [StructLayout(LayoutKind.Auto)]
    public readonly ref struct LineSplitEntry
#pragma warning restore CA1034
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LineSplitEntry" /> struct.
        /// </summary>
        /// <param name="line">The content of the line, excluding the line separator.</param>
        /// <param name="separator">The line separator that terminated this line, or empty if none.</param>
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        /// <summary>
        ///     Gets the content of the line, excluding the trailing line separator.
        /// </summary>
        /// <value>
        ///     A <see cref="ReadOnlySpan{T}" /> of <see cref="char" /> containing the line content.
        /// </value>
        public ReadOnlySpan<char> Line { get; }

        /// <summary>
        ///     Gets the line separator that terminated this line.
        /// </summary>
        /// <value>
        ///     A <see cref="ReadOnlySpan{T}" /> of <see cref="char" /> containing the separator
        ///     (<c>\n</c>, <c>\r</c>, or <c>\r\n</c>), or empty for the last line without a terminator.
        /// </value>
        public ReadOnlySpan<char> Separator { get; }

        /// <summary>
        ///     Deconstructs this entry into its line and separator components.
        /// </summary>
        /// <param name="line">When this method returns, contains the line content.</param>
        /// <param name="separator">When this method returns, contains the line separator.</param>
        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        /// <summary>
        ///     Implicitly converts a <see cref="LineSplitEntry" /> to its line content.
        /// </summary>
        /// <param name="entry">The entry to convert.</param>
        /// <returns>
        ///     A <see cref="ReadOnlySpan{T}" /> of <see cref="char" /> containing the line content.
        /// </returns>
        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}

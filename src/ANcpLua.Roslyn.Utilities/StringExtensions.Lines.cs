using System.Runtime.InteropServices;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
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
    /// <seealso cref="SplitLines(string)" />
    /// <seealso cref="LineSplitEnumerator" />
    /// <seealso cref="LineSplitEntry" />
    public static LineSplitEnumerator SplitLines(this string str)
    {
        return new LineSplitEnumerator(str.AsSpan());
    }

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
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str)
    {
        return new LineSplitEnumerator(str);
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
        public readonly LineSplitEnumerator GetEnumerator()
        {
            return this;
        }

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
        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry)
        {
            return entry.Line;
        }
    }
}

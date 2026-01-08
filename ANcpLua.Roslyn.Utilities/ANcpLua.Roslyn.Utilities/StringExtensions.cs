using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for string manipulation in source generators.
/// </summary>
public static class StringExtensions
{
    private static readonly char[] NewLineSeparator = new[] { '\n' };

    /// <summary>
    ///     Splits a string into lines without allocating (returns a ref struct enumerator).
    /// </summary>
    public static LineSplitEnumerator SplitLines(this string str) => new(str.AsSpan());

    /// <summary>
    ///     Splits a span into lines without allocating.
    /// </summary>
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str) => new(str);

    /// <summary>
    ///     Makes the first letter of the name uppercase.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
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
    ///     Makes the first letter of the name lowercase, handling C# keywords.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
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
    ///     Removes lines that contain only whitespace characters.
    ///     Useful for cleaning generated code where conditional sections may leave empty lines.
    /// </summary>
    /// <param name="text"></param>
    /// <returns>The text with whitespace-only lines removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text" /> is null.</exception>
    /// <example>
    ///     <code>
    /// var cleaned = """
    ///     public class Foo
    ///     {
    ///
    ///         public int Value { get; }
    ///     }
    ///     """.TrimBlankLines();
    /// // Result: lines with only spaces are removed
    /// </code>
    /// </example>
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
    ///     Normalizes line endings to '\n' or your endings.
    /// </summary>
    /// <param name="newLine">'\n' by default</param>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
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
    /// <remarks>
    ///     <para>Performs the following cleanup operations:</para>
    ///     <list type="bullet">
    ///         <item><description>Strips trailing whitespace from all lines</description></item>
    ///         <item><description>Collapses 3+ consecutive empty lines to 2</description></item>
    ///         <item><description>Removes empty lines immediately after <c>{</c> or <c>]</c></description></item>
    ///         <item><description>Removes empty lines immediately before <c>}</c></description></item>
    ///     </list>
    /// </remarks>
    /// <param name="source">The generated source code to clean.</param>
    /// <returns>The cleaned source code with normalized whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
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
    ///     Zero-allocation line enumerator.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
#pragma warning disable CA1034 // Nested types should not be visible - ref structs require this pattern
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        public readonly LineSplitEnumerator GetEnumerator() => this;

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

        public LineSplitEntry Current { get; private set; }
    }

    /// <summary>
    ///     Represents a line and its separator.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly ref struct LineSplitEntry
#pragma warning restore CA1034
    {
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        public ReadOnlySpan<char> Line { get; }
        public ReadOnlySpan<char> Separator { get; }

        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}

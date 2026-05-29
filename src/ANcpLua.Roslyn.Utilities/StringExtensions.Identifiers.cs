namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    // C# reserved keywords. Sourced from the official C# language reference:
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
    // ToParameterName checks membership via O(1) hash lookup, replacing what
    // used to be an 80-arm switch expression (CC ~82) with CC ~4.
    private static readonly HashSet<string> s_cSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    ];

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
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (input.Length == 0)
            throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));

#if NET6_0_OR_GREATER
        return string.Concat(input[0].ToString().ToUpper(CultureInfo.InvariantCulture), input.AsSpan(1));
#else
        return input[0].ToString().ToUpper(CultureInfo.InvariantCulture) + input[1..];
#endif
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
    ///         Handles every C# reserved keyword via a single hash-set lookup. The keyword table is
    ///         the source of truth — adding a new keyword is one string instead of a new switch arm.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ToPropertyName" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Parameter names and C# keyword escaping intentionally produce lowercase or camelCase names.")]
    public static string ToParameterName(this string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (input.Length == 0)
            throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));

        var lowered = input.ToLowerInvariant();
        if (s_cSharpKeywords.Contains(lowered))
            return "@" + lowered;

#if NET6_0_OR_GREATER
        return string.Concat(input[0].ToString().ToLower(CultureInfo.InvariantCulture), input.AsSpan(1));
#else
        return input[0].ToString().ToLower(CultureInfo.InvariantCulture) + input[1..];
#endif
    }

    /// <summary>
    ///     Sanitizes a string for use as a C# identifier by replacing non-alphanumeric characters with underscores.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>A string containing only letters, digits, and underscores.</returns>
    public static string SanitizeIdentifier(this string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";

        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(IsIdentifierChar(c) ? c : '_');

        return sb.ToString();
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    ///     Escapes a string for placement inside a C# double-quoted string literal.
    /// </summary>
    /// <param name="s">The string to escape.</param>
    /// <returns>
    ///     The escaped body (without the surrounding quotes). Handles the full set of C# simple-escape
    ///     sequences (<c>\" \\ \0 \a \b \f \n \r \t \v</c>) and emits <c>\uXXXX</c> for any remaining control
    ///     character. Printable non-ASCII (e.g. em-dash, accented letters) is left as-is, matching the C#
    ///     compiler's acceptance of raw Unicode in UTF-8 string literals.
    /// </returns>
    public static string EscapeCSharpString(this string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var builder = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\a':
                    builder.Append("\\a");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\v':
                    builder.Append("\\v");
                    break;
                default:
                    if (char.IsControl(ch))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Converts a name to an underscore-prefixed camelCase private-field identifier, treating every run of
    ///     non-identifier characters as a word separator.
    /// </summary>
    /// <param name="input">The name to convert (e.g. a metric, column, or tag name).</param>
    /// <param name="fallback">Returned when <paramref name="input" /> yields no identifier characters.</param>
    /// <returns>
    ///     An identifier such as <c>_httpServerDuration</c> for <c>"http.server.duration"</c>; the first
    ///     character's case is preserved and each character following a separator is upper-cased. Returns
    ///     <paramref name="fallback" /> when <paramref name="input" /> contains nothing usable.
    /// </returns>
    public static string ToFieldName(this string input, string fallback = "_field")
    {
        var result = new StringBuilder("_");
        var upperNext = false;

        foreach (var ch in input)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
            {
                upperNext = true;
                continue;
            }

            result.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
            upperNext = false;
        }

        return result.Length > 1 ? result.ToString() : fallback;
    }
}

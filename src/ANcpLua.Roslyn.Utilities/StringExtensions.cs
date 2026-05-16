namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for string manipulation in source generators.
/// </summary>
/// <remarks>
///     <para>
///         Utility methods commonly needed when generating source code. The implementation is split
///         across partial files by responsibility:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This file: PascalCase / kebab / snake casing entry points</description>
///         </item>
///         <item>
///             <description><c>StringExtensions.Lines.cs</c> — zero-allocation line enumeration
///             (<see cref="SplitLines(string)" />, <see cref="LineSplitEnumerator" />)</description>
///         </item>
///         <item>
///             <description><c>StringExtensions.Identifiers.cs</c> — <see cref="ToPropertyName" />,
///             <see cref="ToParameterName" /> (hashset-based keyword escape),
///             <see cref="SanitizeIdentifier" />, <see cref="EscapeCSharpString" /></description>
///         </item>
///         <item>
///             <description><c>StringExtensions.Whitespace.cs</c> — <see cref="TrimBlankLines" />,
///             <see cref="NormalizeLineEndings" />, <see cref="CleanWhiteSpace" />,
///             <see cref="NormalizeWhitespace" /></description>
///         </item>
///         <item>
///             <description><c>StringExtensions.TypeNames.cs</c> — fully-qualified name manipulation,
///             C# keyword aliasing, primitive-JSON detection, prefix/suffix strippers</description>
///         </item>
///         <item>
///             <description><c>StringExtensions.Hashing.cs</c> — <see cref="ToShortHash(string)" /></description>
///         </item>
///         <item>
///             <description><c>StringExtensions.Quoting.cs</c> — quoting / unquoting + graph label
///             escaping (DOT / Mermaid)</description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    /// <summary>
    ///     Converts a PascalCase or camelCase string to kebab-case.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>
    ///     The input string converted to kebab-case using invariant culture rules.
    ///     Returns the input unchanged if it is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Consecutive uppercase letters are treated as a single acronym.
    ///         A dash is inserted at the boundary between an acronym and the next word.
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><c>"GetValue"</c> becomes <c>"get-value"</c></description></item>
    ///         <item><description><c>"XMLParser"</c> becomes <c>"xml-parser"</c></description></item>
    ///         <item><description><c>"GetHTTPClient"</c> becomes <c>"get-http-client"</c></description></item>
    ///         <item><description><c>"SimpleTest"</c> becomes <c>"simple-test"</c></description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ToSnakeCase" />
    /// <seealso cref="ToParameterName" />
    /// <seealso cref="ToPropertyName" />
    public static string ToKebabCase(this string input)
    {
        return ToSeparatedCase(input, '-');
    }

    /// <summary>
    ///     Converts a PascalCase or camelCase string to snake_case.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>
    ///     The input string converted to snake_case using invariant culture rules.
    ///     Returns the input unchanged if it is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Consecutive uppercase letters are treated as a single acronym.
    ///         An underscore is inserted at the boundary between an acronym and the next word.
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><c>"GetValue"</c> becomes <c>"get_value"</c></description></item>
    ///         <item><description><c>"XMLParser"</c> becomes <c>"xml_parser"</c></description></item>
    ///         <item><description><c>"GetHTTPClient"</c> becomes <c>"get_http_client"</c></description></item>
    ///         <item><description><c>"SimpleTest"</c> becomes <c>"simple_test"</c></description></item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="ToKebabCase" />
    /// <seealso cref="ToParameterName" />
    /// <seealso cref="ToPropertyName" />
    public static string ToSnakeCase(this string input)
    {
        return ToSeparatedCase(input, '_');
    }

    private static string ToSeparatedCase(string input, char separator)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length + 4);
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0 && NeedsSeparator(input, i))
                sb.Append(separator);

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    // Separator goes before a standard word boundary (aB) or an acronym boundary (ABc).
    // Pulled into its own predicate so ToSeparatedCase reads as one loop with a single
    // branch instead of three nested condition checks.
    private static bool NeedsSeparator(string input, int i)
    {
        var prevIsLower = char.IsLower(input[i - 1]);
        var nextIsLower = i + 1 < input.Length && char.IsLower(input[i + 1]);
        return prevIsLower || nextIsLower;
    }
}

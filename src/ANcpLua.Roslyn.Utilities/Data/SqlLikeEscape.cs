using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Single-pass escape for SQL <c>LIKE</c> pattern meta-characters.
/// </summary>
/// <remarks>
///     <para>
///         Escapes <c>%</c>, <c>_</c>, and the configured escape character itself.
///         Returns the input unchanged (no allocation) when no escaping is required.
///     </para>
///     <para>
///         Use the returned pattern with an explicit SQL <c>ESCAPE</c> clause:
///         <code>WHERE col LIKE @p ESCAPE '\'</code>
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SqlLikeEscape
{
    /// <summary>Escapes <c>%</c>, <c>_</c>, and the escape character in a LIKE pattern fragment.</summary>
    /// <param name="input">The raw pattern fragment.</param>
    /// <param name="escapeChar">The escape character. Defaults to backslash.</param>
    /// <returns>Escaped pattern. Allocates only when escaping is required.</returns>
    public static string Escape(ReadOnlySpan<char> input, char escapeChar = '\\')
    {
        if (input.IsEmpty) return string.Empty;

        var needsEscape = false;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '%' || c == '_' || c == escapeChar)
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape) return input.ToString();

        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        try
        {
            foreach (var c in input)
            {
                if (c == '%' || c == '_' || c == escapeChar)
                    sb.Append(escapeChar);
                sb.Append(c);
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>String overload of <see cref="Escape(ReadOnlySpan{char},char)" />.</summary>
    /// <param name="input">The raw pattern fragment, or <c>null</c>.</param>
    /// <param name="escapeChar">The escape character. Defaults to backslash.</param>
    /// <returns>Escaped pattern, or <see cref="string.Empty" /> when <paramref name="input" /> is <c>null</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Escape(string? input, char escapeChar = '\\') =>
        input is null ? string.Empty : Escape(input.AsSpan(), escapeChar);
}

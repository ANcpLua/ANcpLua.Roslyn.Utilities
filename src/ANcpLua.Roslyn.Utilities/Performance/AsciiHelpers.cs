using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Branchless ASCII operations used in HTTP/JSON parsers.
///     The <c>|= 0x20</c> trick folds uppercase ASCII to lowercase in a single OR — no branch, no table lookup.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AsciiHelpers
{
    /// <summary>
    ///     Case-insensitive equality for ASCII byte spans using the <c>|= 0x20</c> trick.
    ///     Same pattern used in Kestrel's HTTP header parser.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            var a = left[i];
            var b = right[i];

            if (a == b)
                continue;

            if ((a | 0x20) != (b | 0x20))
                return false;

            // Ensure both are actually ASCII letters (a-z after folding)
            var folded = (uint)(a | 0x20) - 'a';
            if (folded > 'z' - 'a')
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Trims leading and trailing ASCII whitespace (space + tab) from a byte span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> TrimWhiteSpace(ReadOnlySpan<byte> span)
    {
        var start = 0;
        var end = span.Length - 1;

        while (start <= end && (span[start] == ' ' || span[start] == '\t'))
            start++;

        while (end >= start && (span[end] == ' ' || span[end] == '\t'))
            end--;

        return span.Slice(start, end - start + 1);
    }

    /// <summary>
    ///     Returns <see langword="true" /> if every byte in the span is ASCII (&lt; 128).
    ///     Enables the UTF-8 fast path: if all bytes are ASCII, no multi-byte decoding is needed.
    /// </summary>
    public static bool IsAscii(ReadOnlySpan<byte> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] > 127)
                return false;
        }

        return true;
    }
}

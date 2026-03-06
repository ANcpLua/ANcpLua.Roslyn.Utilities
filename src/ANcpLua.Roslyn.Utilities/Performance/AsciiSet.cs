using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     128-bit bitmap for O(1) ASCII character membership testing.
///     Same pattern used internally by System.Text.Json, Regex, and Kestrel
///     to replace branchy <c>if (c == 'a' || c == 'b' || ...)</c> chains
///     with a single bit-test per character.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct AsciiSet
{
    private readonly ulong _lo; // chars 0–63
    private readonly ulong _hi; // chars 64–127

    /// <summary>
    ///     Creates an <see cref="AsciiSet" /> from the specified characters. Non-ASCII characters are ignored.
    /// </summary>
    public AsciiSet(ReadOnlySpan<char> chars)
    {
        ulong lo = 0, hi = 0;
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c < 64)
                lo |= 1UL << c;
            else if (c < 128)
                hi |= 1UL << (c - 64);
        }

        _lo = lo;
        _hi = hi;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if <paramref name="c" /> is in this set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(char c)
    {
        if (c < 64) return (_lo >> c & 1) != 0;
        if (c < 128) return (_hi >> (c - 64) & 1) != 0;
        return false;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if <paramref name="b" /> is in this set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(byte b)
    {
        if (b < 64) return (_lo >> b & 1) != 0;
        return (_hi >> (b - 64) & 1) != 0;
    }

    /// <summary>
    ///     Returns the index of the first character in <paramref name="span" /> that belongs to this set,
    ///     or -1 if none found.
    /// </summary>
    public int IndexOfAny(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (Contains(span[i]))
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Returns the index of the first byte in <paramref name="span" /> that belongs to this set,
    ///     or -1 if none found.
    /// </summary>
    public int IndexOfAny(ReadOnlySpan<byte> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (Contains(span[i]))
                return i;
        }

        return -1;
    }
}

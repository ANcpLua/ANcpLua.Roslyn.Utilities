using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Span utilities missing from netstandard2.0: counting, containment, whitespace checks.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SpanHelpers
{
    /// <summary>
    ///     Counts occurrences of <paramref name="value" /> in the span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count(ReadOnlySpan<char> span, char value)
    {
        var count = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == value)
                count++;
        }

        return count;
    }

    /// <summary>
    ///     Counts occurrences of <paramref name="value" /> in the span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count(ReadOnlySpan<byte> span, byte value)
    {
        var count = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == value)
                count++;
        }

        return count;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the span contains any of the specified values.
    /// </summary>
    public static bool ContainsAny(ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (values.IndexOf(span[i]) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Returns the index of the first occurrence of any value in <paramref name="values" />,
    ///     or -1 if none found.
    /// </summary>
    public static int IndexOfAny(ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (values.IndexOf(span[i]) >= 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the span consists entirely of whitespace characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (!char.IsWhiteSpace(span[i]))
                return false;
        }

        return true;
    }
}

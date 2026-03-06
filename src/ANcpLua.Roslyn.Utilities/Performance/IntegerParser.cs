using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Zero-allocation integer parsing from <see cref="ReadOnlySpan{T}" /> with overflow detection.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class IntegerParser
{
    /// <summary>
    ///     Parses a decimal <see cref="int" /> from a character span. Handles optional leading sign.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseInt32(ReadOnlySpan<char> span, out int result)
    {
        result = 0;
        if (span.IsEmpty)
            return false;

        var i = 0;
        var negative = false;

        switch (span[0])
        {
            case '-':
                negative = true;
                i = 1;
                break;
            case '+':
                i = 1;
                break;
        }

        if (i >= span.Length)
            return false;

        ulong accumulator = 0;
        for (; i < span.Length; i++)
        {
            var digit = (uint)(span[i] - '0');
            if (digit > 9)
                return false;

            accumulator = accumulator * 10 + digit;
            if (accumulator > int.MaxValue + (negative ? 1UL : 0UL))
                return false;
        }

        result = negative ? -(int)accumulator : (int)accumulator;
        return true;
    }

    /// <summary>
    ///     Parses a decimal <see cref="long" /> from a character span. Handles optional leading sign.
    /// </summary>
    public static bool TryParseInt64(ReadOnlySpan<char> span, out long result)
    {
        result = 0;
        if (span.IsEmpty)
            return false;

        var i = 0;
        var negative = false;

        switch (span[0])
        {
            case '-':
                negative = true;
                i = 1;
                break;
            case '+':
                i = 1;
                break;
        }

        if (i >= span.Length)
            return false;

        ulong accumulator = 0;
        const ulong maxBeforeMultiply = ulong.MaxValue / 10;

        for (; i < span.Length; i++)
        {
            var digit = (uint)(span[i] - '0');
            if (digit > 9)
                return false;

            if (accumulator > maxBeforeMultiply)
                return false;

            accumulator = accumulator * 10 + digit;

            var limit = negative ? (ulong)long.MaxValue + 1 : (ulong)long.MaxValue;
            if (accumulator > limit)
                return false;
        }

        result = negative ? -(long)accumulator : (long)accumulator;
        return true;
    }

    /// <summary>
    ///     Parses a decimal <see cref="int" /> from a UTF-8 byte span. Handles optional leading sign.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseInt32(ReadOnlySpan<byte> span, out int result)
    {
        result = 0;
        if (span.IsEmpty)
            return false;

        var i = 0;
        var negative = false;

        switch (span[0])
        {
            case (byte)'-':
                negative = true;
                i = 1;
                break;
            case (byte)'+':
                i = 1;
                break;
        }

        if (i >= span.Length)
            return false;

        ulong accumulator = 0;
        for (; i < span.Length; i++)
        {
            var digit = (uint)(span[i] - '0');
            if (digit > 9)
                return false;

            accumulator = accumulator * 10 + digit;
            if (accumulator > int.MaxValue + (negative ? 1UL : 0UL))
                return false;
        }

        result = negative ? -(int)accumulator : (int)accumulator;
        return true;
    }

    /// <summary>
    ///     Parses a decimal <see cref="long" /> from a UTF-8 byte span. Handles optional leading sign.
    /// </summary>
    public static bool TryParseInt64(ReadOnlySpan<byte> span, out long result)
    {
        result = 0;
        if (span.IsEmpty)
            return false;

        var i = 0;
        var negative = false;

        switch (span[0])
        {
            case (byte)'-':
                negative = true;
                i = 1;
                break;
            case (byte)'+':
                i = 1;
                break;
        }

        if (i >= span.Length)
            return false;

        ulong accumulator = 0;
        const ulong maxBeforeMultiply = ulong.MaxValue / 10;

        for (; i < span.Length; i++)
        {
            var digit = (uint)(span[i] - '0');
            if (digit > 9)
                return false;

            if (accumulator > maxBeforeMultiply)
                return false;

            accumulator = accumulator * 10 + digit;

            var limit = negative ? (ulong)long.MaxValue + 1 : (ulong)long.MaxValue;
            if (accumulator > limit)
                return false;
        }

        result = negative ? -(long)accumulator : (long)accumulator;
        return true;
    }
}

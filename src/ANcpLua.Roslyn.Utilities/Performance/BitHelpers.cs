using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Performance;

/// <summary>
///     Software polyfills for <c>System.Numerics.BitOperations</c> (unavailable on netstandard2.0).
///     Uses the same algorithms as the .NET runtime: Hamming weight, De Bruijn sequences,
///     and bit-twiddling tricks that the JIT can optimize into hardware intrinsics on modern runtimes.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class BitHelpers
{
    /// <summary>
    ///     Returns the population count (number of set bits) in a 32-bit unsigned integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(uint value)
    {
        // Hamming weight — same algorithm as .NET runtime fallback
        value -= (value >> 1) & 0x5555_5555u;
        value = (value & 0x3333_3333u) + ((value >> 2) & 0x3333_3333u);
        value = (value + (value >> 4)) & 0x0F0F_0F0Fu;
        return (int)((value * 0x0101_0101u) >> 24);
    }

    /// <summary>
    ///     Returns the population count (number of set bits) in a 64-bit unsigned integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong value)
    {
        return PopCount((uint)value) + PopCount((uint)(value >> 32));
    }

    /// <summary>
    ///     Returns the number of leading zero bits in a 32-bit unsigned integer.
    ///     Returns 32 when <paramref name="value" /> is zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeroCount(uint value)
    {
        if (value == 0)
            return 32;

        var n = 0;
        if (value <= 0x0000_FFFFu) { n += 16; value <<= 16; }
        if (value <= 0x00FF_FFFFu) { n += 8; value <<= 8; }
        if (value <= 0x0FFF_FFFFu) { n += 4; value <<= 4; }
        if (value <= 0x3FFF_FFFFu) { n += 2; value <<= 2; }
        if (value <= 0x7FFF_FFFFu) { n += 1; }
        return n;
    }

    /// <summary>
    ///     Returns the number of trailing zero bits in a 32-bit unsigned integer.
    ///     Returns 32 when <paramref name="value" /> is zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(uint value)
    {
        // Isolate lowest set bit, then count bits below it
        return value == 0 ? 32 : PopCount((value & (uint)-(int)value) - 1);
    }

    /// <summary>
    ///     Returns the number of trailing zero bits in a 64-bit unsigned integer.
    ///     Returns 64 when <paramref name="value" /> is zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(ulong value)
    {
        if (value == 0)
            return 64;

        var lo = (uint)value;
        return lo != 0
            ? TrailingZeroCount(lo)
            : 32 + TrailingZeroCount((uint)(value >> 32));
    }

    /// <summary>
    ///     Returns the integer (floor) base-2 logarithm of <paramref name="value" />.
    ///     Returns 0 when <paramref name="value" /> is zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Log2(uint value)
    {
        return value == 0 ? 0 : 31 - LeadingZeroCount(value);
    }

    /// <summary>
    ///     Determines whether <paramref name="value" /> is a power of two.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(uint value)
    {
        return value != 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    ///     Determines whether <paramref name="value" /> is a power of two.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(ulong value)
    {
        return value != 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    ///     Rounds <paramref name="value" /> up to the next power of two.
    ///     Returns 0 when <paramref name="value" /> is zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RoundUpToPowerOf2(uint value)
    {
        if (value == 0)
            return 0;

        --value;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    ///     Rotates <paramref name="value" /> left by <paramref name="offset" /> bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateLeft(uint value, int offset)
    {
        return (value << offset) | (value >> (32 - offset));
    }

    /// <summary>
    ///     Rotates <paramref name="value" /> right by <paramref name="offset" /> bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateRight(uint value, int offset)
    {
        return (value >> offset) | (value << (32 - offset));
    }

    /// <summary>
    ///     Rotates <paramref name="value" /> left by <paramref name="offset" /> bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateLeft(ulong value, int offset)
    {
        return (value << offset) | (value >> (64 - offset));
    }

    /// <summary>
    ///     Rotates <paramref name="value" /> right by <paramref name="offset" /> bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateRight(ulong value, int offset)
    {
        return (value >> offset) | (value << (64 - offset));
    }
}

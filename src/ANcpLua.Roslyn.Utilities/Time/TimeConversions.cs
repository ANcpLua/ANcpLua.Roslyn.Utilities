using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Time;

/// <summary>
///     Nanosecond, millisecond, and <see cref="DateTimeOffset" /> conversions.
///     All OTLP timestamps are stored as Unix nanoseconds (uint64).
/// </summary>
/// <remarks>
///     <para>Valid until year ~2554 (ulong max / 1e9 seconds).</para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TimeConversions
{
    private const long NanosPerMillisecond = 1_000_000L;

    /// <summary>
    ///     Unix epoch (1970-01-01T00:00:00Z) for environments where
    ///     <c>DateTime.UnixEpoch</c> is not available.
    /// </summary>
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── DateTimeOffset -> UnixNano ────────────────────────────────────────────

    /// <summary>Converts a <see cref="DateTimeOffset" /> to Unix nanoseconds (signed).</summary>
    /// <param name="dto">The date/time value to convert.</param>
    /// <returns>Unix nanoseconds as a signed 64-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixNano(DateTimeOffset dto) =>
        dto.ToUnixTimeMilliseconds() * NanosPerMillisecond;

    /// <summary>Converts a <see cref="DateTimeOffset" /> to Unix nanoseconds (unsigned).</summary>
    /// <param name="dto">The date/time value to convert.</param>
    /// <returns>Unix nanoseconds as an unsigned 64-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ToUnixNanoUnsigned(DateTimeOffset dto) =>
        (ulong)(dto.ToUnixTimeMilliseconds() * NanosPerMillisecond);

    // ── Nanoseconds -> Milliseconds ──────────────────────────────────────────

    /// <summary>Converts signed nanoseconds to milliseconds.</summary>
    /// <param name="nanos">The nanosecond value to convert.</param>
    /// <returns>The equivalent value in milliseconds.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NanosToMs(long nanos) => nanos / (double)NanosPerMillisecond;

    /// <summary>Converts unsigned nanoseconds to milliseconds.</summary>
    /// <param name="nanos">The nanosecond value to convert.</param>
    /// <returns>The equivalent value in milliseconds.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NanosToMs(ulong nanos) => nanos / (double)NanosPerMillisecond;

    // ── UnixNano -> DateTimeOffset / DateTime ────────────────────────────────

    /// <summary>Converts signed Unix nanoseconds to a <see cref="DateTimeOffset" />.</summary>
    /// <param name="nanos">Unix nanoseconds (signed).</param>
    /// <returns>The corresponding <see cref="DateTimeOffset" /> in UTC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset NanosToDateTimeOffset(long nanos) =>
        DateTimeOffset.FromUnixTimeMilliseconds(nanos / NanosPerMillisecond);

    /// <summary>Converts unsigned Unix nanoseconds to a <see cref="DateTimeOffset" />.</summary>
    /// <param name="nanos">Unix nanoseconds (unsigned).</param>
    /// <returns>The corresponding <see cref="DateTimeOffset" /> in UTC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset NanosToDateTimeOffset(ulong nanos) =>
        DateTimeOffset.FromUnixTimeMilliseconds((long)(nanos / NanosPerMillisecond));

    /// <summary>
    ///     Converts Unix nanoseconds (unsigned) to <see cref="DateTime" /> (UTC) via ticks (100ns precision).
    /// </summary>
    /// <remarks>
    ///     Uses tick-based conversion (nanos / 100) for higher precision than millisecond rounding.
    ///     Any <see langword="ulong" /> nanosecond value is safe: ulong.MaxValue / 100 ~= 184 quintillion ticks (year ~2554).
    /// </remarks>
    /// <param name="unixNano">Unix nanoseconds (unsigned).</param>
    /// <returns>A UTC <see cref="DateTime" /> representing the given timestamp.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime UnixNanoToDateTime(ulong unixNano)
    {
        var ticks = (long)(unixNano / 100);
        return UnixEpoch.AddTicks(ticks);
    }
}

using System.Security.Cryptography;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Short-id factories: <see cref="NewHex32" /> (32-char record id), <see cref="NewHex" /> (truncated correlation id),
///     <see cref="NewPrefixedSortable" /> (<c>{prefix}-{hex}</c> with an 80-bit entropy floor — prevents the
///     <c>$"ws-{Guid.CreateVersion7():N}"[..24]</c> bug that kept only ~36 random bits), and
///     <see cref="NewUrlSafeRandom" /> (cryptographically-strong URL slug). Hex variants use <c>Guid.NewGuid()</c>
///     and are NOT for secrets.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ShortId
{
    /// <summary>Minimum hex-char count that satisfies the 80-bit entropy floor for prefixed ids.</summary>
    private const int MinPrefixedRandomHexChars = 20;

    /// <summary>Returns a 32-character lowercase hexadecimal id (equivalent to <c>Guid.NewGuid().ToString("N")</c>).</summary>
    /// <returns>A 32-char hex string.</returns>
    public static string NewHex32() => Guid.NewGuid().ToString("N");

    /// <summary>
    ///     Returns an <paramref name="hexChars" />-character lowercase hexadecimal id truncated from
    ///     <c>Guid.NewGuid().ToString("N")</c>.
    /// </summary>
    /// <param name="hexChars">Number of hex characters to return (1–32).</param>
    /// <returns>A truncated hex string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="hexChars" /> is not in the range 1–32.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Intended for correlation ids, log enricher instance ids, and other non-security,
    ///         non-primary-key uses. For security-sensitive random tokens, use
    ///         <see cref="NewUrlSafeRandom" />.
    ///     </para>
    /// </remarks>
    public static string NewHex(int hexChars)
    {
        if (hexChars is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(hexChars), hexChars, "Must be 1–32.");

        return Guid.NewGuid().ToString("N")[..hexChars];
    }

    /// <summary>
    ///     Returns a prefixed sortable identifier in the shape <c>{prefix}-{hex}</c> with a guaranteed
    ///     entropy floor of 80 bits (20 hex characters).
    /// </summary>
    /// <param name="prefix">The type-discriminator prefix (e.g. <c>"ws"</c>, <c>"job"</c>, <c>"run"</c>).</param>
    /// <param name="randomHexChars">
    ///     Number of random hex characters after the prefix. Must be at least 20 (80 bits of entropy).
    ///     Defaults to 24 (96 bits).
    /// </param>
    /// <returns>An id like <c>"ws-{hex}"</c> with a configurable random tail.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="randomHexChars" /> is below the 20-char entropy floor or above 32.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The previous hand-rolled pattern <c>$"{prefix}-{Guid.CreateVersion7():N}"[..24]</c>
    ///         truncated AFTER the prefix concatenation, leaving <c>24 - (prefix.Length + 1)</c>
    ///         random chars — for <c>ws-</c> that is 21 chars but of those ~12 are UUIDv7 timestamp,
    ///         leaving only ~36 random bits (collision-prone at scale). This helper forbids that shape
    ///         by requiring an explicit post-prefix entropy count.
    ///     </para>
    ///     <para>
    ///         Uses <see cref="RandomNumberGenerator" /> (not <see cref="Guid.NewGuid" />) so the random
    ///         tail is cryptographically strong. Suitable for tokens that appear in URLs or tenant ids.
    ///     </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "This helper's public contract returns lowercase hexadecimal identifiers.")]
    public static string NewPrefixedSortable(string prefix, int randomHexChars = 24)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix must not be empty or whitespace.", nameof(prefix));
        if (randomHexChars < MinPrefixedRandomHexChars || randomHexChars > 32)
            throw new ArgumentOutOfRangeException(
                nameof(randomHexChars),
                randomHexChars,
                $"Must be {MinPrefixedRandomHexChars}–32 (80-bit entropy floor).");

        var needBytes = (randomHexChars + 1) / 2;
        var buffer = new byte[needBytes];
#if NET5_0_OR_GREATER
        RandomNumberGenerator.Fill(buffer);
#else
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
#endif

#if NET5_0_OR_GREATER
        var hex = Convert.ToHexString(buffer).ToLowerInvariant();
#else
        var hex = BitConverter.ToString(buffer).Replace("-", "").ToLowerInvariant();
#endif

        return prefix + "-" + hex[..randomHexChars];
    }

    /// <summary>
    ///     Returns a URL-safe Base64 id backed by <paramref name="byteLength" /> bytes of
    ///     cryptographically strong randomness. Thin delegate over <see cref="Base64Url.NewRandom" />.
    /// </summary>
    /// <param name="byteLength">Number of random bytes. Must be greater than zero.</param>
    /// <returns>A URL-safe, unpadded Base64 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteLength" /> is non-positive.</exception>
    /// <seealso cref="Base64Url.NewRandom" />
    public static string NewUrlSafeRandom(int byteLength) => Base64Url.NewRandom(byteLength);
}

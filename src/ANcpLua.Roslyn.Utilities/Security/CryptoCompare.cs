using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Constant-time equality helpers for security-sensitive string and byte comparisons
///     (PKCE challenges, API keys, bearer tokens, HMAC signatures).
/// </summary>
/// <remarks>
///     <para>
///         Direct <see cref="string.Equals(string, string)" /> short-circuits on the first mismatching
///         character and therefore leaks information about the secret via branch timing. The helpers
///         here run in time independent of the compared contents (see CWE-208, "Observable Timing
///         Discrepancy").
///     </para>
///     <para>
///         <b>Length-oracle mitigation.</b> <see cref="FixedTimeEqualsUtf8" /> pre-hashes both inputs
///         with SHA-256 so the subsequent constant-time compare always runs over fixed 32-byte digests.
///         This eliminates the length-oracle that exists when callers feed raw UTF-8 bytes of unequal
///         length directly into <see cref="FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
///         (which returns <see langword="false" /> immediately on length mismatch).
///     </para>
///     <para>
///         <b>Null handling.</b> <see cref="FixedTimeEqualsUtf8" /> short-circuits when either input is
///         <see langword="null" />. Callers MUST NOT rely on null-path timing to keep a secret;
///         nullability of the incoming value is assumed to already be observable through the
///         surrounding control flow (e.g. a missing header is an independently observable event).
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class CryptoCompare
{
    /// <summary>
    ///     Compares two strings for equality in time that does not depend on the string contents,
    ///     and — by SHA-256-normalizing both inputs to 32 bytes — also does not leak the input length.
    /// </summary>
    /// <param name="a">First string, or <see langword="null" />.</param>
    /// <param name="b">Second string, or <see langword="null" />.</param>
    /// <returns>
    ///     <see langword="true" /> when both strings are <see langword="null" />, or when both are
    ///     non-null and their UTF-8 SHA-256 digests are byte-equal; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Intended for comparing user-supplied secrets against expected values: PKCE
    ///         <c>code_verifier</c> / <c>code_challenge</c>, OTLP API keys, bearer tokens, webhook
    ///         signatures encoded as strings.
    ///     </para>
    ///     <para>
    ///         Unlike <c>CryptographicOperations.FixedTimeEquals</c>, this method does NOT early-return
    ///         <see langword="false" /> on length mismatch: after SHA-256 normalization both operands
    ///         are always 32 bytes, so the length check is trivially equal and the whole comparison
    ///         runs over the full digest.
    ///     </para>
    /// </remarks>
    public static bool FixedTimeEqualsUtf8(string? a, string? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

#if NET5_0_OR_GREATER
        Span<byte> hashA = stackalloc byte[32];
        Span<byte> hashB = stackalloc byte[32];
        HashUtf8(a, hashA);
        HashUtf8(b, hashB);
        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
#else
        using var sha = SHA256.Create();
        var hashA = sha.ComputeHash(Encoding.UTF8.GetBytes(a));
        var hashB = sha.ComputeHash(Encoding.UTF8.GetBytes(b));
        return FixedTimeEquals(hashA, hashB);
#endif
    }

    /// <summary>
    ///     Compares two byte spans for equality in time that does not depend on their contents.
    /// </summary>
    /// <param name="a">First buffer.</param>
    /// <param name="b">Second buffer.</param>
    /// <returns>
    ///     <see langword="true" /> when the buffers have equal length and equal contents; otherwise
    ///     <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Follows standard <c>CryptographicOperations.FixedTimeEquals</c> semantics: returns
    ///         <see langword="false" /> immediately on length mismatch. Callers that need to hide the
    ///         length of a secret must equalize the inputs themselves — for raw strings, prefer
    ///         <see cref="FixedTimeEqualsUtf8" />, which SHA-256-normalizes to 32 bytes.
    ///     </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
#if NET5_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(a, b);
#else
        if (a.Length != b.Length) return false;

        var accumulator = 0;
        unchecked
        {
            for (var i = 0; i < a.Length; i++)
                accumulator |= a[i] ^ b[i];
        }

        return accumulator == 0;
#endif
    }

#if NET5_0_OR_GREATER
    private static void HashUtf8(string value, Span<byte> destination)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount <= 512)
        {
            Span<byte> utf8 = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(value, utf8);
            SHA256.HashData(utf8, destination);
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(value, rented);
            SHA256.HashData(rented.AsSpan(0, written), destination);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
#endif
}

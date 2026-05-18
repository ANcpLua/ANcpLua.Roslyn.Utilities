using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     URL-safe, unpadded Base64 (RFC 4648 §5) plus PKCE S256 challenge helpers.
/// </summary>
/// <remarks>
///     <para>
///         Encodes and decodes binary payloads using the URL/filename-safe alphabet
///         (<c>-</c> and <c>_</c> in place of <c>+</c> and <c>/</c>) and strips
///         <c>=</c> padding, producing strings safe for query strings, cookies,
///         cursors, and token identifiers without further escaping.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Encode" /> — raw bytes to URL-safe Base64 without padding.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="TryDecode" /> — tolerant decode that accepts padded or
///                 unpadded input and returns <c>false</c> for malformed data instead
///                 of throwing (catches both <see cref="FormatException" /> and
///                 <see cref="ArgumentException" />).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="NewRandom" /> — cryptographically strong random token.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Sha256Challenge" /> — RFC 7636 PKCE S256 code challenge
///                 from a verifier.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Base64Url
{
#if NET5_0_OR_GREATER
    private const int StackallocByteThreshold = 192;
#endif

    /// <summary>
    ///     Encodes raw bytes as URL-safe Base64 without <c>=</c> padding.
    /// </summary>
    /// <param name="bytes">The bytes to encode. An empty span produces an empty string.</param>
    /// <returns>The URL-safe Base64 representation of <paramref name="bytes" />.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;

#if NET5_0_OR_GREATER
        var maxChars = ((bytes.Length + 2) / 3) * 4;

        if (bytes.Length <= StackallocByteThreshold)
        {
            Span<char> buffer = stackalloc char[maxChars];
            if (!Convert.TryToBase64Chars(bytes, buffer, out var written))
                return ToUrlSafe(Convert.ToBase64String(bytes));

            return ToUrlSafe(buffer.Slice(0, written));
        }

        return ToUrlSafe(Convert.ToBase64String(bytes));
#else
        return ToUrlSafe(Convert.ToBase64String(bytes.ToArray()));
#endif
    }

    /// <summary>
    ///     Attempts to decode a URL-safe Base64 string, tolerating missing or present padding.
    /// </summary>
    /// <param name="input">The URL-safe Base64 string. May be <c>null</c>, empty, padded, or unpadded.</param>
    /// <param name="bytes">
    ///     On success, the decoded bytes. On failure (null/whitespace/malformed input), an empty array.
    /// </param>
    /// <returns><c>true</c> when decoding succeeds; otherwise <c>false</c>.</returns>
    /// <remarks>
    ///     Catches both <see cref="FormatException" /> and <see cref="ArgumentException" /> —
    ///     <see cref="Convert.FromBase64String" /> throws <see cref="ArgumentException" />
    ///     on certain invalid lengths, which a narrow <see cref="FormatException" /> catch would miss.
    /// </remarks>
    public static bool TryDecode(string? input, out byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            bytes = Array.Empty<byte>();
            return false;
        }

        var normalized = input!.Replace('-', '+').Replace('_', '/');
        var padding = (4 - (normalized.Length % 4)) % 4;
        if (padding > 0) normalized += new string('=', padding);

        try
        {
            bytes = Convert.FromBase64String(normalized);
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
        catch (ArgumentException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    ///     Generates a URL-safe Base64 string backed by <paramref name="byteLength" /> bytes
    ///     of cryptographically strong randomness.
    /// </summary>
    /// <param name="byteLength">Number of random bytes to generate. Must be greater than zero.</param>
    /// <returns>A URL-safe, unpadded Base64 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="byteLength" /> is zero or negative.
    /// </exception>
    public static string NewRandom(int byteLength)
    {
        if (byteLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "Value must be greater than zero.");

#if NET5_0_OR_GREATER
        if (byteLength <= 256)
        {
            Span<byte> buffer = stackalloc byte[byteLength];
            RandomNumberGenerator.Fill(buffer);
            return Encode(buffer);
        }

        var rented = ArrayPool<byte>.Shared.Rent(byteLength);
        try
        {
            var span = rented.AsSpan(0, byteLength);
            RandomNumberGenerator.Fill(span);
            return Encode(span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
#else
        var buffer = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }

        return Encode(buffer);
#endif
    }

    /// <summary>
    ///     Computes the RFC 7636 PKCE S256 code challenge: SHA-256 over the UTF-8 bytes of
    ///     <paramref name="verifier" />, encoded as URL-safe Base64 without padding.
    /// </summary>
    /// <param name="verifier">The PKCE code verifier.</param>
    /// <returns>The Base64Url-encoded SHA-256 hash of the verifier.</returns>
    public static string Sha256Challenge(ReadOnlySpan<char> verifier)
    {
#if NET5_0_OR_GREATER
        var byteCount = Encoding.UTF8.GetByteCount(verifier);
        if (byteCount <= StackallocByteThreshold)
        {
            Span<byte> utf8 = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(verifier, utf8);
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(utf8, hash);
            return Encode(hash);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var utf8 = rented.AsSpan(0, byteCount);
                Encoding.UTF8.GetBytes(verifier, utf8);
                Span<byte> hash = stackalloc byte[32];
                SHA256.HashData(utf8, hash);
                return Encode(hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
#else
        var utf8 = Encoding.UTF8.GetBytes(verifier.ToString());
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(utf8);
        return Encode(hash);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToUrlSafe(string padded)
    {
        var end = padded.Length;
        while (end > 0 && padded[end - 1] == '=') end--;
        if (end == 0) return string.Empty;

        var buffer = new char[end];
        for (var i = 0; i < end; i++)
        {
            var c = padded[i];
            buffer[i] = c switch
            {
                '+' => '-',
                '/' => '_',
                _ => c
            };
        }

        return new string(buffer);
    }

#if NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToUrlSafe(ReadOnlySpan<char> padded)
    {
        var end = padded.Length;
        while (end > 0 && padded[end - 1] == '=') end--;
        if (end == 0) return string.Empty;

        Span<char> buffer = end <= 256 ? stackalloc char[end] : new char[end];
        for (var i = 0; i < end; i++)
        {
            var c = padded[i];
            buffer[i] = c switch
            {
                '+' => '-',
                '/' => '_',
                _ => c
            };
        }

        return new string(buffer);
    }
#endif
}

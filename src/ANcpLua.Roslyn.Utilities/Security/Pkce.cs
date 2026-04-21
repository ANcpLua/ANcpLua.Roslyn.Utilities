namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     RFC 7636 Proof Key for Code Exchange (PKCE) S256 helpers.
/// </summary>
/// <remarks>
///     <para>
///         Composes <see cref="Base64Url" /> (for encoding) and <see cref="CryptoCompare" />
///         (for constant-time comparison). All three primitives live in this package so a
///         full PKCE client-side or authorization-server-side flow can be built without any
///         additional dependencies.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="GenerateVerifier" /> — emits the 43-char URL-safe Base64 verifier
///                 (256 random bits by default — RFC 7636 allows 32–96 bytes of entropy).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ComputeS256Challenge" /> — SHA-256 over the UTF-8 bytes of the
///                 verifier, Base64Url-encoded (RFC 7636 §4.2).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ValidateS256" /> — server-side verification in constant time.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Pkce
{
    /// <summary>
    ///     Generates an RFC 7636 <c>code_verifier</c>: URL-safe Base64 over <paramref name="byteLength" />
    ///     bytes of cryptographic randomness.
    /// </summary>
    /// <param name="byteLength">Random-byte count. Defaults to 32 (→ 43-char verifier, 256 bits).</param>
    /// <returns>A URL-safe, unpadded Base64 verifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="byteLength" /> is outside the RFC 7636 allowed range of 32–96.
    /// </exception>
    public static string GenerateVerifier(int byteLength = 32)
    {
        if (byteLength is < 32 or > 96)
            throw new ArgumentOutOfRangeException(
                nameof(byteLength),
                byteLength,
                "RFC 7636 requires 32–96 random bytes.");

        return Base64Url.NewRandom(byteLength);
    }

    /// <summary>
    ///     Computes the RFC 7636 S256 <c>code_challenge</c> for the supplied verifier:
    ///     <c>BASE64URL-ENCODE(SHA256(ASCII(code_verifier)))</c>.
    /// </summary>
    /// <param name="verifier">The PKCE <c>code_verifier</c>.</param>
    /// <returns>The Base64Url-encoded SHA-256 hash of the verifier.</returns>
    /// <seealso cref="Base64Url.Sha256Challenge" />
    public static string ComputeS256Challenge(ReadOnlySpan<char> verifier) =>
        Base64Url.Sha256Challenge(verifier);

    /// <summary>
    ///     Validates a verifier against a stored challenge in constant time.
    /// </summary>
    /// <param name="verifier">The client-supplied <c>code_verifier</c>.</param>
    /// <param name="storedChallenge">The previously-stored <c>code_challenge</c> (S256).</param>
    /// <returns><see langword="true" /> when the verifier matches the challenge; otherwise <see langword="false" />.</returns>
    /// <remarks>
    ///     <para>
    ///         The computed challenge and the stored challenge are both 43-character Base64Url
    ///         strings; <see cref="CryptoCompare.FixedTimeEqualsUtf8" /> SHA-256-normalizes to
    ///         32 bytes so there is no length or branch timing leak (CWE-208).
    ///     </para>
    /// </remarks>
    public static bool ValidateS256(string verifier, string storedChallenge)
    {
        var computed = ComputeS256Challenge(verifier.AsSpan());
        return CryptoCompare.FixedTimeEqualsUtf8(computed, storedChallenge);
    }
}

using System.Security.Cryptography;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    /// <summary>
    ///     Computes a deterministic 8-character uppercase hexadecimal hash from a string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>
    ///     An 8-character uppercase hexadecimal string derived from the SHA-256 hash of the input.
    ///     Returns <c>"00000000"</c> if the input is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Produces a short, deterministic identifier suitable for use as a suffix in generated type
    ///         names, file hint names, or graph node IDs where full hashes are too long.
    ///     </para>
    /// </remarks>
    public static string ToShortHash(this string input)
    {
        return ToShortHash(input, 8);
    }

    /// <summary>
    ///     Computes a deterministic N-character hexadecimal hash from a string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <param name="hexChars">Number of hex characters to return (1–64).</param>
    /// <param name="lowercase">
    ///     If <c>true</c>, returns lowercase hex; otherwise uppercase. Defaults to <c>false</c> for visual
    ///     parity with <see cref="ToShortHash(string)" />.
    /// </param>
    /// <returns>
    ///     An <paramref name="hexChars" />-character hexadecimal string derived from the SHA-256 hash of
    ///     the input. Returns a string of <paramref name="hexChars" /> zeroes if the input is <c>null</c>
    ///     or empty.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="hexChars" /> is not in the range 1–64.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The two overloads share an internal implementation so adding cases doesn't require two
    ///         parallel branches. Cyclomatic complexity per overload stays at 1–2.
    ///     </para>
    /// </remarks>
    public static string ToShortHash(this string input, int hexChars, bool lowercase = false)
    {
        if (hexChars is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(hexChars), hexChars, "Must be 1–64.");

        if (string.IsNullOrEmpty(input))
            return new string('0', hexChars);

        var hex = ComputeSha256Hex(input);
        if (lowercase) hex = hex.ToLowerInvariant();
        return hex[..hexChars];
    }

    private static string ComputeSha256Hex(string input)
    {
#if NET5_0_OR_GREATER
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
#else
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "");
#endif
    }
}

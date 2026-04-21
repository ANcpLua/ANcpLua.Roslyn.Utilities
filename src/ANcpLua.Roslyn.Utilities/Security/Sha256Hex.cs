using System.Security.Cryptography;

namespace ANcpLua.Roslyn.Utilities.Security;

/// <summary>
///     SHA-256 of UTF-8 input returned as lowercase hex or URL-safe base64 — the two shapes that appear
///     repeatedly for fingerprints, cache keys, and content-addressed artifact ids.
/// </summary>
/// <remarks>Use for non-cryptographic grouping/fingerprinting. For password hashing use a KDF (PBKDF2, Argon2).</remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Sha256Hex
{
    /// <summary>Lowercase-hex SHA-256 of UTF-8 <paramref name="value" />. 64 chars.</summary>
    public static string Hash(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = HexChar(b >> 4);
            chars[i * 2 + 1] = HexChar(b & 0xF);
        }
        return new string(chars);

        static char HexChar(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + nibble - 10);
    }
}

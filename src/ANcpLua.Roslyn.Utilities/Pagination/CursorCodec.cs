using System.Buffers.Binary;

namespace ANcpLua.Roslyn.Utilities.Pagination;

/// <summary>
///     Opaque cursor codec for timestamp-based pagination.
///     Encodes a Unix nanosecond timestamp into a compact URL-safe Base64 token.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class CursorCodec
{
    private const string Prefix = "c_";

    /// <summary>
    ///     Encodes a Unix nanosecond timestamp into an opaque cursor string.
    /// </summary>
    /// <param name="unixNano">The Unix timestamp in nanoseconds to encode.</param>
    /// <returns>A URL-safe opaque cursor string.</returns>
    public static string Encode(ulong unixNano)
    {
        var bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, unixNano);
        var token = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return Prefix + token;
    }

    /// <summary>
    ///     Attempts to decode an opaque cursor string back into a Unix nanosecond timestamp.
    /// </summary>
    /// <param name="cursor">The cursor string to decode.</param>
    /// <param name="unixNano">When this method returns, contains the decoded timestamp if successful.</param>
    /// <returns><see langword="true" /> if the cursor was successfully decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryDecode(string? cursor, out ulong unixNano)
    {
        unixNano = 0;
        if (string.IsNullOrWhiteSpace(cursor) || !cursor.StartsWithOrdinal(Prefix))
            return false;

        try
        {
            var raw = cursor!.Substring(Prefix.Length)
                .Replace('-', '+')
                .Replace('_', '/');

            var padded = raw.PadRight(raw.Length + ((4 - (raw.Length % 4)) % 4), '=');
            var bytes = Convert.FromBase64String(padded);
            if (bytes.Length != sizeof(ulong))
                return false;

            unixNano = BinaryPrimitives.ReadUInt64BigEndian(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

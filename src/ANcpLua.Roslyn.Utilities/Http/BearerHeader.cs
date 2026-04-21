namespace ANcpLua.Roslyn.Utilities.Http;

/// <summary>
///     Construction and extraction for <c>Authorization: Bearer &lt;token&gt;</c> headers.
///     Handles the case-insensitive scheme match and trims surrounding whitespace that wire-format parsers leave behind.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class BearerHeader
{
    private const string Scheme = "Bearer ";

    /// <summary>Returns <c>"Bearer {token}"</c>. Throws on null/empty <paramref name="token" /> — silent empty headers hide configuration bugs.</summary>
    public static string Build(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bearer token must be non-empty.", nameof(token));
        return Scheme + token;
    }

    /// <summary>Extracts the token from an <c>Authorization</c> header value if it starts with <c>"Bearer "</c> (case-insensitive).</summary>
    /// <returns><c>true</c> and the trimmed token when present; <c>false</c> with <paramref name="token" /> set to <c>null</c> otherwise.</returns>
    public static bool TryExtract(string? headerValue, out string? token)
    {
        token = null;
        if (string.IsNullOrWhiteSpace(headerValue)) return false;
        if (!headerValue!.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)) return false;
        var candidate = headerValue.Substring(Scheme.Length).Trim();
        if (candidate.Length == 0) return false;
        token = candidate;
        return true;
    }
}

namespace ANcpLua.Roslyn.Utilities.Web;

/// <summary>
///     URL query-string assembly that skips <c>null</c>/empty values and escapes every value with
///     <see cref="Uri.EscapeDataString" />. Replaces the <c>List&lt;string&gt; + conditional Add + string.Join("&amp;",...)</c>
///     pattern that accumulates anywhere an HTTP client composes a URL from a few optional filters.
/// </summary>
/// <example><code>
///     var url = "/api/v1/logs?limit=100" + QueryString.AppendPairs(
///         ("trace", traceId), ("level", level), ("service", serviceName));
///     // → "/api/v1/logs?limit=100&amp;trace=abc&amp;level=error" (service omitted if null)
/// </code></example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class QueryString
{
    /// <summary>
    ///     Builds <c>key=encodedValue</c> pairs joined by <c>&amp;</c>. Pairs with <c>null</c> or whitespace values are skipped;
    ///     the result does NOT include a leading <c>?</c>.
    /// </summary>
    public static string Build(params (string Key, string? Value)[] pairs)
    {
        if (pairs is null || pairs.Length == 0) return string.Empty;
        var sb = new StringBuilder(pairs.Length * 16);
        var first = true;
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!first) sb.Append('&');
            first = false;
            sb.Append(key).Append('=').Append(Uri.EscapeDataString(value!));
        }
        return sb.ToString();
    }

    /// <summary>
    ///     Returns <paramref name="url" /> with <paramref name="pairs" /> appended. Leading <c>?</c> or <c>&amp;</c>
    ///     is chosen automatically based on whether <paramref name="url" /> already contains a <c>?</c>.
    ///     Empty/whitespace values are skipped.
    /// </summary>
    public static string AppendPairs(string url, params (string Key, string? Value)[] pairs)
    {
        if (url is null) throw new ArgumentNullException(nameof(url));
        var query = Build(pairs);
        if (query.Length == 0) return url;
        var separator = url.IndexOf('?') >= 0 ? '&' : '?';
        return url + separator + query;
    }
}

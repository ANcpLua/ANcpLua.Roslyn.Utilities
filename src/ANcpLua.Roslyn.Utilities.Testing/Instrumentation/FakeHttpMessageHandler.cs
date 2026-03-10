using System.Net;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Configurable <see cref="HttpMessageHandler"/> that returns canned responses
///     and records all requests for assertion.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var handler = new FakeHttpMessageHandler()
///         .WithResponse("*/oauth/access_token", HttpStatusCode.OK, """{"access_token":"tok"}""")
///         .WithResponse("*/user", HttpStatusCode.OK, """{"login":"testuser"}""");
///
///     using var httpClient = new HttpClient(handler);
///     // ... use httpClient in code under test ...
///
///     Assert.Equal(2, handler.Requests.Count);
///     </code>
/// </remarks>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<ResponseRule> _rules = [];
    private readonly Lock _lock = new();

    /// <summary>All requests sent through this handler, in order.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>
    ///     Fallback status code when no rule matches. Defaults to <see cref="HttpStatusCode.NotFound"/>.
    /// </summary>
    public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.NotFound;

    // ── Builder methods ──────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a rule: requests whose URL contains <paramref name="urlPattern"/> get the
    ///     specified response. Rules are evaluated in order; first match wins.
    /// </summary>
    /// <param name="urlPattern">Substring to match in the request URL. Use "*" prefix for contains-match.</param>
    /// <param name="statusCode">HTTP status code to return.</param>
    /// <param name="body">Response body (JSON or other content).</param>
    /// <param name="contentType">Content-Type header. Defaults to "application/json".</param>
    public FakeHttpMessageHandler WithResponse(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        string contentType = "application/json")
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, body, contentType, Times: null));
        return this;
    }

    /// <summary>
    ///     Adds a rule that returns an error response (empty body) for matching URLs.
    /// </summary>
    public FakeHttpMessageHandler WithError(string urlPattern, HttpStatusCode statusCode)
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, string.Empty, "text/plain", Times: null));
        return this;
    }

    /// <summary>
    ///     Adds a rule that matches only N times, then falls through to the next rule.
    /// </summary>
    public FakeHttpMessageHandler WithResponse(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        int times,
        string contentType = "application/json")
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, body, contentType, times));
        return this;
    }

    // ── HttpMessageHandler ───────────────────────────────────────────────────

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var url = request.RequestUri?.ToString() ?? string.Empty;

        using (_lock.EnterScope())
        {
            Requests.Add(new RecordedRequest(request.Method, url, request.Headers.Authorization?.ToString()));

            foreach (var rule in _rules)
            {
                if (!url.Contains(rule.UrlPattern, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (rule.Times.HasValue)
                {
                    if (rule.RemainingUses <= 0)
                        continue;
                    rule.RemainingUses--;
                }

                return Task.FromResult(new HttpResponseMessage(rule.StatusCode)
                {
                    Content = new StringContent(rule.Body, Encoding.UTF8, rule.ContentType)
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(DefaultStatusCode));
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private sealed class ResponseRule(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        string contentType,
        int? Times)
    {
        public string UrlPattern => urlPattern;
        public HttpStatusCode StatusCode => statusCode;
        public string Body => body;
        public string ContentType => contentType;
        public int? Times => Times;
        public int RemainingUses { get; set; } = Times ?? 0;
    }
}

/// <summary>Records a single HTTP request sent through <see cref="FakeHttpMessageHandler"/>.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Url">The full request URL.</param>
/// <param name="Authorization">The Authorization header value, if present.</param>
public sealed record RecordedRequest(HttpMethod Method, string Url, string? Authorization);

// Licensed to the .NET Foundation under one or more agreements.

using System.Net;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     Configurable <see cref="HttpMessageHandler" /> that returns canned responses
///     and records all requests for assertion. Supports URL-pattern matching,
///     SSE streams, request validation, and limited-use rules.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var handler = new FakeHttpMessageHandler()
///         .WithResponse("/oauth/token", HttpStatusCode.OK, """{"access_token":"tok"}""")
///         .WithSseResponse("/events", ["event1", "event2"])
///         .WithRequestValidator(req => Assert.Equal(HttpMethod.Post, req.Method));
/// 
///     using var httpClient = handler.BuildHttpClient("https://api.example.com");
///     // ... use httpClient in code under test ...
/// 
///     Assert.Equal(2, handler.Requests.Count);
///     </code>
/// </remarks>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Lock _lock = new();
    private readonly List<Action<HttpRequestMessage>> _requestValidators = [];
    private readonly List<ResponseRule> _rules = [];

    /// <summary>All requests sent through this handler, in order.</summary>
    public IList<RecordedRequest> Requests { get; } = [];

    /// <summary>
    ///     Fallback status code when no rule matches. Defaults to <see cref="HttpStatusCode.NotFound" />.
    /// </summary>
    public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.NotFound;

    // ── Builder methods ──────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a rule: requests whose URL contains <paramref name="urlPattern" /> get the
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
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, body, contentType, times: null));
        return this;
    }

    /// <summary>
    ///     Adds a rule that returns an error response (empty body) for matching URLs.
    /// </summary>
    public FakeHttpMessageHandler WithError(string urlPattern, HttpStatusCode statusCode)
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, string.Empty, "text/plain", times: null));
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

    /// <summary>
    ///     Adds a rule returning an SSE (Server-Sent Events) stream for matching URLs.
    ///     Each event string should be the JSON payload (without the "data:" prefix).
    /// </summary>
    /// <param name="urlPattern">Substring to match in the request URL.</param>
    /// <param name="serializedEvents">JSON payloads for each SSE event.</param>
    public FakeHttpMessageHandler WithSseResponse(
        string urlPattern,
        IEnumerable<string> serializedEvents)
    {
        StringBuilder sb = new();
        foreach (var e in serializedEvents) sb.Append("data: ").Append(e).Append("\n\n");

        _rules.Add(new ResponseRule(
            urlPattern.TrimStart('*'), HttpStatusCode.OK, sb.ToString(), "text/event-stream", times: null));
        return this;
    }

    /// <summary>
    ///     Adds a request validator called for every request passing through this handler.
    ///     Validators run before response matching. Use to assert request properties
    ///     (URL, headers, body) in tests.
    /// </summary>
    public FakeHttpMessageHandler WithRequestValidator(Action<HttpRequestMessage> validator)
    {
        _requestValidators.Add(validator);
        return this;
    }

    /// <summary>
    ///     Creates an <see cref="HttpClient" /> backed by this handler.
    ///     The handler is not disposed when the client is disposed.
    /// </summary>
    public HttpClient BuildHttpClient()
    {
        return new HttpClient(this, disposeHandler: false);
    }

    /// <summary>
    ///     Creates an <see cref="HttpClient" /> with the specified base address.
    ///     The handler is not disposed when the client is disposed.
    /// </summary>
    public HttpClient BuildHttpClient(string baseAddress)
    {
        return new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri(baseAddress) };
    }

    // ── HttpMessageHandler ───────────────────────────────────────────────────

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var validator in _requestValidators) validator(request);

        var url = request.RequestUri?.ToString() ?? string.Empty;

        using (_lock.EnterScope())
        {
            Requests.Add(new RecordedRequest(request.Method, url, request.Headers.Authorization?.ToString()));

            foreach (var rule in _rules)
            {
                if (!url.Contains(rule.UrlPattern, StringComparison.OrdinalIgnoreCase)) continue;

                if (rule.IsLimited)
                {
                    if (rule.RemainingUses <= 0) continue;

                    rule.RemainingUses--;
                }

                return Task.FromResult(new HttpResponseMessage(rule.StatusCode)
                {
                    Content = new StringContent(rule.Body, Encoding.UTF8, rule.ContentType),
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(DefaultStatusCode));
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private sealed class ResponseRule
    {
        public ResponseRule(
            string urlPattern,
            HttpStatusCode statusCode,
            string body,
            string contentType,
            int? times)
        {
            UrlPattern = urlPattern;
            StatusCode = statusCode;
            Body = body;
            ContentType = contentType;
            IsLimited = times.HasValue;
            RemainingUses = times ?? 0;
        }

        public string UrlPattern { get; }
        public HttpStatusCode StatusCode { get; }
        public string Body { get; }
        public string ContentType { get; }
        public bool IsLimited { get; }
        public int RemainingUses { get; set; }
    }
}
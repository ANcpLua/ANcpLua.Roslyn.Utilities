// Licensed to the .NET Foundation under one or more agreements.

using System.Net;
using System.Text;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Fluent builder for creating mock <see cref="HttpMessageHandler"/> instances for testing HTTP interactions.
/// Supports SSE streams, custom status codes, and request validation.
/// </summary>
public sealed class MockHttpMessageHandlerBuilder
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = string.Empty;
    private string _contentType = "text/plain";
    private readonly List<Action<HttpRequestMessage>> _requestValidators = [];

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static MockHttpMessageHandlerBuilder Create() => new();

    /// <summary>
    /// Sets the HTTP status code to return.
    /// </summary>
    public MockHttpMessageHandlerBuilder WithStatusCode(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Sets the response body content and content type.
    /// </summary>
    public MockHttpMessageHandlerBuilder WithContent(string content, string contentType = "text/plain")
    {
        _content = content;
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Configures the response as an SSE stream from pre-serialized event strings.
    /// Each event string should be the JSON payload (without "data:" prefix).
    /// </summary>
    public MockHttpMessageHandlerBuilder WithSseEvents(IEnumerable<string> serializedEvents)
    {
        StringBuilder sb = new();
        foreach (string e in serializedEvents)
        {
            sb.Append("data: ").Append(e).Append("\n\n");
        }

        _content = sb.ToString();
        _contentType = "text/event-stream";
        return this;
    }

    /// <summary>
    /// Adds a request validator that will be called with the outgoing <see cref="HttpRequestMessage"/>.
    /// Use this to assert request properties (URL, headers, body) in tests.
    /// </summary>
    public MockHttpMessageHandlerBuilder WithRequestValidator(Action<HttpRequestMessage> validator)
    {
        _requestValidators.Add(validator);
        return this;
    }

    /// <summary>
    /// Builds an <see cref="HttpClient"/> backed by the configured mock handler.
    /// </summary>
    public HttpClient BuildHttpClient()
    {
        MockHandler handler = new(_statusCode, _content, _contentType, _requestValidators);
        return new HttpClient(handler);
    }

    /// <summary>
    /// Builds an <see cref="HttpClient"/> with the specified base address.
    /// </summary>
    public HttpClient BuildHttpClient(string baseAddress)
    {
        HttpClient client = BuildHttpClient();
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    private sealed class MockHandler(
        HttpStatusCode statusCode,
        string content,
        string contentType,
        List<Action<HttpRequestMessage>> validators) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            foreach (Action<HttpRequestMessage> validator in validators)
            {
                validator(request);
            }

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, contentType),
            });
        }
    }
}

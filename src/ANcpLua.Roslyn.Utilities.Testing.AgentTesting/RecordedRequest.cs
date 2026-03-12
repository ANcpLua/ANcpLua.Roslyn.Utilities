// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>Records a single HTTP request sent through <see cref="FakeHttpMessageHandler" />.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Url">The full request URL.</param>
/// <param name="Authorization">The Authorization header value, if present.</param>
public sealed record RecordedRequest(HttpMethod Method, string Url, string? Authorization);
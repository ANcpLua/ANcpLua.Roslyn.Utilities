// Licensed to the .NET Foundation under one or more agreements.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Sets up an in-memory test server with an AG-UI endpoint for integration testing.
/// Handles <see cref="WebApplication"/> lifecycle including disposal.
/// </summary>
public sealed class AGUITestServer : IAsyncDisposable
{
    private WebApplication? _app;

    /// <summary>
    /// Gets the <see cref="HttpClient"/> configured to target the test server.
    /// Available after <see cref="StartAsync"/> completes.
    /// </summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Gets the endpoint pattern used for the AG-UI endpoint.
    /// </summary>
    public string EndpointPattern { get; }

    private readonly AIAgent _agent;
    private readonly Action<WebApplicationBuilder>? _configureBuilder;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly JsonSerializerOptions? _jsonOptions;

    private AGUITestServer(
        AIAgent agent,
        string endpointPattern,
        Action<WebApplicationBuilder>? configureBuilder,
        Action<IServiceCollection>? configureServices,
        JsonSerializerOptions? jsonOptions)
    {
        _agent = agent;
        EndpointPattern = endpointPattern;
        _configureBuilder = configureBuilder;
        _configureServices = configureServices;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Creates and starts a test server with the given agent mapped to the specified endpoint.
    /// </summary>
    public static async Task<AGUITestServer> CreateAsync(
        AIAgent agent,
        string endpointPattern = "/agent",
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<IServiceCollection>? configureServices = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        AGUITestServer server = new(agent, endpointPattern, configureBuilder, configureServices, jsonOptions);
        await server.StartAsync();
        return server;
    }

    /// <summary>
    /// Creates and starts a test server with a <see cref="FakeTextStreamingAgent"/>
    /// that streams the given chunks.
    /// </summary>
    public static Task<AGUITestServer> CreateWithFakeAgentAsync(
        string endpointPattern = "/agent",
        params string[] chunks)
    {
        string[] effectiveChunks = chunks.Length > 0 ? chunks : ["Hello", " from", " fake", " agent!"];
        return CreateAsync(new FakeTextStreamingAgent(effectiveChunks), endpointPattern);
    }

    private async Task StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        _configureBuilder?.Invoke(builder);

        builder.Services.AddAGUI();
        _configureServices?.Invoke(builder.Services);

        if (_jsonOptions?.TypeInfoResolver is not null)
        {
            builder.Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.TypeInfoResolverChain.Add(_jsonOptions.TypeInfoResolver));
        }

        _app = builder.Build();
        _app.MapAGUI(EndpointPattern, _agent);

        await _app.StartAsync();

        TestServer testServer = _app.Services.GetRequiredService<IServer>() as TestServer
            ?? throw new InvalidOperationException("TestServer not found in services.");

        Client = testServer.CreateClient();
        Client.BaseAddress = new Uri($"http://localhost{EndpointPattern}");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

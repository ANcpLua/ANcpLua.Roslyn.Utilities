using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using TUnit.Core;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting.TUnit;

/// <summary>
///     Real Kestrel test base for TUnit. HTTP/2, WebSockets, SSE, and Playwright testing.
///     Uses UseKestrel(0) + StartServer() for real network I/O.
/// </summary>
/// <typeparam name="TProgram">The web application's Program class.</typeparam>
public abstract class KestrelTestBase<TProgram>
    where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;
    private HttpClient? _client;
    private FakeLogCollector? _logs;

    /// <summary>HTTP client for making requests to the test server. Available after <see cref="SetUp" />.</summary>
    protected HttpClient Client =>
        _client ?? throw new InvalidOperationException("Server not started. Await SetUp first.");

    /// <summary>Fake log collector for asserting log output. Available after <see cref="SetUp" />.</summary>
    protected FakeLogCollector Logs =>
        _logs ?? throw new InvalidOperationException("Server not started. Await SetUp first.");

    /// <summary>The base address of the running server.</summary>
    protected Uri BaseAddress =>
        _factory?.ClientOptions.BaseAddress ?? throw new InvalidOperationException("Server not started.");

    /// <summary>Starts a fresh Kestrel server per test.</summary>
    [Before(HookType.Test)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The factory lifetime is stored in _factory and disposed by TearDown.")]
    public virtual Task SetUp()
    {
        _factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(ConfigureTestServices));
        _factory.UseKestrel(0);
        _factory.StartServer();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _logs = _factory.Services.GetFakeLogCollector();
        return Task.CompletedTask;
    }

    /// <summary>Disposes the server after each test.</summary>
    [After(HookType.Test)]
    public virtual async Task TearDown()
    {
        _client?.Dispose();
        if (_factory != null) await _factory.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Override to configure test services. Default adds fake logging.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        services.AddFakeLogging();
    }
}

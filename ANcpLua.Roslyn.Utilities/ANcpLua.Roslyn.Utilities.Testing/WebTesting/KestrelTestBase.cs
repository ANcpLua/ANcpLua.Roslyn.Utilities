using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting;

/// <summary>
/// Real Kestrel test base for HTTP/2, WebSockets, SSE, and Playwright testing.
/// Uses UseKestrel(0) + StartServer() for real network I/O.
/// </summary>
/// <typeparam name="TProgram">The web application's Program class.</typeparam>
public abstract class KestrelTestBase<TProgram> : IAsyncLifetime
    where TProgram : class
{
    private readonly WebApplicationFactory<TProgram> _baseFactory;

    /// <summary>HTTP client for making requests to the test server.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Fake log collector for asserting log output.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    protected FakeLogCollector? Logs { get; private set; }

    /// <summary>The base address of the running server.</summary>
    protected Uri BaseAddress => _baseFactory.ClientOptions.BaseAddress;

    /// <summary>
    /// Creates a new Kestrel test base.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory to use as base.</param>
    protected KestrelTestBase(WebApplicationFactory<TProgram> factory)
    {
        _baseFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(ConfigureTestServices);
        });
    }

    /// <summary>
    /// Override to configure test services. Default adds fake logging.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        services.AddFakeLogging();
    }

    /// <inheritdoc />
    public virtual ValueTask InitializeAsync()
    {
        _baseFactory.UseKestrel(0);
        _baseFactory.StartServer();
        Client = _baseFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        Logs = _baseFactory.Services.GetFakeLogCollector();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        await _baseFactory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

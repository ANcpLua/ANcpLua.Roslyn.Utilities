using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting;

/// <summary>
/// Fast integration test base using in-memory TestServer.
/// Use for most API tests that don't require real network I/O.
/// </summary>
/// <typeparam name="TProgram">The web application's Program class.</typeparam>
public abstract class IntegrationTestBase<TProgram> : IClassFixture<WebApplicationFactory<TProgram>>, IAsyncLifetime
    where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;

    /// <summary>The configured WebApplicationFactory.</summary>
    protected WebApplicationFactory<TProgram> Factory => _factory!;

    /// <summary>HTTP client for making requests to the test server.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Fake log collector for asserting log output.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    protected FakeLogCollector? Logs { get; private set; }

    /// <summary>
    /// Creates a new integration test base.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory provided by xUnit fixture.</param>
    protected IntegrationTestBase(WebApplicationFactory<TProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => { builder.ConfigureTestServices(ConfigureTestServices); });
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
        Client = Factory.CreateClient();
        Logs = Factory.Services.GetFakeLogCollector();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

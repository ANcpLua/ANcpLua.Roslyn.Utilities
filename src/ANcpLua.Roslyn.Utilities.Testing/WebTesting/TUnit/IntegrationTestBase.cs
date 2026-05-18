using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using TUnit.Core;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting.TUnit;

/// <summary>
///     Fast integration test base using in-memory TestServer for TUnit.
///     Use for most API tests that don't require real network I/O.
/// </summary>
/// <typeparam name="TProgram">The web application's Program class.</typeparam>
public abstract class IntegrationTestBase<TProgram>
    where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;

    /// <summary>The configured WebApplicationFactory.</summary>
    protected WebApplicationFactory<TProgram> Factory =>
        _factory ?? throw new InvalidOperationException("Not initialized. Await SetUp first.");

    /// <summary>HTTP client for making requests to the test server.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    ///     Fake log collector for asserting log output.
    ///     Available after <see cref="SetUp" /> completes.
    /// </summary>
    protected FakeLogCollector? Logs { get; private set; }

    /// <summary>Creates the factory, HTTP client, and log collector per test.</summary>
    [Before(HookType.Test)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The factory lifetime is stored in _factory and disposed by TearDown.")]
    public virtual Task SetUp()
    {
        _factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(ConfigureTestServices));
        Client = _factory.CreateClient();
        Logs = _factory.Services.GetFakeLogCollector();
        return Task.CompletedTask;
    }

    /// <summary>Disposes the HTTP client and factory after each test.</summary>
    [After(HookType.Test)]
    public virtual async Task TearDown()
    {
        Client.Dispose();
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

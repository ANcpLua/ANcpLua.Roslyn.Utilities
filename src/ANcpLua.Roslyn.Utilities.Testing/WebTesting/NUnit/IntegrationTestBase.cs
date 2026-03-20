using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting.NUnit;

/// <summary>
///     Fast integration test base using in-memory TestServer for NUnit.
///     Use for most API tests that don't require real network I/O.
/// </summary>
/// <typeparam name="TProgram">The web application's Program class.</typeparam>
public abstract class IntegrationTestBase<TProgram>
    where TProgram : class
{
    /// <summary>The configured WebApplicationFactory.</summary>
    protected WebApplicationFactory<TProgram> Factory { get; private set; } = null!;

    /// <summary>HTTP client for making requests to the test server.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    ///     Fake log collector for asserting log output.
    ///     Available after <see cref="SetUp" /> completes.
    /// </summary>
    protected FakeLogCollector? Logs { get; private set; }

    /// <summary>Creates the WebApplicationFactory once per test class.</summary>
    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        Factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(ConfigureTestServices));
    }

    /// <summary>Creates a fresh HTTP client and log collector per test.</summary>
    [SetUp]
    public virtual void SetUp()
    {
        Client = Factory.CreateClient();
        Logs = Factory.Services.GetFakeLogCollector();
    }

    /// <summary>Disposes the HTTP client after each test.</summary>
    [TearDown]
    public virtual void TearDown()
    {
        Client.Dispose();
    }

    /// <summary>Disposes the WebApplicationFactory after all tests in the class.</summary>
    [OneTimeTearDown]
    public virtual async Task OneTimeTearDownAsync()
    {
        await Factory.DisposeAsync();
    }

    /// <summary>
    ///     Override to configure test services. Default adds fake logging.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        services.AddFakeLogging();
    }
}

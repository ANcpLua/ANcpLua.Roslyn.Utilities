using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing.WebTesting.Bunit;

/// <summary>
///     Base class for bUnit Blazor component tests with integrated fake logging.
///     Extends <see cref="BunitContext" /> and pre-configures <see cref="FakeLogCollector" />.
/// </summary>
public abstract class BunitTestBase : BunitContext
{
    private FakeLogCollector? _logs;

    /// <summary>
    ///     Fake log collector for asserting log output.
    ///     Available after the service provider is built (after the first render).
    /// </summary>
    protected FakeLogCollector Logs =>
        _logs ??= Services.GetRequiredService<FakeLogCollector>();

    /// <summary>
    ///     Creates a new bUnit test base with fake logging pre-configured.
    /// </summary>
    protected BunitTestBase()
    {
        Services.AddFakeLogging();
    }
}

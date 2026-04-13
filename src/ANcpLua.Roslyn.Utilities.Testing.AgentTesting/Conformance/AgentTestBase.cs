// Licensed to the .NET Foundation under one or more agreements.

using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Generic base class that owns the lifecycle of an <see cref="IAgentFixture" />.
///     Subclasses pass a fixture factory and inherit <see cref="IAsyncLifetime" /> wiring for free.
/// </summary>
/// <typeparam name="TFixture">Concrete fixture type.</typeparam>
/// <param name="createFixture">Factory invoked once per test instance to build a fresh fixture.</param>
public abstract class AgentTestBase<TFixture>(Func<TFixture> createFixture) : IAsyncLifetime
    where TFixture : IAgentFixture
{
    /// <summary>The fixture under test, available after <see cref="InitializeAsync" /> runs.</summary>
    protected TFixture Fixture { get; private set; } = default!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Fixture = createFixture();
        await Fixture.InitializeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Fixture.DisposeAsync().ConfigureAwait(false);
    }
}

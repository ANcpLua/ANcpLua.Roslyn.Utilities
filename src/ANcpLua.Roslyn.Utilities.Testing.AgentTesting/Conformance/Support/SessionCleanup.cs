// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;

/// <summary><c>await using</c> wrapper that deletes an <see cref="AgentSession" /> on disposal.</summary>
public sealed class SessionCleanup(AgentSession session, IAgentFixture fixture) : IAsyncDisposable
{
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask(fixture.DeleteSessionAsync(session));
    }
}

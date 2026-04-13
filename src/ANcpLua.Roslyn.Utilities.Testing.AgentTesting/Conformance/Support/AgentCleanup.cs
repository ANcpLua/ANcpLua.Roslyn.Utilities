// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;

/// <summary>
///     <c>await using</c> wrapper that deletes a <see cref="ChatClientAgent" /> when disposed.
///     Use inside a test body to keep cleanup at the call site instead of finally blocks.
/// </summary>
public sealed class AgentCleanup(ChatClientAgent agent, IChatClientAgentFixture fixture) : IAsyncDisposable
{
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask(fixture.DeleteAgentAsync(agent));
    }
}

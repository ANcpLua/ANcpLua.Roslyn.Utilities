// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     A simple fake agent that streams deterministic text chunks.
///     Useful for basic streaming integration tests.
/// </summary>
public sealed class FakeTextStreamingAgent(params string[] chunks) : FakeAgentBase
{
    /// <inheritdoc />
    protected override IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        return StreamChunksAsync(chunks, cancellationToken);
    }
}
// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     Streams a fixed list of text chunks under one shared message id.
///     The minimal harness for asserting on streaming-update plumbing.
/// </summary>
public sealed class FakeTextStreamingAgent(params string[] chunks) : FakeAgentBase
{
    /// <inheritdoc />
    protected override IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
        => StreamChunksAsync(chunks, cancellationToken);
}

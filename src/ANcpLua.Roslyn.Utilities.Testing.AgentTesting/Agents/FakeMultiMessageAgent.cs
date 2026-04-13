// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     Streams multiple distinct messages — each with its own message id — in a single turn.
///     Use to verify multi-message streaming and message-id boundary handling.
/// </summary>
public sealed class FakeMultiMessageAgent(params string[][] messageChunks) : FakeAgentBase
{
    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var chunks in messageChunks)
        {
            await foreach (var update in StreamChunksAsync(chunks, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     A fake agent that echoes user messages back as assistant responses.
///     Filters for <see cref="ChatRole.User" /> messages and returns their text
///     with an optional prefix. The simplest possible round-trip test double.
/// </summary>
public sealed class FakeEchoAgent(
    string? id = null,
    string? name = null,
    string? prefix = null,
    TimeProvider? timeProvider = null)
    : FakeAgentBase(id, name, timeProvider)
{
    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            if (message.Role != ChatRole.User || string.IsNullOrEmpty(message.Text))
            {
                continue;
            }

            yield return new AgentResponseUpdate
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Role = ChatRole.Assistant,
                AuthorName = Name ?? Id,
                CreatedAt = Time.GetUtcNow(),
                Contents = [new TextContent(prefix is null ? message.Text : prefix + message.Text)],
            };

            await Task.Yield();
        }
    }
}

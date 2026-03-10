// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// A fake agent that echoes back user messages as assistant responses.
/// Filters for <see cref="ChatRole.User"/> messages and returns their text
/// with an optional prefix. Useful for testing round-trip message flow.
/// </summary>
public sealed class FakeEchoAgent(string? id = null, string? name = null, string? prefix = null) : FakeAgentBase
{
    /// <inheritdoc/>
    protected override string? IdCore => id ?? base.IdCore;

    /// <inheritdoc/>
    public override string? Name => name;

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (ChatMessage message in messages)
        {
            if (message.Role != ChatRole.User || string.IsNullOrEmpty(message.Text))
            {
                continue;
            }

            string echoText = prefix is not null ? $"{prefix}{message.Text}" : message.Text;
            string messageId = Guid.NewGuid().ToString("N");

            yield return new AgentResponseUpdate
            {
                MessageId = messageId,
                Role = ChatRole.Assistant,
                AuthorName = Name ?? Id,
                CreatedAt = TimeProvider.System.GetUtcNow(),
                Contents = [new TextContent(echoText)],
            };

            await Task.Yield();
        }
    }
}

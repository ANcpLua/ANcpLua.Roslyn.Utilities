// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     A fake agent that replays a pre-recorded sequence of <see cref="ChatMessage" /> instances.
///     Each message is streamed content-by-content, preserving message ids and content shape —
///     ideal for deterministic multi-turn and streaming-composition tests.
/// </summary>
public sealed class FakeReplayAgent(
    IReadOnlyList<ChatMessage>? messages = null,
    string? id = null,
    string? name = null,
    TimeProvider? timeProvider = null)
    : FakeAgentBase(id, name, timeProvider)
{
    /// <summary>The pre-recorded messages to replay. Empty if none were supplied.</summary>
    public IReadOnlyList<ChatMessage> Messages { get; } = ValidateNoDuplicateConsecutiveIds(messages) ?? [];

    /// <summary>Build a replay agent from plain text strings, one assistant message per string.</summary>
    public static FakeReplayAgent FromStrings(params string[] texts) => new(texts.ToChatMessages());

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var responseId = Guid.NewGuid().ToString("N");

        foreach (var message in Messages)
        {
            foreach (var content in message.Contents)
            {
                yield return new AgentResponseUpdate
                {
                    AgentId = Id,
                    AuthorName = Name,
                    MessageId = message.MessageId,
                    ResponseId = responseId,
                    Role = message.Role,
                    Contents = [content],
                };
            }

            await Task.Yield();
        }
    }

    private static IReadOnlyList<ChatMessage>? ValidateNoDuplicateConsecutiveIds(IReadOnlyList<ChatMessage>? candidates)
    {
        if (candidates is null)
        {
            return null;
        }

        string? previous = null;
        foreach (var message in candidates)
        {
            if (previous is not null && string.Equals(previous, message.MessageId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Duplicate consecutive message ids are not allowed.", nameof(candidates));
            }

            previous = message.MessageId;
        }

        return candidates;
    }
}

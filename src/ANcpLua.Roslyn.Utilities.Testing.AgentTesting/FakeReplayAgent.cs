// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// A fake agent that replays a pre-recorded sequence of <see cref="ChatMessage"/> instances.
/// Each message is streamed content-by-content, preserving message IDs and structure.
/// Useful for deterministic multi-turn testing and streaming composition validation.
/// </summary>
public sealed class FakeReplayAgent(IReadOnlyList<ChatMessage>? messages = null, string? id = null, string? name = null) : FakeAgentBase
{
    /// <summary>
    /// The pre-recorded messages to replay. Empty if none provided.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; } = ValidateNoDuplicateIds(messages) ?? [];

    /// <inheritdoc/>
    protected override string? IdCore => id ?? base.IdCore;

    /// <inheritdoc/>
    public override string? Name => name;

    /// <summary>
    /// Creates a <see cref="FakeReplayAgent"/> from plain text strings.
    /// Each string is split into word-level <see cref="TextContent"/> items to simulate streaming.
    /// </summary>
    public static FakeReplayAgent FromStrings(params string[] texts) =>
        new(ToChatMessages(texts));

    /// <summary>
    /// Converts plain text strings into <see cref="ChatMessage"/> instances
    /// with word-level content splitting for realistic streaming simulation.
    /// </summary>
    public static IReadOnlyList<ChatMessage> ToChatMessages(params string[] texts) =>
        texts.Select(static text =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return new ChatMessage(ChatRole.Assistant, "") { MessageId = "" };
            }

            string[] splits = text.Split(' ');
            for (int i = 0; i < splits.Length - 1; i++)
            {
                splits[i] += ' ';
            }

            List<AIContent> contents = splits
                .Select<string, AIContent>(static s => new TextContent(s) { RawRepresentation = s })
                .ToList();

            return new ChatMessage(ChatRole.Assistant, contents)
            {
                MessageId = Guid.NewGuid().ToString("N"),
                RawRepresentation = text,
                CreatedAt = TimeProvider.System.GetUtcNow(),
            };
        }).ToList();

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string responseId = Guid.NewGuid().ToString("N");

        foreach (ChatMessage message in Messages)
        {
            foreach (AIContent content in message.Contents)
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

    private static IReadOnlyList<ChatMessage>? ValidateNoDuplicateIds(IReadOnlyList<ChatMessage>? candidateMessages)
    {
        if (candidateMessages is null)
        {
            return null;
        }

        string? previousId = null;
        foreach (ChatMessage message in candidateMessages)
        {
            if (previousId is not null && string.Equals(previousId, message.MessageId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Duplicate consecutive message IDs are not allowed.", nameof(candidateMessages));
            }

            previousId = message.MessageId;
        }

        return candidateMessages;
    }
}

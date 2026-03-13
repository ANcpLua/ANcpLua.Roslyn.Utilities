// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     A fake agent that replays a pre-recorded sequence of <see cref="ChatMessage" /> instances.
///     Each message is streamed content-by-content, preserving message IDs and structure.
///     Useful for deterministic multi-turn testing and streaming composition validation.
/// </summary>
public sealed class FakeReplayAgent(IReadOnlyList<ChatMessage>? messages = null, string? id = null, string? name = null)
    : FakeAgentBase
{
    /// <summary>
    ///     The pre-recorded messages to replay. Empty if none provided.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; } = ValidateNoDuplicateIds(messages) ?? [];

    /// <inheritdoc />
    protected override string? IdCore => id ?? base.IdCore;

    /// <inheritdoc />
    public override string? Name => name;

    /// <summary>
    ///     Creates a <see cref="FakeReplayAgent" /> from plain text strings.
    ///     Each string is split into word-level <see cref="TextContent" /> items to simulate streaming.
    /// </summary>
    public static FakeReplayAgent FromStrings(params string[] texts)
    {
        return new FakeReplayAgent(ToChatMessages(texts));
    }

    /// <summary>
    ///     Converts plain text strings into <see cref="ChatMessage" /> instances
    ///     with word-level content splitting for realistic streaming simulation.
    ///     Delegates to <see cref="ChatMessageExtensions.ToChatMessages" />.
    /// </summary>
    public static IReadOnlyList<ChatMessage> ToChatMessages(params string[] texts)
    {
        return texts.ToChatMessages();
    }

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
                yield return new AgentResponseUpdate
                {
                    AgentId = Id,
                    AuthorName = Name,
                    MessageId = message.MessageId,
                    ResponseId = responseId,
                    Role = message.Role,
                    Contents = [content]
                };

            await Task.Yield();
        }
    }

    private static IReadOnlyList<ChatMessage>? ValidateNoDuplicateIds(IReadOnlyList<ChatMessage>? candidateMessages)
    {
        if (candidateMessages is null) return null;

        string? previousId = null;
        foreach (var message in candidateMessages)
        {
            if (previousId is not null && string.Equals(previousId, message.MessageId, StringComparison.Ordinal))
                throw new ArgumentException("Duplicate consecutive message IDs are not allowed.",
                    nameof(candidateMessages));

            previousId = message.MessageId;
        }

        return candidateMessages;
    }
}
// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Extension methods for building <see cref="ChatMessage"/> and <see cref="AgentResponseUpdate"/>
/// instances from strings, simulating word-level streaming for agent tests.
/// </summary>
public static class ChatMessageExtensions
{
    /// <summary>
    /// Splits a string into word-level <see cref="TextContent"/> items,
    /// simulating how streaming responses arrive one token at a time.
    /// </summary>
    public static IReadOnlyList<AIContent> ToContentStream(this string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return [];
        }

        string[] splits = message.Split(' ');
        for (int i = 0; i < splits.Length - 1; i++)
        {
            splits[i] += " ";
        }

        return splits.Select<string, AIContent>(static text => new TextContent(text) { RawRepresentation = text }).ToList();
    }

    /// <summary>
    /// Wraps a single <see cref="AIContent"/> in an <see cref="AgentResponseUpdate"/>.
    /// </summary>
    public static AgentResponseUpdate ToResponseUpdate(
        this AIContent content,
        string? messageId = null,
        string? responseId = null,
        string? agentId = null,
        string? authorName = null) =>
        new()
        {
            Role = ChatRole.Assistant,
            CreatedAt = TimeProvider.System.GetUtcNow(),
            MessageId = messageId ?? Guid.NewGuid().ToString("N"),
            ResponseId = responseId,
            AgentId = agentId,
            AuthorName = authorName,
            Contents = [content],
        };

    /// <summary>
    /// Converts a string into a sequence of <see cref="AgentResponseUpdate"/> items,
    /// one per word, sharing a single message ID.
    /// </summary>
    public static IEnumerable<AgentResponseUpdate> ToResponseUpdates(
        this string message,
        string? messageId = null,
        string? responseId = null,
        string? agentId = null,
        string? authorName = null)
    {
        messageId ??= Guid.NewGuid().ToString("N");
        return message.ToContentStream()
            .Select(content => content.ToResponseUpdate(messageId, responseId, agentId, authorName));
    }

    /// <summary>
    /// Creates a <see cref="ChatMessage"/> from a content list with assistant role.
    /// </summary>
    public static ChatMessage ToChatMessage(
        this IEnumerable<AIContent> contents,
        string? messageId = null,
        string? authorName = null) =>
        new(ChatRole.Assistant, contents is List<AIContent> list ? list : contents.ToList())
        {
            AuthorName = authorName,
            CreatedAt = TimeProvider.System.GetUtcNow(),
            MessageId = messageId ?? Guid.NewGuid().ToString("N"),
        };

    /// <summary>
    /// Converts a <see cref="ChatMessage"/> into a stream of <see cref="AgentResponseUpdate"/> items,
    /// one per content item.
    /// </summary>
    public static IEnumerable<AgentResponseUpdate> StreamMessage(
        this ChatMessage message,
        string? responseId = null,
        string? agentId = null)
    {
        responseId ??= Guid.NewGuid().ToString("N");
        string messageId = message.MessageId ?? Guid.NewGuid().ToString("N");

        return message.Contents.Select(content =>
            content.ToResponseUpdate(messageId, responseId, agentId, message.AuthorName));
    }

    /// <summary>
    /// Converts multiple <see cref="ChatMessage"/> instances into a flat stream of
    /// <see cref="AgentResponseUpdate"/> items.
    /// </summary>
    public static IEnumerable<AgentResponseUpdate> StreamMessages(
        this IEnumerable<ChatMessage> messages,
        string? agentId = null) =>
        messages.SelectMany(message => message.StreamMessage(agentId));

    /// <summary>
    /// Converts plain text strings into <see cref="ChatMessage"/> instances
    /// with word-level content streaming.
    /// </summary>
    public static IReadOnlyList<ChatMessage> ToChatMessages(
        this IEnumerable<string> messages,
        string? authorName = null) =>
        messages.Select(text => new ChatMessage(ChatRole.Assistant, text.ToContentStream().ToList())
        {
            AuthorName = authorName,
            MessageId = Guid.NewGuid().ToString("N"),
            RawRepresentation = text,
            CreatedAt = TimeProvider.System.GetUtcNow(),
        }).ToList();
}

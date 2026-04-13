// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     Guards group-chat tests: throws if it sees an Assistant message authored by a different agent.
///     Set <c>allowOtherAssistantRoles</c> to opt out and accept foreign authors.
/// </summary>
public sealed class FakeRoleCheckAgent(
    bool allowOtherAssistantRoles = false,
    string? id = null,
    string? name = null,
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
            if (!allowOtherAssistantRoles
                && message.Role == ChatRole.Assistant
                && message.AuthorName is not null
                && !string.Equals(message.AuthorName, Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Message from a foreign assistant author detected: AuthorName='{message.AuthorName}', expected '{Name ?? "<null>"}'.");
            }
        }

        yield return new AgentResponseUpdate(ChatRole.Assistant, "Ok")
        {
            AgentId = Id,
            AuthorName = Name,
            CreatedAt = Time.GetUtcNow(),
            MessageId = Guid.NewGuid().ToString("N"),
            ResponseId = Guid.NewGuid().ToString("N"),
        };

        await Task.Yield();
    }
}

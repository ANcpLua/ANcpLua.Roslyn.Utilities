// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/RoleCheckAgent.cs

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

// Guards group-chat tests: throws if an assistant message with a foreign
// AuthorName reaches this agent. Set allowOtherAssistantRoles=true to opt out.
internal sealed class RoleCheckAgent(bool allowOtherAssistantRoles, string? id = null, string? name = null) : AIAgent
{
    protected override string? IdCore => id;

    public override string? Name => name;

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => new(new RoleCheckAgentSession());

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new(new RoleCheckAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => default;

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        => this.RunStreamingAsync(messages, session, options, cancellationToken).ToAgentResponseAsync(cancellationToken);

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            if (!allowOtherAssistantRoles &&
                message.Role == ChatRole.Assistant &&
                message.AuthorName is not null &&
                message.AuthorName != this.Name)
            {
                throw new InvalidOperationException($"Message from other assistant role detected: AuthorName={message.AuthorName}");
            }
        }

        yield return new AgentResponseUpdate(ChatRole.Assistant, "Ok")
        {
            AgentId = this.Id,
            AuthorName = this.Name,
            MessageId = Guid.NewGuid().ToString("N"),
            ResponseId = Guid.NewGuid().ToString("N"),
        };
    }

    private sealed class RoleCheckAgentSession : AgentSession;
}

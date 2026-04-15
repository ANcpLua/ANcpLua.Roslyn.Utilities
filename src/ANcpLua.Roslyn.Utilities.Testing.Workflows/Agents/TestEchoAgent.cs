// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — TestEchoAgent.cs
//
// Stateful AIAgent test double with in-memory chat history and session serialization.
// Echoes user messages back as assistant messages with an optional prefix.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

/// <summary>
/// Stateful echo agent test double. Maintains an in-memory chat history and
/// supports session serialization. Echoes user messages back as assistant
/// messages with a configurable prefix. Pair with workflow tests that need
/// to verify history-dependent behavior.
/// </summary>
public class TestEchoAgent(string? id = null, string? name = null, string? prefix = null, TimeProvider? timeProvider = null) : AIAgent
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override string? IdCore => id;

    public override string? Name => name ?? base.Name;

    public InMemoryChatHistoryProvider ChatHistoryProvider { get; } = new();

    protected override async ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default) =>
        serializedState.Deserialize<EchoAgentSession>(jsonSerializerOptions) ?? await this.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        if (session is not EchoAgentSession typedSession)
        {
            throw new InvalidOperationException($"The provided session type '{session.GetType().Name}' is not compatible with this agent. Only sessions of type '{nameof(EchoAgentSession)}' can be serialized by this agent.");
        }

        return new(JsonSerializer.SerializeToElement(typedSession, jsonSerializerOptions));
    }

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        new(new EchoAgentSession());

    private ChatMessage UpdateSession(ChatMessage message, AgentSession? session = null)
    {
        this.ChatHistoryProvider.GetMessages(session).Add(message);
        return message;
    }

    private IEnumerable<ChatMessage> EchoMessages(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null)
    {
        foreach (ChatMessage message in messages)
        {
            this.UpdateSession(message, session);
        }

        IEnumerable<ChatMessage> echoMessages
            = from message in messages
              where message.Role == ChatRole.User &&
                    !string.IsNullOrEmpty(message.Text)
              select
                    this.UpdateSession(new ChatMessage(ChatRole.Assistant, $"{prefix}{message.Text}")
                    {
                        AuthorName = this.Name ?? this.Id,
                        CreatedAt = this._timeProvider.GetUtcNow(),
                        MessageId = Guid.NewGuid().ToString("N"),
                    }, session);

        return echoMessages.Concat(this.GetEpilogueMessages(options).Select(m => this.UpdateSession(m, session)));
    }

    protected virtual IEnumerable<ChatMessage> GetEpilogueMessages(AgentRunOptions? options = null) => [];

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        AgentResponse result =
            new(this.EchoMessages(messages, session, options).ToList())
            {
                AgentId = this.Id,
                CreatedAt = this._timeProvider.GetUtcNow(),
                ResponseId = Guid.NewGuid().ToString("N"),
            };

        return Task.FromResult(result);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string responseId = Guid.NewGuid().ToString("N");

        foreach (ChatMessage message in this.EchoMessages(messages, session, options).ToList())
        {
            yield return
                new(message.Role, message.Contents)
                {
                    AgentId = this.Id,
                    AuthorName = message.AuthorName,
                    ResponseId = responseId,
                    MessageId = message.MessageId,
                    CreatedAt = message.CreatedAt,
                };
        }

        await Task.Yield();
    }

    private sealed class EchoAgentSession : AgentSession
    {
        internal EchoAgentSession() { }

        [JsonConstructor]
        internal EchoAgentSession(AgentSessionStateBag stateBag) : base(stateBag) { }
    }
}

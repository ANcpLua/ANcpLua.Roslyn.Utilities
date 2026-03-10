// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Abstract base class for fake <see cref="AIAgent"/> implementations in tests.
/// Handles all session boilerplate — subclasses only override <see cref="StreamResponseAsync"/>.
/// </summary>
public abstract class FakeAgentBase : AIAgent
{
    /// <inheritdoc/>
    protected override string? IdCore => GetType().Name;

    /// <inheritdoc/>
    public override string Description => $"Fake agent: {GetType().Name}";

    /// <summary>
    /// Override this to produce the streaming response for the agent.
    /// This is the only method subclasses need to implement.
    /// </summary>
    protected abstract IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        new(new FakeSession());

    /// <inheritdoc/>
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        new(serializedState.Deserialize<FakeSession>(jsonSerializerOptions)!);

    /// <inheritdoc/>
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (session is not FakeSession fakeSession)
        {
            throw new InvalidOperationException(
                $"Session type '{session.GetType().Name}' is not compatible with {GetType().Name}. Expected FakeSession.");
        }

        return new(JsonSerializer.SerializeToElement(fakeSession, jsonSerializerOptions));
    }

    /// <inheritdoc/>
    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default) =>
        RunCoreStreamingAsync(messages, session, options, cancellationToken)
            .ToAgentResponseAsync(cancellationToken);

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (AgentResponseUpdate update in StreamResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <summary>
    /// Streams text chunks as <see cref="AgentResponseUpdate"/> instances sharing a single message ID.
    /// </summary>
    protected static async IAsyncEnumerable<AgentResponseUpdate> StreamChunksAsync(
        string[] chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string messageId = Guid.NewGuid().ToString("N");
        foreach (string chunk in chunks)
        {
            yield return new AgentResponseUpdate
            {
                MessageId = messageId,
                Role = ChatRole.Assistant,
                Contents = [new TextContent(chunk)],
            };

            await Task.Yield();
        }
    }

    private sealed class FakeSession : AgentSession
    {
        public FakeSession()
        {
        }

        [JsonConstructor]
        public FakeSession(AgentSessionStateBag stateBag) : base(stateBag)
        {
        }
    }
}

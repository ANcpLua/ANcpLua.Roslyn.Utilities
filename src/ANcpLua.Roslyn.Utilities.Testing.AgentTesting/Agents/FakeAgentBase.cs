// Licensed to the .NET Foundation under one or more agreements.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     Abstract base for fake <see cref="AIAgent" /> implementations used in tests.
///     Handles every piece of session, run, and run-streaming boilerplate so subclasses
///     only override <see cref="StreamResponseAsync" />.
/// </summary>
/// <remarks>
///     <para>
///         Time access is centralised on <see cref="TimeProvider" /> so tests can substitute a
///         <c>FakeTimeProvider</c> for deterministic <c>CreatedAt</c> stamps.
///     </para>
///     <para>
///         The session model is a single <see cref="FakeSession" /> type that round-trips through
///         <see cref="JsonSerializer" />. Subclasses that need richer state can override
///         <see cref="CreateSessionCoreAsync" /> and the serialise/deserialise pair.
///     </para>
/// </remarks>
public abstract class FakeAgentBase(string? id = null, string? name = null, TimeProvider? timeProvider = null) : AIAgent
{
    /// <summary>Time source for <c>CreatedAt</c> stamps. Defaults to <see cref="TimeProvider.System" />.</summary>
    protected TimeProvider Time { get; } = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    protected override string? IdCore => id ?? GetType().Name;

    /// <inheritdoc />
    public override string? Name => name;

    /// <inheritdoc />
    public override string Description => $"Fake agent: {GetType().Name}";

    /// <summary>
    ///     Produce the streaming response for one turn of the agent. The single hot spot
    ///     subclasses must implement.
    /// </summary>
    protected abstract IAsyncEnumerable<AgentResponseUpdate> StreamResponseAsync(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => new(new FakeSession());

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
        => new(serializedState.Deserialize<FakeSession>(jsonSerializerOptions) ?? new FakeSession());

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (session is not FakeSession fakeSession)
        {
            throw new InvalidOperationException(
                $"Session type '{session.GetType().Name}' is not compatible with {GetType().Name}. Expected {nameof(FakeSession)}.");
        }

        return new(JsonSerializer.SerializeToElement(fakeSession, jsonSerializerOptions));
    }

    /// <inheritdoc />
    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => RunCoreStreamingAsync(messages, session, options, cancellationToken).ToAgentResponseAsync(cancellationToken);

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in StreamResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <summary>
    ///     Stream a sequence of text chunks as <see cref="AgentResponseUpdate" /> instances that share one message id.
    /// </summary>
    protected static async IAsyncEnumerable<AgentResponseUpdate> StreamChunksAsync(
        IReadOnlyList<string> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messageId = Guid.NewGuid().ToString("N");
        foreach (var chunk in chunks)
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
        public FakeSession() { }

        [JsonConstructor]
        public FakeSession(AgentSessionStateBag stateBag) : base(stateBag) { }
    }
}

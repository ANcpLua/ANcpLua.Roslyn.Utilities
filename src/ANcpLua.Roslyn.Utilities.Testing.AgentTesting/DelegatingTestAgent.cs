// Licensed to the .NET Foundation under one or more agreements.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     A maximally flexible test double for <see cref="AIAgent" /> that delegates
///     all behavior through configurable <see cref="Func{TResult}" /> properties.
///     Every method throws <see cref="NotSupportedException" /> by default — configure
///     only the methods your test exercises.
/// </summary>
/// <remarks>
///     Unlike <see cref="FakeAgentBase" /> (which provides working session management
///     and requires subclassing), this agent lets you inject behavior via delegates
///     without creating a new type for each test scenario.
/// </remarks>
public sealed class DelegatingTestAgent : AIAgent
{
    /// <summary>
    ///     Delegate invoked for <see cref="AIAgent.Name" />. Returns base name if null.
    /// </summary>
    public Func<string?>? NameFunc { get; set; }

    /// <summary>
    ///     Delegate invoked for <see cref="AIAgent.Description" />. Returns base description if null.
    /// </summary>
    public Func<string?>? DescriptionFunc { get; set; }

    /// <summary>
    ///     Delegate invoked for session creation. Throws <see cref="NotSupportedException" /> by default.
    /// </summary>
    public Func<CancellationToken, ValueTask<AgentSession>> CreateSessionFunc { get; set; } =
        delegate { throw new NotSupportedException(); };

    /// <summary>
    ///     Delegate invoked for session deserialization. Throws <see cref="NotSupportedException" /> by default.
    /// </summary>
    public Func<JsonElement, JsonSerializerOptions?, AgentSession> DeserializeSessionFunc { get; set; } =
        delegate { throw new NotSupportedException(); };

    /// <summary>
    ///     Delegate invoked for non-streaming run. Throws <see cref="NotSupportedException" /> by default.
    /// </summary>
    public Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken, Task<AgentResponse>>
        RunAsyncFunc { get; set; } =
        delegate { throw new NotSupportedException(); };

    /// <summary>
    ///     Delegate invoked for streaming run. Throws <see cref="NotSupportedException" /> by default.
    /// </summary>
    public Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken,
        IAsyncEnumerable<AgentResponseUpdate>> RunStreamingAsyncFunc { get; set; } =
        delegate { throw new NotSupportedException(); };

    /// <summary>
    ///     Delegate invoked for <see cref="AIAgent.GetService" />. Falls back to base if null.
    /// </summary>
    public Func<Type, object?, object?>? GetServiceFunc { get; set; }

    /// <inheritdoc />
    public override string? Name => NameFunc?.Invoke() ?? base.Name;

    /// <inheritdoc />
    public override string? Description => DescriptionFunc?.Invoke() ?? base.Description;

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        return CreateSessionFunc(cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<AgentSession>(DeserializeSessionFunc(serializedState, jsonSerializerOptions));
    }

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsyncFunc(messages, session, options, cancellationToken);
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return RunStreamingAsyncFunc(messages, session, options, cancellationToken);
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        return GetServiceFunc is { } func
            ? func(serviceType, serviceKey)
            : base.GetService(serviceType, serviceKey);
    }
}
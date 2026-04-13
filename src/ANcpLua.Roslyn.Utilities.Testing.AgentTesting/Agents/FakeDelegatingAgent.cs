// Licensed to the .NET Foundation under one or more agreements.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Agents;

/// <summary>
///     A maximally flexible <see cref="AIAgent" /> test double: every override is a settable
///     <see cref="Func{TResult}" /> property that defaults to throwing <see cref="NotSupportedException" />.
///     Configure only the methods your test exercises.
/// </summary>
/// <remarks>
///     Choose <see cref="FakeDelegatingAgent" /> over <see cref="FakeAgentBase" /> when you want to
///     inject behaviour without subclassing — it's the right tool for one-off scenarios where the
///     ceremony of a derived class would obscure the test.
/// </remarks>
public sealed class FakeDelegatingAgent : AIAgent
{
    /// <summary>Delegate invoked for <see cref="AIAgent.Name" />. Falls back to base name if null.</summary>
    public Func<string?>? NameFunc { get; set; }

    /// <summary>Delegate invoked for <see cref="AIAgent.Description" />. Falls back to base description if null.</summary>
    public Func<string?>? DescriptionFunc { get; set; }

    /// <summary>Delegate invoked for session creation. Throws by default.</summary>
    public Func<CancellationToken, ValueTask<AgentSession>> CreateSessionFunc { get; set; } =
        static _ => throw new NotSupportedException($"{nameof(CreateSessionFunc)} not configured");

    /// <summary>Delegate invoked for session deserialisation. Throws by default.</summary>
    public Func<JsonElement, JsonSerializerOptions?, AgentSession> DeserializeSessionFunc { get; set; } =
        static (_, _) => throw new NotSupportedException($"{nameof(DeserializeSessionFunc)} not configured");

    /// <summary>Delegate invoked for non-streaming run. Throws by default.</summary>
    public Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken, Task<AgentResponse>> RunAsyncFunc { get; set; } =
        static (_, _, _, _) => throw new NotSupportedException($"{nameof(RunAsyncFunc)} not configured");

    /// <summary>Delegate invoked for streaming run. Throws by default.</summary>
    public Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken, IAsyncEnumerable<AgentResponseUpdate>> RunStreamingAsyncFunc { get; set; } =
        static (_, _, _, _) => throw new NotSupportedException($"{nameof(RunStreamingAsyncFunc)} not configured");

    /// <summary>Delegate invoked for <see cref="AIAgent.GetService" />. Falls back to base if null.</summary>
    public Func<Type, object?, object?>? GetServiceFunc { get; set; }

    /// <inheritdoc />
    public override string? Name => NameFunc?.Invoke() ?? base.Name;

    /// <inheritdoc />
    public override string? Description => DescriptionFunc?.Invoke() ?? base.Description;

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => CreateSessionFunc(cancellationToken);

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
        => new(DeserializeSessionFunc(serializedState, jsonSerializerOptions));

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{nameof(FakeDelegatingAgent)} does not implement session serialization.");

    /// <inheritdoc />
    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => RunAsyncFunc(messages, session, options, cancellationToken);

    /// <inheritdoc />
    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => RunStreamingAsyncFunc(messages, session, options, cancellationToken);

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
        => GetServiceFunc?.Invoke(serviceType, serviceKey) ?? base.GetService(serviceType, serviceKey);
}

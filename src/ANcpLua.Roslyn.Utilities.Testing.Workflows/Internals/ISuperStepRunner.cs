// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/ISuperStepRunner.cs
// TRIMMED: ConcurrentEventSink replaced with IAsyncEnumerable<WorkflowEvent> to avoid deep dep.

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal interface ISuperStepRunner
{
    string SessionId { get; }

    string StartExecutorId { get; }

    WorkflowTelemetryContext TelemetryContext { get; }

    bool HasUnservicedRequests { get; }
    bool HasUnprocessedMessages { get; }

    ValueTask EnqueueResponseAsync(ExternalResponse response, CancellationToken cancellationToken = default);
    bool TryGetResponsePortExecutorId(string portId, out string? executorId);

    ValueTask<bool> IsValidInputTypeAsync<T>(CancellationToken cancellationToken = default);
    ValueTask<bool> EnqueueMessageAsync<T>(T message, CancellationToken cancellationToken = default);
    ValueTask<bool> EnqueueMessageUntypedAsync(object message, Type declaredType, CancellationToken cancellationToken = default);

    IAsyncEnumerable<WorkflowEvent> OutgoingEvents { get; }

    ValueTask RepublishPendingEventsAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> RunSuperStepAsync(CancellationToken cancellationToken);

    ValueTask RequestEndRunAsync();
}

// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using Microsoft.Agents.AI.Workflows.Execution;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Runtime;

/// <summary>
/// Per-executor IWorkflowContext backed by StateManager + concurrent queues.
/// Use TestRunState.ContextFor(executorId) to allocate one bound to a shared run state.
/// </summary>
internal sealed class TestWorkflowContext(string executorId, TestRunState? state = null, bool concurrentRunsEnabled = false) : IWorkflowContext
{
    private readonly TestRunState _state = state ?? new TestRunState();

    public bool ConcurrentRunsEnabled { get; } = concurrentRunsEnabled;

    public ConcurrentQueue<object> SentMessages => this._state.SentMessages.GetOrAdd(executorId, _ => new());

    public StateManager StateManager => this._state.StateManager;

    public ConcurrentQueue<WorkflowEvent> EmittedEvents => this._state.EmittedEvents;

    public ConcurrentQueue<object> YieldedOutputs => this._state.YieldedOutputs.GetOrAdd(executorId, _ => new());

    public ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        this.EmittedEvents.Enqueue(workflowEvent);
        return default;
    }

    public ValueTask YieldOutputAsync(object output, CancellationToken cancellationToken = default)
    {
        this.YieldedOutputs.Enqueue(output);

        return output switch
        {
            AgentResponseUpdate update => this.AddEventAsync(new AgentResponseUpdateEvent(executorId, update), cancellationToken),
            AgentResponse response => this.AddEventAsync(new AgentResponseEvent(executorId, response), cancellationToken),
            _ => this.AddEventAsync(new WorkflowOutputEvent(output, executorId), cancellationToken),
        };
    }

    public ValueTask RequestHaltAsync()
    {
        this._state.IncrementHaltRequests();
        return default;
    }

    public ValueTask QueueClearScopeAsync(string? scopeName = null, CancellationToken cancellationToken = default)
        => this.StateManager.ClearStateAsync(new ScopeId(executorId, scopeName));

    public ValueTask QueueStateUpdateAsync<T>(string key, T? value, string? scopeName = null, CancellationToken cancellationToken = default)
        => this.StateManager.WriteStateAsync(new ScopeId(executorId, scopeName), key, value);

    public ValueTask<T?> ReadStateAsync<T>(string key, string? scopeName = null, CancellationToken cancellationToken = default)
        => this.StateManager.ReadStateAsync<T>(new ScopeId(executorId, scopeName), key);

    public ValueTask<T> ReadOrInitStateAsync<T>(string key, Func<T> initialStateFactory, string? scopeName = null, CancellationToken cancellationToken = default)
        => this.StateManager.ReadOrInitStateAsync(new ScopeId(executorId, scopeName), key, initialStateFactory);

    public ValueTask<HashSet<string>> ReadStateKeysAsync(string? scopeName = null, CancellationToken cancellationToken = default)
        => this.StateManager.ReadKeysAsync(new ScopeId(executorId, scopeName));

    public ValueTask SendMessageAsync(object message, string? targetId = null, CancellationToken cancellationToken = default)
    {
        this.SentMessages.Enqueue(message);
        return default;
    }

    public IReadOnlyDictionary<string, string>? TraceContext => null;
}

// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Agents.AI.Workflows.Execution;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Runtime;

/// <summary>Concurrent state shared across TestWorkflowContext instances during a multi-executor unit test.</summary>
internal sealed class TestRunState
{
    public ConcurrentDictionary<string, ConcurrentQueue<object>> SentMessages = new();
    public StateManager StateManager { get; } = new();
    public ConcurrentQueue<WorkflowEvent> EmittedEvents { get; } = new();
    public ConcurrentDictionary<string, ConcurrentQueue<object>> YieldedOutputs { get; } = new();

    private int _haltRequests;
    public int HaltRequests => Volatile.Read(ref this._haltRequests);

    public void IncrementHaltRequests() => Interlocked.Increment(ref this._haltRequests);

    public TestWorkflowContext ContextFor(string executorId) => new(executorId, this);
}

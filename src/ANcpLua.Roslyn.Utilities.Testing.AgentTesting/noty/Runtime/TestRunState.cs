// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestRunState.cs

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Execution;

namespace Noty.Workflows.Tests;

/// <summary>
/// Shared state bag that lives between one or more <see cref="TestWorkflowContext"/>
/// instances inside a single test. Use <see cref="ContextFor"/> to materialize a
/// per-executor context that shares state with its siblings.
/// </summary>
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

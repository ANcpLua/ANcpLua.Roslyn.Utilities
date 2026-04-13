// Copyright (c) Microsoft. All rights reserved.
//
// The single import. Derive WorkflowFixture<TInput>, override BuildWorkflow(),
// and `this.RunAsync(input)` returns a WorkflowRunResult you assert with Should().
// Handles execution environments, checkpoint/resume, and typed event projection.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.InProc;
using Xunit;

namespace Noty.Workflows.Tests;

public abstract class WorkflowFixture<TInput>(ITestOutputHelper output) : IDisposable
    where TInput : notnull
{
    private readonly CancellationTokenSource _cts = new(TimeSpan.FromSeconds(60));
    private CheckpointManager? _checkpointManager;
    private CheckpointInfo? _lastCheckpoint;

    protected ITestOutputHelper Output { get; } = output;

    /// <summary>Builds the workflow under test. Called once per run.</summary>
    protected abstract Workflow BuildWorkflow();

    /// <summary>Runs the workflow to quiescence under the chosen execution environment.</summary>
    protected Task<WorkflowRunResult> RunAsync(
        TInput input,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcess_Lockstep)
        => this.RunCoreAsync(input, environment, useCheckpointing: false);

    /// <summary>Runs the workflow with in-memory checkpointing. The last checkpoint is stored for a subsequent <see cref="ResumeAsync"/>.</summary>
    protected Task<WorkflowRunResult> RunWithCheckpointingAsync(
        TInput input,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcess_Lockstep)
        => this.RunCoreAsync(input, environment, useCheckpointing: true);

    /// <summary>Resumes the most recent run from its last checkpoint, optionally pumping an external response.</summary>
    protected async Task<WorkflowRunResult> ResumeAsync(
        ExternalResponse? response = null,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcess_Lockstep)
    {
        if (this._lastCheckpoint is null || this._checkpointManager is null)
        {
            throw new InvalidOperationException("Call RunWithCheckpointingAsync before ResumeAsync.");
        }

        var env = environment.ToWorkflowExecutionEnvironment().WithCheckpointing(this._checkpointManager);
        await using StreamingRun run = await env.ResumeStreamingAsync(this.BuildWorkflow(), this._lastCheckpoint, this._cts.Token);

        if (response is not null)
        {
            await run.SendResponseAsync(response);
        }

        var events = await CollectAsync(run, this._cts.Token);
        this._lastCheckpoint = LatestCheckpoint(events) ?? this._lastCheckpoint;
        return new WorkflowRunResult(events, this._lastCheckpoint);
    }

    /// <summary>Answers the next pending external request with <paramref name="data"/> and resumes.</summary>
    protected Task<WorkflowRunResult> AnswerRequestAsync(
        WorkflowRunResult pending,
        object data,
        ExecutionEnvironment environment = ExecutionEnvironment.InProcess_Lockstep)
    {
        var request = pending.PendingRequests.FirstOrDefault()
            ?? throw new InvalidOperationException("No pending RequestInfoEvent to answer.");
        return this.ResumeAsync(request.CreateResponse(data), environment);
    }

    public void Dispose()
    {
        this._cts.Cancel();
        this._cts.Dispose();
    }

    private async Task<WorkflowRunResult> RunCoreAsync(TInput input, ExecutionEnvironment environment, bool useCheckpointing)
    {
        var env = environment.ToWorkflowExecutionEnvironment();
        if (useCheckpointing)
        {
            this._checkpointManager ??= CheckpointManager.CreateInMemory();
            env = env.WithCheckpointing(this._checkpointManager);
        }

        await using StreamingRun run = await env.RunStreamingAsync(this.BuildWorkflow(), input);
        var events = await CollectAsync(run, this._cts.Token);
        this._lastCheckpoint = LatestCheckpoint(events);
        return new WorkflowRunResult(events, this._lastCheckpoint);
    }

    private static async Task<IReadOnlyList<WorkflowEvent>> CollectAsync(StreamingRun run, CancellationToken cancellationToken)
    {
        List<WorkflowEvent> collected = [];
        await foreach (var evt in run.WatchStreamAsync(blockOnPendingRequest: false, cancellationToken))
        {
            collected.Add(evt);
        }
        return collected;
    }

    private static CheckpointInfo? LatestCheckpoint(IReadOnlyList<WorkflowEvent> events)
        => events.OfType<SuperStepCompletedEvent>()
                 .LastOrDefault()?.CompletionInfo?.Checkpoint;
}

/// <summary>
/// Typed projection of a workflow run. Materialized once; every property is a
/// frozen <see cref="IReadOnlyList{T}"/>. Call <see cref="Should"/> for fluent
/// assertions.
/// </summary>
public sealed record WorkflowRunResult(IReadOnlyList<WorkflowEvent> Events, CheckpointInfo? LastCheckpoint)
{
    public IReadOnlyList<WorkflowOutputEvent> Outputs { get; } = [.. Events.OfType<WorkflowOutputEvent>()];

    public IReadOnlyList<ExecutorCompletedEvent> CompletedExecutors { get; } = [.. Events.OfType<ExecutorCompletedEvent>()];

    public IReadOnlyList<SuperStepCompletedEvent> SuperSteps { get; } = [.. Events.OfType<SuperStepCompletedEvent>()];

    public IReadOnlyList<WorkflowErrorEvent> Errors { get; } = [.. Events.OfType<WorkflowErrorEvent>()];

    public IReadOnlyList<ExternalRequest> PendingRequests { get; }
        = [.. Events.OfType<RequestInfoEvent>().Select(e => e.Request)];

    public WorkflowRunAssertions Should() => new(this);
}

/// <summary>
/// Fluent assertions. Every method returns `this` so chains read like specs:
///   run.Should().YieldOutput&lt;string&gt;(...).And.HaveNoErrors().And.CompletedExecutors("A","B");
/// </summary>
public readonly struct WorkflowRunAssertions(WorkflowRunResult result)
{
    public WorkflowRunAssertions And => this;

    public WorkflowRunAssertions YieldOutput<TOutput>(Action<TOutput>? assert = null)
    {
        var matches = result.Outputs.Where(e => e.Data is TOutput).Select(e => (TOutput)e.Data!).ToArray();
        matches.Should().NotBeEmpty($"expected at least one WorkflowOutputEvent carrying {typeof(TOutput).Name}");
        if (assert is not null)
        {
            foreach (var value in matches) { assert(value); }
        }
        return this;
    }

    public WorkflowRunAssertions Emit<TEvent>(int? count = null) where TEvent : WorkflowEvent
    {
        int actual = result.Events.OfType<TEvent>().Count();
        if (count is int expected)
        {
            actual.Should().Be(expected);
        }
        else
        {
            actual.Should().BeGreaterThan(0, $"expected at least one {typeof(TEvent).Name}");
        }
        return this;
    }

    public WorkflowRunAssertions NotEmit<TEvent>() where TEvent : WorkflowEvent
    {
        result.Events.OfType<TEvent>().Should().BeEmpty($"expected zero {typeof(TEvent).Name}");
        return this;
    }

    public WorkflowRunAssertions CompletedExecutors(params string[] ids)
    {
        result.CompletedExecutors.Select(e => e.ExecutorId).Should().BeEquivalentTo(ids);
        return this;
    }

    public WorkflowRunAssertions HaveNoErrors()
    {
        result.Errors.Should().BeEmpty("workflow emitted error events");
        return this;
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — WorkflowHarness.cs
//
// Modifications from upstream:
// - GenerateCodeAsync stripped (requires Shared.Code.Compiler, a Microsoft-internal test helper).
// - Console.Out routing removed — harness does not touch Console. Route via TestOutputAdapter at the caller.
// - ConfigureAwait(false) on all await calls. Null check replaces Assert.NotNull.

using System.Globalization;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.Declarative.Events;
using Xunit;
using Xunit.Sdk;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Framework;

/// <summary>
/// Drives a <see cref="Workflow"/> through <see cref="InProcessExecution"/> and monitors its event stream.
/// Supports both in-memory and JSON file-system checkpointing via the same API.
/// Use <see cref="RunTestcaseAsync{TInput}"/> for JSON-driven testcase replay, or
/// <see cref="RunWorkflowAsync{TInput}"/> + <see cref="ResumeAsync"/> for manual control.
/// </summary>
public sealed class WorkflowHarness(Workflow workflow, string runId, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private CheckpointManager? _checkpointManager;
    private CheckpointInfo? _lastCheckpoint;

    /// <summary>
    /// Runs a full testcase: starts the workflow, then pumps responses from <paramref name="testcase"/>
    /// each time the workflow pauses for external input, until no new requests remain.
    /// </summary>
    public async Task<WorkflowEvents> RunTestcaseAsync<TInput>(Testcase testcase, TInput input, bool useJson = false) where TInput : notnull
    {
        WorkflowEvents workflowEvents = await this.RunWorkflowAsync(input, useJson).ConfigureAwait(false);
        int requestCount = workflowEvents.InputEvents.Count;
        int responseCount = 0;
        while (requestCount > responseCount)
        {
            ExternalRequest request = workflowEvents.InputEvents[^1].Request;
            Assert.NotNull(testcase.Setup.Responses);
            Assert.NotEmpty(testcase.Setup.Responses);
            string inputText = testcase.Setup.Responses[responseCount].Value;
            ++responseCount;
            ExternalResponse response = request.CreateResponse(new ExternalInputResponse(new ChatMessage(ChatRole.User, inputText)));
            WorkflowEvents runEvents = await this.ResumeAsync(response).ConfigureAwait(false);
            workflowEvents = new WorkflowEvents([.. workflowEvents.Events, .. runEvents.Events]);
            requestCount = workflowEvents.InputEvents.Count;
        }

        return workflowEvents;
    }

    /// <summary>
    /// Starts a workflow run and monitors the event stream until suspension or completion.
    /// </summary>
    public async Task<WorkflowEvents> RunWorkflowAsync<TInput>(TInput input, bool useJson = false) where TInput : notnull
    {
        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input, this.GetCheckpointManager(useJson), runId).ConfigureAwait(false);
        IReadOnlyList<WorkflowEvent> workflowEvents = await MonitorAndDisposeWorkflowRunAsync(run).ToArrayAsync().ConfigureAwait(false);
        this._lastCheckpoint = workflowEvents.OfType<SuperStepCompletedEvent>().LastOrDefault()?.CompletionInfo?.Checkpoint;
        return new WorkflowEvents(workflowEvents);
    }

    /// <summary>
    /// Resumes the workflow from the last checkpoint with the given external response.
    /// </summary>
    public async Task<WorkflowEvents> ResumeAsync(ExternalResponse response)
    {
        if (this._lastCheckpoint is null)
        {
            throw new InvalidOperationException("Call RunWorkflowAsync before ResumeAsync to establish a checkpoint.");
        }

        StreamingRun run = await InProcessExecution.ResumeStreamingAsync(workflow, this._lastCheckpoint, this.GetCheckpointManager()).ConfigureAwait(false);
        IReadOnlyList<WorkflowEvent> workflowEvents = await MonitorAndDisposeWorkflowRunAsync(run, response).ToArrayAsync().ConfigureAwait(false);
        this._lastCheckpoint = workflowEvents.OfType<SuperStepCompletedEvent>().LastOrDefault()?.CompletionInfo?.Checkpoint;
        return new WorkflowEvents(workflowEvents);
    }

    private CheckpointManager GetCheckpointManager(bool useJson = false)
    {
        if (useJson && this._checkpointManager is null)
        {
            string stamp = this._timeProvider.GetUtcNow().ToString("yyMMdd-HHmmss-ff", CultureInfo.InvariantCulture);
            DirectoryInfo checkpointFolder = Directory.CreateDirectory(Path.Combine(".", $"chk-{stamp}"));
            this._checkpointManager = CheckpointManager.CreateJson(new FileSystemJsonCheckpointStore(checkpointFolder));
        }
        else
        {
            this._checkpointManager ??= CheckpointManager.CreateInMemory();
        }

        return this._checkpointManager;
    }

    private static async IAsyncEnumerable<WorkflowEvent> MonitorAndDisposeWorkflowRunAsync(StreamingRun run, ExternalResponse? response = null)
    {
        await using IAsyncDisposable disposeRun = run;

        if (response is not null)
        {
            await run.SendResponseAsync(response).ConfigureAwait(false);
        }

        bool exitLoop = false;
        bool hasRequest = false;

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync().ConfigureAwait(false))
        {
            switch (workflowEvent)
            {
                case SuperStepCompletedEvent:
                    if (hasRequest)
                    {
                        exitLoop = true;
                    }
                    break;
                case RequestInfoEvent requestInfo:
                    if (response is null || requestInfo.Request.RequestId != response.RequestId)
                    {
                        hasRequest = true;
                    }
                    else
                    {
                        continue;
                    }
                    break;
                case WorkflowErrorEvent errorEvent:
                    throw errorEvent.Data as Exception ?? new XunitException("Unexpected failure...");
            }

            yield return workflowEvent;

            if (exitLoop)
            {
                break;
            }
        }
    }
}

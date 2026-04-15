// Checkpoint/resume: run to mid-workflow, rewind to an earlier checkpoint,
// verify pending RequestInfoEvents are re-emitted.
// Source: Sample/05_Simple_Workflow_Checkpointing.cs + CheckpointResumeTests.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.InProc;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

internal static class CheckpointingSample
{
    public static async ValueTask<string> RunAsync(
        TextWriter writer,
        Func<string, int> userGuessCallback,
        InProcessExecutionEnvironment environment,
        bool rehydrateToRestore = false,
        CheckpointManager? checkpointManager = null)
    {
        Dictionary<CheckpointInfo, (NumberSignal Signal, string? Prompt)> checkpointedOutputs = [];

        var signal = NumberSignal.Init;
        string? prompt = ExternalRequestSample.UpdatePrompt(null, signal);

        checkpointManager ??= CheckpointManager.Default;
        Workflow workflow = ExternalRequestSample.Build();

        StreamingRun handle = await environment
            .WithCheckpointing(checkpointManager)
            .RunStreamingAsync(workflow, NumberSignal.Init);

        List<CheckpointInfo> checkpoints = [];
        CancellationTokenSource cancellationSource = new();

        string? result = await RunStreamToHaltOrMaxStepAsync(maxStep: 6);
        result.Should().BeNull();
        checkpoints.Should().HaveCount(6);

        var targetCheckpoint = checkpoints[2];

        if (rehydrateToRestore)
        {
            await handle.DisposeAsync();
            handle = await environment.WithCheckpointing(checkpointManager)
                                      .ResumeStreamingAsync(workflow, targetCheckpoint, CancellationToken.None);
        }
        else
        {
            await handle.RestoreCheckpointAsync(targetCheckpoint, CancellationToken.None);
        }

        (signal, prompt) = checkpointedOutputs[targetCheckpoint];

        cancellationSource.Dispose();
        cancellationSource = new();

        checkpoints.Clear();
        result = await RunStreamToHaltOrMaxStepAsync();

        result.Should().NotBeNull();
        checkpoints.Should().HaveCountGreaterThanOrEqualTo(6).And.HaveCountLessThanOrEqualTo(7);

        cancellationSource.Dispose();
        return result!;

        async ValueTask<string?> RunStreamToHaltOrMaxStepAsync(int? maxStep = null)
        {
            List<ExternalRequest> requests = [];

            await foreach (var evt in handle.WatchStreamAsync(cancellationSource.Token))
            {
                switch (evt)
                {
                    case WorkflowOutputEvent outputEvent when outputEvent.ExecutorId == ExternalRequestSample.JudgeId:
                        if (outputEvent.Is(out NumberSignal newSignal))
                        {
                            prompt = ExternalRequestSample.UpdatePrompt(prompt, signal = newSignal);
                        }
                        else if (!outputEvent.Is<TryCount>())
                        {
                            throw new InvalidOperationException($"Unexpected output type {outputEvent.Data!.GetType()}");
                        }
                        break;

                    case RequestInfoEvent requestInputEvt:
                        requests.Add(requestInputEvt.Request);
                        break;

                    case SuperStepCompletedEvent stepCompletedEvt:
                        var checkpoint = stepCompletedEvt.CompletionInfo?.Checkpoint;
                        if (checkpoint is not null)
                        {
                            checkpoints.Add(checkpoint);
                            checkpointedOutputs[checkpoint] = (signal, prompt);
                        }

                        if (maxStep.HasValue && stepCompletedEvt.StepNumber >= maxStep.Value - 1)
                        {
                            cancellationSource.Cancel();
                            return null;
                        }

                        foreach (var request in requests)
                        {
                            await handle.SendResponseAsync(ExecuteExternalRequest(request, userGuessCallback, prompt));
                        }
                        requests.Clear();
                        break;
                }
            }

            if (cancellationSource.IsCancellationRequested)
            {
                return null;
            }

            writer.WriteLine($"Result: {prompt}");
            return prompt!;
        }
    }

    private static ExternalResponse ExecuteExternalRequest(ExternalRequest request, Func<string, int> userGuessCallback, string? runningState)
    {
        object result = request.PortInfo.PortId switch
        {
            "GuessNumber" => userGuessCallback(runningState ?? "Guess the number."),
            _ => throw new NotSupportedException($"Request {request.PortInfo.PortId} is not supported"),
        };
        return request.CreateResponse(result);
    }
}

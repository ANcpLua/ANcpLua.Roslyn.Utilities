// External request port: judge asks host for a number through a RequestPort.
// Source: Sample/04_Simple_Workflow_ExternalRequest.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

internal static class ExternalRequestSample
{
    public const string JudgeId = "Judge";

    public static Workflow Build(int target = 42)
    {
        RequestPort guessNumber = RequestPort.Create<NumberSignal, int>("GuessNumber");
        JudgeExecutor judge = new(JudgeId, target);

        return new WorkflowBuilder(guessNumber)
            .AddEdge(guessNumber, judge)
            .AddEdge(judge, guessNumber, (NumberSignal signal) => signal != NumberSignal.Matched)
            .WithOutputFrom(judge)
            .Build();
    }

    public static async ValueTask<string> RunAsync(
        TextWriter writer,
        Func<string, int> userGuessCallback,
        IWorkflowExecutionEnvironment environment)
    {
        var signal = NumberSignal.Init;
        string? prompt = UpdatePrompt(null, signal);

        StreamingRun handle = await environment.RunStreamingAsync(Build(), NumberSignal.Init);
        List<ExternalRequest> requests = [];

        await foreach (var evt in handle.WatchStreamAsync())
        {
            switch (evt)
            {
                case WorkflowOutputEvent outputEvent when outputEvent.ExecutorId == JudgeId:
                    if (outputEvent.Is(out NumberSignal newSignal))
                    {
                        prompt = UpdatePrompt(prompt, signal = newSignal);
                    }
                    else if (!outputEvent.Is<TryCount>())
                    {
                        throw new InvalidOperationException($"Unexpected output type {outputEvent.Data!.GetType()}");
                    }
                    break;

                case RequestInfoEvent requestInputEvt:
                    requests.Add(requestInputEvt.Request);
                    break;

                case SuperStepCompletedEvent:
                    foreach (var request in requests)
                    {
                        var response = ExecuteExternalRequest(request, userGuessCallback, prompt);
                        await handle.SendResponseAsync(response);
                    }
                    requests.Clear();
                    break;

                case ExecutorCompletedEvent completed:
                    writer.WriteLine($"'{completed.ExecutorId}: {completed.Data}");
                    break;
            }
        }

        writer.WriteLine($"Result: {prompt}");
        return prompt!;
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

    internal static string? UpdatePrompt(string? runningResult, NumberSignal signal)
        => signal switch
        {
            NumberSignal.Matched => "You guessed correctly! You Win!",
            NumberSignal.Above => "Your guess was too high. Try again.",
            NumberSignal.Below => "Your guess was too low. Try again.",
            _ => runningResult,
        };
}

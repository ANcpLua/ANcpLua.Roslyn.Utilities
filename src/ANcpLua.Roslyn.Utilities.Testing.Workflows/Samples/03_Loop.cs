// Stateful loop: guess-the-number with ReadStateAsync / QueueStateUpdateAsync.
// Source: Sample/03_Simple_Workflow_Loop.cs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

internal enum NumberSignal { Init, Above, Below, Matched }

internal sealed record TryCount(int Tries);

internal sealed record NumberBounds(int LowerBound, int UpperBound)
{
    public int CurrGuess => (this.LowerBound + this.UpperBound) / 2;

    public NumberBounds ForAboveHint() => this with { UpperBound = this.CurrGuess - 1 };

    public NumberBounds ForBelowHint() => this with { LowerBound = this.CurrGuess + 1 };
}

internal static class LoopSample
{
    public static Workflow Build(int target = 42)
    {
        GuessNumberExecutor guess = new("GuessNumber", 1, 100);
        JudgeExecutor judge = new("Judge", target);

        return new WorkflowBuilder(guess)
            .AddEdge(guess, judge)
            .AddEdge(judge, guess)
            .WithOutputFrom(guess)
            .Build();
    }

    public static async ValueTask<string> RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment)
    {
        StreamingRun run = await environment.RunStreamingAsync(Build(), NumberSignal.Init);
        await foreach (var evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent outputEvt)
            {
                string result = outputEvt.As<string>()!;
                writer.WriteLine($"Result: {result}");
                return result;
            }
        }

        throw new InvalidOperationException("Workflow failed to yield an output.");
    }
}

[YieldsOutput(typeof(string))]
internal sealed partial class GuessNumberExecutor : Executor
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) => protocolBuilder;
    private readonly int _initialLowerBound;
    private readonly int _initialUpperBound;

    public GuessNumberExecutor(string id, int lowerBound, int upperBound)
        : base(id, default(ExecutorOptions), declareCrossRunShareable: true)
    {
        if (lowerBound >= upperBound)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerBound), "Lower bound must be less than upper bound.");
        }

        this._initialLowerBound = lowerBound;
        this._initialUpperBound = upperBound;
    }

    [MessageHandler]
    public async ValueTask<int> HandleAsync(NumberSignal message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var bounds = await context.ReadStateAsync<NumberBounds>(nameof(NumberBounds), cancellationToken: cancellationToken)
                     ?? new NumberBounds(this._initialLowerBound, this._initialUpperBound);

        switch (message)
        {
            case NumberSignal.Matched:
                await context.YieldOutputAsync($"Guessed the number: {bounds.CurrGuess}", cancellationToken);
                break;
            case NumberSignal.Above:
                bounds = bounds.ForAboveHint();
                break;
            case NumberSignal.Below:
                bounds = bounds.ForBelowHint();
                break;
        }

        await context.QueueStateUpdateAsync(nameof(NumberBounds), bounds, cancellationToken: cancellationToken);
        return bounds.CurrGuess;
    }
}

[YieldsOutput(typeof(TryCount))]
internal sealed partial class JudgeExecutor(string id, int targetNumber) : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) => protocolBuilder;
    [MessageHandler]
    public async ValueTask<NumberSignal> HandleAsync(int message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        int tries = await context.ReadStateAsync<int>("TryCount", cancellationToken: cancellationToken) + 1;
        await context.YieldOutputAsync(new TryCount(tries), cancellationToken);

        return message == targetNumber ? NumberSignal.Matched
             : message < targetNumber ? NumberSignal.Below
             : NumberSignal.Above;
    }
}

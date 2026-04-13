// Sequential workflow: Uppercase -> Reverse -> output.
// Source: Sample/01_Simple_Workflow_Sequential.cs

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace Noty.Workflows.Tests.Samples;

internal static class SequentialSample
{
    public static Workflow Build()
    {
        UppercaseExecutor uppercase = new();
        ReverseTextExecutor reverse = new();

        return new WorkflowBuilder(uppercase)
            .AddEdge(uppercase, reverse)
            .WithOutputFrom(reverse)
            .Build();
    }

    public static async ValueTask RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment)
    {
        StreamingRun run = await environment.RunStreamingAsync(Build(), input: "Hello, World!");

        await foreach (var evt in run.WatchStreamAsync())
        {
            if (evt is ExecutorCompletedEvent completed)
            {
                writer.WriteLine($"{completed.ExecutorId}: {completed.Data}");
            }
        }
    }
}

internal sealed class UppercaseExecutor() : Executor<string, string>(nameof(UppercaseExecutor), declareCrossRunShareable: true)
{
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        => new(message.ToUpperInvariant());
}

internal sealed partial class ReverseTextExecutor() : Executor(nameof(ReverseTextExecutor), declareCrossRunShareable: true)
{
    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string result = string.Concat(message.Reverse());
        await context.YieldOutputAsync(result, cancellationToken);
        return result;
    }
}

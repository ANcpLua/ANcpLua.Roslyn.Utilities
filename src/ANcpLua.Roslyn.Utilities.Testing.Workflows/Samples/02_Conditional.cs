// Conditional fan-out: DetectSpam routes to Respond or Remove by predicate.
// Source: Sample/02_Simple_Workflow_Condition.cs

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

internal static class ConditionalSample
{
    public static Workflow Build(params string[] spamKeywords)
    {
        DetectSpamExecutor detect = new("DetectSpam", spamKeywords.Length == 0 ? ["spam", "advertisement", "offer"] : spamKeywords);
        RespondToMessageExecutor respond = new("RespondToMessage");
        RemoveSpamExecutor remove = new("RemoveSpam");

        return new WorkflowBuilder(detect)
            .AddEdge(detect, respond, (bool isSpam) => !isSpam)
            .AddEdge(detect, remove, (bool isSpam) => isSpam)
            .WithOutputFrom(respond, remove)
            .Build();
    }

    public static async ValueTask<string> RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment, string input)
    {
        StreamingRun handle = await environment.RunStreamingAsync(Build(), input: input);

        await foreach (var evt in handle.WatchStreamAsync())
        {
            switch (evt)
            {
                case WorkflowOutputEvent outputEvt:
                    string result = outputEvt.As<string>()!;
                    writer.WriteLine($"Result: {result}");
                    return result;

                case ExecutorCompletedEvent completed:
                    writer.WriteLine($"{completed.ExecutorId}: {completed.Data}");
                    break;

                case WorkflowErrorEvent errorEvent:
                    Assert.Fail($"Workflow failed with error: {errorEvent.Exception}");
                    break;
            }
        }

        throw new InvalidOperationException("Workflow failed to yield an output.");
    }
}

internal sealed partial class DetectSpamExecutor(string id, string[] spamKeywords) : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) => protocolBuilder;
    [MessageHandler]
    public ValueTask<bool> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        => new(spamKeywords.Any(keyword => message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
}

internal sealed partial class RespondToMessageExecutor(string id) : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) => protocolBuilder;
    public const string ActionResult = "Message processed successfully.";

    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask HandleAsync(bool message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (message)
        {
            throw new InvalidOperationException("Received a spam message that should not be getting a reply.");
        }
        await context.YieldOutputAsync(ActionResult, cancellationToken);
    }
}

internal sealed partial class RemoveSpamExecutor(string id) : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) => protocolBuilder;
    public const string ActionResult = "Spam message removed.";

    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask HandleAsync(bool message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!message)
        {
            throw new InvalidOperationException("Received a non-spam message that should not be getting removed.");
        }
        await context.YieldOutputAsync(ActionResult, cancellationToken);
    }
}

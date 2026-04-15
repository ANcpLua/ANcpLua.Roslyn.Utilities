// Sequential workflow sample: Uppercase -> Reverse -> output.
// Rewritten from upstream Sample/01_Simple_Workflow_Sequential.cs against the
// public Executor<TIn, TOut> shape (no partial + source generator pattern).

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

/// <summary>
/// Two executors chained by an edge, output yielded from the final executor.
/// </summary>
public static class SimpleSequentialSample
{
    /// <summary>Builds the sample workflow.</summary>
    public static Workflow Build()
    {
        UppercaseExecutor uppercase = new();
        ReverseTextExecutor reverse = new();

        return new WorkflowBuilder(uppercase)
            .AddEdge(uppercase, reverse)
            .WithOutputFrom(reverse)
            .Build();
    }

    internal sealed class UppercaseExecutor() : Executor<string, string>(nameof(UppercaseExecutor))
    {
        public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            => new(message.ToUpperInvariant());
    }

    internal sealed class ReverseTextExecutor() : Executor<string, string>(nameof(ReverseTextExecutor))
    {
        public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            string result = string.Concat(message.Reverse());
            await context.YieldOutputAsync(result, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}

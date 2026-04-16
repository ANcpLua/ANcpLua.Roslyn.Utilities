// Copyright (c) Microsoft. All rights reserved.
//
// Everything a consumer needs to learn is in this one file.
// Derive WorkflowFixture<TInput>, override BuildWorkflow(), write tests.

using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Agents.AI.Workflows;
using ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

public sealed class SequentialQuickstart(ITestOutputHelper output) : WorkflowFixture<string>(output)
{
    protected override Workflow BuildWorkflow() => SequentialSample.Build();

    [Theory]
    [InlineData(ExecutionEnvironment.InProcess_Lockstep)]
    [InlineData(ExecutionEnvironment.InProcess_OffThread)]
    internal async Task ReversesAfterUppercasingAsync(ExecutionEnvironment environment)
    {
        var run = await this.RunAsync("Hello, World!", environment);

        run.Should()
           .YieldOutput<string>(s => s.Should().Be("!DLROW ,OLLEH"))
           .And.CompletedExecutors(nameof(UppercaseExecutor), nameof(ReverseTextExecutor))
           .And.HaveNoErrors();
    }

    [Fact]
    public async Task EmitsOneSuperStepPerExecutorAsync()
    {
        var run = await this.RunAsync("input");

        run.Should()
           .Emit<SuperStepCompletedEvent>(count: 2)
           .And.NotEmit<WorkflowErrorEvent>();
    }
}

internal sealed class CheckpointQuickstart(ITestOutputHelper output) : WorkflowFixture<NumberSignal>(output)
{
    protected override Workflow BuildWorkflow() => ExternalRequestSample.Build(target: 42);

    [Fact]
    public async Task ResumesFromCheckpointAndAnswersPendingRequestAsync()
    {
        var first = await this.RunWithCheckpointingAsync(NumberSignal.Init);

        first.LastCheckpoint.Should().NotBeNull();
        first.PendingRequests.Should().NotBeEmpty();

        var answered = await this.AnswerRequestAsync(first, data: 42);

        answered.Should().HaveNoErrors();
    }
}

// Licensed to the .NET Foundation under one or more agreements.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Provider-agnostic conformance tests for streaming agent runs. Mirrors <see cref="RunTests{TFixture}" />
///     but exercises <see cref="AIAgent.RunStreamingAsync(AgentSession?,AgentRunOptions?,CancellationToken)" />.
/// </summary>
public abstract class RunStreamingTests<TFixture>(Func<TFixture> createFixture) : AgentTestBase<TFixture>(createFixture)
    where TFixture : IAgentFixture
{
    /// <summary>Override to inject provider-specific run options for every test.</summary>
    public virtual Func<Task<AgentRunOptions?>> AgentRunOptionsFactory { get; set; } =
        static () => Task.FromResult(default(AgentRunOptions));

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithNoMessageDoesNotFailAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        await foreach (var _update in agent.RunStreamingAsync(
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct).ConfigureAwait(false))
        {
            // Drain to surface any provider exceptions.
        }
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithStringReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var text = await ConcatStreamAsync(agent.RunStreamingAsync(
            "What is the capital of France.",
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct), ct).ConfigureAwait(false);

        Assert.Contains("Paris", text, StringComparison.Ordinal);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithChatMessageReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var text = await ConcatStreamAsync(agent.RunStreamingAsync(
            new ChatMessage(ChatRole.User, "What is the capital of France."),
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct), ct).ConfigureAwait(false);

        Assert.Contains("Paris", text, StringComparison.Ordinal);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task SessionMaintainsHistoryAsync()
    {
        const string FranceQuestion = "What is the capital of France.";
        const string AustriaQuestion = "And Austria?";

        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var options = await AgentRunOptionsFactory.Invoke().ConfigureAwait(false);
        var first = await ConcatStreamAsync(agent.RunStreamingAsync(FranceQuestion, session, options, ct), ct).ConfigureAwait(false);
        var second = await ConcatStreamAsync(agent.RunStreamingAsync(AustriaQuestion, session, options, ct), ct).ConfigureAwait(false);

        Assert.Contains("Paris", first, StringComparison.Ordinal);
        Assert.Contains("Vienna", second, StringComparison.Ordinal);

        var history = await Fixture.GetChatHistoryAsync(agent, session).ConfigureAwait(false);
        Assert.Equal(4, history.Count);
        Assert.Equal(2, history.Count(static m => m.Role == ChatRole.User));
        Assert.Equal(2, history.Count(static m => m.Role == ChatRole.Assistant));
    }

    private static async Task<string> ConcatStreamAsync(IAsyncEnumerable<AgentResponseUpdate> updates, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            builder.Append(update.Text);
        }

        return builder.ToString();
    }
}

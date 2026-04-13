// Licensed to the .NET Foundation under one or more agreements.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Provider-agnostic conformance tests for <see cref="AIAgent.RunAsync(AgentSession?,AgentRunOptions?,CancellationToken)" />.
///     Inherit this class with a concrete <typeparamref name="TFixture" /> to run all five tests
///     against a specific provider — no per-provider duplication.
/// </summary>
public abstract class RunTests<TFixture>(Func<TFixture> createFixture) : AgentTestBase<TFixture>(createFixture)
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

        var response = await agent.RunAsync(session, cancellationToken: ct).ConfigureAwait(false);

        Assert.NotNull(response);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithStringReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var response = await agent.RunAsync(
            "What is the capital of France.",
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Contains("Paris", response.Text, StringComparison.Ordinal);
        Assert.Equal(agent.Id, response.AgentId);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithChatMessageReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var response = await agent.RunAsync(
            new ChatMessage(ChatRole.User, "What is the capital of France."),
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Contains("Paris", response.Text, StringComparison.Ordinal);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithChatMessagesReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        ChatMessage[] messages =
        [
            new(ChatRole.User, "Hello."),
            new(ChatRole.User, "What is the capital of France."),
        ];

        var response = await agent.RunAsync(
            messages,
            session,
            await AgentRunOptionsFactory.Invoke().ConfigureAwait(false),
            ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Contains("Paris", response.Text, StringComparison.Ordinal);
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
        var first = await agent.RunAsync(FranceQuestion, session, options, ct).ConfigureAwait(false);
        var second = await agent.RunAsync(AustriaQuestion, session, options, ct).ConfigureAwait(false);

        Assert.Contains("Paris", first.Text, StringComparison.Ordinal);
        Assert.Contains("Vienna", second.Text, StringComparison.Ordinal);

        var history = await Fixture.GetChatHistoryAsync(agent, session).ConfigureAwait(false);
        Assert.Equal(4, history.Count);
        Assert.Equal(2, history.Count(static m => m.Role == ChatRole.User));
        Assert.Equal(2, history.Count(static m => m.Role == ChatRole.Assistant));
        Assert.Equal(FranceQuestion, history[0].Text);
        Assert.Contains("Paris", history[1].Text, StringComparison.Ordinal);
        Assert.Equal(AustriaQuestion, history[2].Text);
        Assert.Contains("Vienna", history[3].Text, StringComparison.Ordinal);
    }
}

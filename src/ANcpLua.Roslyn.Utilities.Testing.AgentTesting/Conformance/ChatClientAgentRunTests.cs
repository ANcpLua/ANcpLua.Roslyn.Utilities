// Licensed to the .NET Foundation under one or more agreements.

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     <see cref="ChatClientAgent" />-specific conformance tests in addition to those in <see cref="RunTests{TFixture}" />.
///     Exercises instructions-only runs and end-to-end function calling against the <see cref="MenuPlugin" />.
/// </summary>
public abstract class ChatClientAgentRunTests<TFixture>(Func<TFixture> createFixture) : AgentTestBase<TFixture>(createFixture)
    where TFixture : IChatClientAgentFixture
{
    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithInstructionsAndNoMessageReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = await Fixture.CreateChatClientAgentAsync(
            instructions: "ALWAYS RESPOND WITH 'Computer says no', even if there was no user input.").ConfigureAwait(false);
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _agentCleanup = new AgentCleanup(agent, Fixture).ConfigureAwait(false);
        await using var _sessionCleanup = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var response = await agent.RunAsync(session, cancellationToken: ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.False(string.IsNullOrWhiteSpace(response.Text),
            "Agent should return non-empty response even without user input");
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithFunctionsInvokesFunctionsAndReturnsExpectedResultsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        (string Question, string ExpectedAnswer)[] qa =
        [
            ("Hello", string.Empty),
            ("What is the special soup?", "Clam Chowder"),
            ("What is the special drink?", "Chai Tea"),
            ("What is the special salad?", "Cobb Salad"),
            ("Thank you", string.Empty),
        ];

        var agent = await Fixture.CreateChatClientAgentAsync(
            aiTools:
            [
                AIFunctionFactory.Create(MenuPlugin.GetSpecials),
                AIFunctionFactory.Create(MenuPlugin.GetItemPrice),
            ]).ConfigureAwait(false);
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _agentCleanup = new AgentCleanup(agent, Fixture).ConfigureAwait(false);
        await using var _sessionCleanup = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        foreach (var (question, expected) in qa)
        {
            var result = await agent.RunAsync(
                new ChatMessage(ChatRole.User, question),
                session,
                cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Contains(expected, result.Text, StringComparison.Ordinal);
        }
    }
}

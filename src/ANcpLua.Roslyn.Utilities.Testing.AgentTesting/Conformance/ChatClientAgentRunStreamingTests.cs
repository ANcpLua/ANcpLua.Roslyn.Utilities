// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — ChatClientAgentRunStreamingTests.cs
//
// Modifications from upstream:
// - AgentTests<> renamed to AgentTestBase<> (in-tree convention)
// - Constants → ConformanceConstants
// - [RetryFact] → [Fact] (in-tree standard; retries handled at the fixture level)

using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
/// Conformance tests specific to <see cref="ChatClientAgent"/> streaming, in addition to those in
/// <see cref="RunStreamingTests{TAgentFixture}"/>.
/// </summary>
/// <typeparam name="TAgentFixture">The type of test fixture used by the concrete test implementation.</typeparam>
/// <param name="createAgentFixture">Function to create the test fixture with.</param>
public abstract class ChatClientAgentRunStreamingTests<TAgentFixture>(Func<TAgentFixture> createAgentFixture) : AgentTestBase<TAgentFixture>(createAgentFixture)
    where TAgentFixture : IChatClientAgentFixture
{
    /// <summary>Conformance test: streaming run with instructions and no user message.</summary>
    [Fact]
    public virtual async Task RunWithInstructionsAndNoMessageReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = await Fixture.CreateChatClientAgentAsync(instructions: "Always respond with 'Computer says no', even if there was no user input.").ConfigureAwait(false);
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var agentCleanup = new AgentCleanup(agent, Fixture).ConfigureAwait(false);
        await using var sessionCleanup = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var responseUpdates = await agent.RunStreamingAsync(session, cancellationToken: ct).ToListAsync(ct).ConfigureAwait(false);

        var chatResponseText = string.Concat(responseUpdates.Select(x => x.Text));
        Assert.Contains("Computer says no", chatResponseText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Conformance test: streaming run with function tools.</summary>
    [Fact]
    public virtual async Task RunWithFunctionsInvokesFunctionsAndReturnsExpectedResultsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var questionsAndAnswers = new[]
        {
            (Question: "Hello", ExpectedAnswer: string.Empty),
            (Question: "What is the special soup?", ExpectedAnswer: "Clam Chowder"),
            (Question: "What is the special drink?", ExpectedAnswer: "Chai Tea"),
            (Question: "What is the special salad?", ExpectedAnswer: "Cobb Salad"),
            (Question: "Thank you", ExpectedAnswer: string.Empty),
        };

        var agent = await Fixture.CreateChatClientAgentAsync(
            aiTools:
            [
                AIFunctionFactory.Create(MenuPlugin.GetSpecials),
                AIFunctionFactory.Create(MenuPlugin.GetItemPrice),
            ]).ConfigureAwait(false);
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);

        foreach (var questionAndAnswer in questionsAndAnswers)
        {
            var responseUpdates = await agent.RunStreamingAsync(
                new ChatMessage(ChatRole.User, questionAndAnswer.Question),
                session,
                cancellationToken: ct).ToListAsync(ct).ConfigureAwait(false);

            var chatResponseText = string.Concat(responseUpdates.Select(x => x.Text));
            Assert.Contains(questionAndAnswer.ExpectedAnswer, chatResponseText, StringComparison.OrdinalIgnoreCase);
        }
    }
}

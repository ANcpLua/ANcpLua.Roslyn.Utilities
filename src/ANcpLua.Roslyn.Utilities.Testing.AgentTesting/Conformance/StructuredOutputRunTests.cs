// Licensed to the .NET Foundation under one or more agreements.

using System.Text.Json;
using ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Conformance tests for structured-output agent runs. Verifies that providers honour
///     a JSON-schema response format and the typed <see cref="AgentResponse{T}" /> shape for
///     both reference types and primitives.
/// </summary>
public abstract class StructuredOutputRunTests<TFixture>(Func<TFixture> createFixture) : AgentTestBase<TFixture>(createFixture)
    where TFixture : IAgentFixture
{
    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithResponseFormatReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        var options = new AgentRunOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<CityInfo>(AgentAbstractionsJsonUtilities.DefaultOptions),
        };

        var response = await agent.RunAsync(
            new ChatMessage(ChatRole.User, "Provide information about the capital of France."),
            session,
            options,
            ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Contains("Paris", response.Text, StringComparison.Ordinal);
        Assert.True(TryDeserialize(response.Text, AgentAbstractionsJsonUtilities.DefaultOptions, out CityInfo cityInfo));
        Assert.Equal("Paris", cityInfo.Name);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithGenericTypeReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        AgentResponse<CityInfo> response = await agent.RunAsync<CityInfo>(
            new ChatMessage(ChatRole.User, "Provide information about the capital of France."),
            session,
            cancellationToken: ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Contains("Paris", response.Text, StringComparison.Ordinal);
        Assert.NotNull(response.Result);
        Assert.Equal("Paris", response.Result.Name);
    }

    /// <summary>Conformance test.</summary>
    [Fact]
    public virtual async Task RunWithPrimitiveTypeReturnsExpectedResultAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var agent = Fixture.Agent;
        var session = await agent.CreateSessionAsync(ct).ConfigureAwait(false);
        await using var _ = new SessionCleanup(session, Fixture).ConfigureAwait(false);

        AgentResponse<int> response = await agent.RunAsync<int>(
            new ChatMessage(ChatRole.User, "What is the sum of 15 and 27? Respond with just the number."),
            session,
            cancellationToken: ct).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.Single(response.Messages);
        Assert.Equal(42, response.Result);
    }

    private static bool TryDeserialize<T>(string json, JsonSerializerOptions options, out T result)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(json, options);
            if (deserialized is null)
            {
                result = default!;
                return false;
            }

            result = deserialized;
            return true;
        }
        catch (JsonException)
        {
            result = default!;
            return false;
        }
    }
}

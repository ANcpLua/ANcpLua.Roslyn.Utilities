// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestRequestAgent.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

// Generates N mixed paired/unpaired function-call or tool-approval requests,
// tracks serviced/unserviced state across runs. The canonical shape for
// testing request/response interrupts and checkpoint-resume flows.
public enum TestAgentRequestType
{
    FunctionCall,
    UserInputRequest,
}

internal sealed record TestRequestAgentSessionState(
    JsonElement SessionState,
    Dictionary<string, PortableValue> UnservicedRequests,
    HashSet<string> ServicedRequests,
    HashSet<string> PairedRequests);

internal sealed class TestRequestAgent(
    TestAgentRequestType requestType,
    int unpairedRequestCount,
    int pairedRequestCount,
    string? id,
    string? name) : AIAgent
{
    public Random RNG { get; set; } = new Random(HashCode.Combine(requestType, nameof(TestRequestAgent)));

    public AgentSession? LastSession { get; set; }

    protected override string? IdCore => id;

    public override string? Name => name;

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
        => new(requestType switch
        {
            TestAgentRequestType.FunctionCall => new TestRequestAgentSession<FunctionCallContent, FunctionResultContent>(),
            TestAgentRequestType.UserInputRequest => new TestRequestAgentSession<ToolApprovalRequestContent, ToolApprovalResponseContent>(),
            _ => throw new NotSupportedException(),
        });

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => this.CreateSessionCoreAsync(cancellationToken);

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => default;

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        => this.RunStreamingAsync(messages, session, options, cancellationToken).ToAgentResponseAsync(cancellationToken);

    protected override IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        => requestType switch
        {
            TestAgentRequestType.FunctionCall => this.RunStreamingAsync(new FunctionCallStrategy(), messages, session, options, cancellationToken),
            TestAgentRequestType.UserInputRequest => this.RunStreamingAsync(new FunctionApprovalStrategy(), messages, session, options, cancellationToken),
            _ => throw new NotSupportedException($"Unknown AgentRequestType {requestType}"),
        };

    // Reservoir sampling: uniformly pick c indices from [0..n) without listing them all.
    private static int[] SampleIndicies(Random rng, int n, int c)
    {
        int[] result = Enumerable.Range(0, c).ToArray();
        for (int i = c; i < n; i++)
        {
            int radix = rng.Next(i);
            if (radix < c)
            {
                result[radix] = i;
            }
        }
        return result;
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync<TRequest, TResponse>(
        IRequestResponseStrategy<TRequest, TResponse> strategy,
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        this.LastSession = session ??= await this.CreateSessionAsync(cancellationToken);
        var traSession = ConvertSession<TRequest, TResponse>(session);

        if (traSession.HasSentRequests)
        {
            foreach (var response in messages.SelectMany(m => m.Contents).OfType<TResponse>())
            {
                strategy.ProcessResponse(response, traSession);
            }

            yield return traSession.UnservicedRequests.Count == 0
                ? new(ChatRole.Assistant, "Done")
                : new(ChatRole.Assistant, $"Remaining: {traSession.UnservicedRequests.Count}");

            yield break;
        }

        int totalRequestCount = unpairedRequestCount + pairedRequestCount;
        yield return new(ChatRole.Assistant, $"Creating {totalRequestCount} requests, {pairedRequestCount} paired.");

        HashSet<int> servicedIndicies = [.. SampleIndicies(this.RNG, totalRequestCount, pairedRequestCount)];
        (string, TRequest)[] requests = strategy.CreateRequests(totalRequestCount).ToArray();
        List<AIContent> pairedResponses = new(capacity: pairedRequestCount);

        for (int i = 0; i < requests.Length; i++)
        {
            (string id, TRequest request) = requests[i];
            if (servicedIndicies.Contains(i))
            {
                traSession.PairedRequests.Add(id);
                pairedResponses.Add(strategy.CreatePairedResponse(request));
            }
            else
            {
                traSession.UnservicedRequests.Add(id, request);
            }

            yield return new(ChatRole.Assistant, [request]);
        }

        yield return new(ChatRole.Assistant, pairedResponses);
        traSession.HasSentRequests = true;
    }

    private static TestRequestAgentSession<TRequest, TResponse> ConvertSession<TRequest, TResponse>(AgentSession session)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        if (session is not TestRequestAgentSession<TRequest, TResponse> traSession)
        {
            throw new ArgumentException($"Bad AgentSession type: Expected {typeof(TestRequestAgentSession<TRequest, TResponse>)}, got {session.GetType()}.", nameof(session));
        }
        return traSession;
    }

    internal IEnumerable<ExternalResponse> ValidateUnpairedRequests(List<ExternalRequest> requests)
    {
        List<object> responses = requestType switch
        {
            TestAgentRequestType.FunctionCall =>
                [.. this.ValidateUnpairedRequests(requests.Select(Extract<FunctionCallContent>), new FunctionCallStrategy())],
            TestAgentRequestType.UserInputRequest =>
                [.. this.ValidateUnpairedRequests(requests.Select(Extract<ToolApprovalRequestContent>), new FunctionApprovalStrategy())],
            _ => throw new NotSupportedException($"Unknown AgentRequestType {requestType}"),
        };

        return Enumerable.Zip(requests, responses, (req, resp) => req.CreateResponse(resp));

        static TRequest Extract<TRequest>(ExternalRequest request)
        {
            request.TryGetDataAs(out TRequest? content).Should().BeTrue();
            return content!;
        }
    }

    private IEnumerable<TResponse> ValidateUnpairedRequests<TRequest, TResponse>(IEnumerable<TRequest> requests, IRequestResponseStrategy<TRequest, TResponse> strategy)
        where TRequest : AIContent
        where TResponse : AIContent
    {
        this.LastSession.Should().NotBeNull();
        var traSession = ConvertSession<TRequest, TResponse>(this.LastSession!);

        requests.Should().HaveCount(traSession.UnservicedRequests.Count);
        foreach (var request in requests)
        {
            string requestId = RetrieveId(request);
            traSession.UnservicedRequests.Should().ContainKey(requestId);
            yield return strategy.CreatePairedResponse(request);
        }
    }

    private static string RetrieveId<TRequest>(TRequest request) where TRequest : AIContent
        => request switch
        {
            FunctionCallContent fc => fc.CallId,
            ToolApprovalRequestContent ar => ar.RequestId,
            _ => throw new NotSupportedException($"Unknown request type {typeof(TRequest)}"),
        };

    private interface IRequestResponseStrategy<TRequest, TResponse>
        where TRequest : AIContent
        where TResponse : AIContent
    {
        IEnumerable<(string, TRequest)> CreateRequests(int count);
        TResponse CreatePairedResponse(TRequest request);
        void ProcessResponse(TResponse response, TestRequestAgentSession<TRequest, TResponse> session);
    }

    private sealed class FunctionCallStrategy : IRequestResponseStrategy<FunctionCallContent, FunctionResultContent>
    {
        public FunctionResultContent CreatePairedResponse(FunctionCallContent request) => new(request.CallId, request);

        public IEnumerable<(string, FunctionCallContent)> CreateRequests(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string callId = Guid.NewGuid().ToString("N");
                yield return (callId, new FunctionCallContent(callId, "TestFunction"));
            }
        }

        public void ProcessResponse(FunctionResultContent response, TestRequestAgentSession<FunctionCallContent, FunctionResultContent> session)
        {
            if (session.UnservicedRequests.TryGetValue(response.CallId, out var request))
            {
                response.Result.As<FunctionCallContent>().Should().Be(request);
                session.ServicedRequests.Add(response.CallId);
                session.UnservicedRequests.Remove(response.CallId);
                return;
            }

            if (session.ServicedRequests.Contains(response.CallId))
            {
                throw new InvalidOperationException($"Seeing duplicate response with id {response.CallId}");
            }

            if (session.PairedRequests.Contains(response.CallId))
            {
                throw new InvalidOperationException($"Seeing explicit response to initially paired request with id {response.CallId}");
            }

            throw new InvalidOperationException($"Seeing response to nonexistent request with id {response.CallId}");
        }
    }

    private sealed class FunctionApprovalStrategy : IRequestResponseStrategy<ToolApprovalRequestContent, ToolApprovalResponseContent>
    {
        public ToolApprovalResponseContent CreatePairedResponse(ToolApprovalRequestContent request)
            => new(request.RequestId, true, request.ToolCall);

        public IEnumerable<(string, ToolApprovalRequestContent)> CreateRequests(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string id = Guid.NewGuid().ToString("N");
                yield return (id, new ToolApprovalRequestContent(id, new FunctionCallContent(id, "TestFunction")));
            }
        }

        public void ProcessResponse(ToolApprovalResponseContent response, TestRequestAgentSession<ToolApprovalRequestContent, ToolApprovalResponseContent> session)
        {
            if (session.UnservicedRequests.TryGetValue(response.RequestId, out var request))
            {
                response.Approved.Should().BeTrue();
                ((FunctionCallContent)response.ToolCall).Should().Be((FunctionCallContent)request.ToolCall);
                session.ServicedRequests.Add(response.RequestId);
                session.UnservicedRequests.Remove(response.RequestId);
                return;
            }

            if (session.ServicedRequests.Contains(response.RequestId))
            {
                throw new InvalidOperationException($"Seeing duplicate response with id {response.RequestId}");
            }

            if (session.PairedRequests.Contains(response.RequestId))
            {
                throw new InvalidOperationException($"Seeing explicit response to initially paired request with id {response.RequestId}");
            }

            throw new InvalidOperationException($"Seeing response to nonexistent request with id {response.RequestId}");
        }
    }

    private sealed class TestRequestAgentSession<TRequest, TResponse> : AgentSession
        where TRequest : AIContent
        where TResponse : AIContent
    {
        public bool HasSentRequests { get; set; }

        public Dictionary<string, TRequest> UnservicedRequests { get; } = [];

        public HashSet<string> ServicedRequests { get; } = [];

        public HashSet<string> PairedRequests { get; } = [];

        public TestRequestAgentSession() { }

        public TestRequestAgentSession(JsonElement element, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var state = JsonSerializer.Deserialize<TestRequestAgentSessionState>(element, jsonSerializerOptions)
                ?? throw new ArgumentException("Unable to deserialize session state.");

            this.StateBag = AgentSessionStateBag.Deserialize(state.SessionState);
            this.UnservicedRequests = state.UnservicedRequests.ToDictionary(kv => kv.Key, kv => kv.Value.As<TRequest>()!);
            this.ServicedRequests = state.ServicedRequests;
            this.PairedRequests = state.PairedRequests;
        }

        internal JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var portable = this.UnservicedRequests.ToDictionary(kv => kv.Key, kv => new PortableValue(kv.Value));
            var state = new TestRequestAgentSessionState(this.StateBag.Serialize(), portable, this.ServicedRequests, this.PairedRequests);
            return JsonSerializer.SerializeToElement(state, jsonSerializerOptions);
        }
    }
}

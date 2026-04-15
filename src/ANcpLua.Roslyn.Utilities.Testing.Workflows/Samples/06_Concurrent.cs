// Fan-out/fan-in: host N agents concurrently through AgentWorkflowBuilder.
// Source: Sample/11_Concurrent_HostAsAgent.cs

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANcpLua.Roslyn.Utilities.Testing.AgentTesting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Samples;

internal static class ConcurrentSample
{
    public const int AgentCount = 2;
    public const string EchoAgentIdPrefix = "echo-";
    public const string EchoAgentNamePrefix = "Echo";

    public static string ExpectedOutputForInput(string input, int agentNumber) => $"{EchoAgentNamePrefix}{agentNumber}: {input}";

    public static Workflow Build()
    {
        FakeEchoAgent[] agents = Enumerable.Range(1, AgentCount)
            .Select(i => new FakeEchoAgent($"{EchoAgentIdPrefix}{i}", $"{EchoAgentNamePrefix}{i}"))
            .ToArray();

        return AgentWorkflowBuilder.BuildConcurrent(agents);
    }

    public static async ValueTask RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment, IEnumerable<string> inputs)
    {
        AIAgent hostAgent = Build().AsAIAgent("echo-workflow", "EchoW", executionEnvironment: environment);
        AgentSession session = await hostAgent.CreateSessionAsync();

        foreach (string input in inputs)
        {
            AgentResponse response;
            ResponseContinuationToken? continuationToken = null;
            do
            {
                response = await hostAgent.RunAsync(input, session, new AgentRunOptions { ContinuationToken = continuationToken });
            }
            while ((continuationToken = response.ContinuationToken) is { });

            foreach (var message in response.Messages)
            {
                writer.WriteLine($"{message.AuthorName}: {message.Text}");
            }
        }
    }
}

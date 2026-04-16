// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/EdgeMap.cs
// TRIMMED: only TryRegisterPort is used by TestRunContext. Full EdgeMap manages edge runners,
// delivery mappings, checkpoint state import/export — none of which our test harness needs.

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

/// <summary>Minimal stub of the internal EdgeMap — only the port registration surface used by TestRunContext.</summary>
internal sealed class EdgeMap
{
    private readonly Dictionary<string, object> _portEdgeRunners = [];

    public bool TryRegisterPort(IRunnerContext runContext, string executorId, RequestPort port)
        => this._portEdgeRunners.TryAdd(port.Id, new { ExecutorId = executorId });
}

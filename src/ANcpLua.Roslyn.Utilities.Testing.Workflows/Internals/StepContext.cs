// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Execution/StepContext.cs
// TRIMMED: Removed ExportMessages/ImportMessages (depend on PortableMessageEnvelope, internal).
// Only QueuedMessages, HasMessages, and MessagesFor are needed by IRunnerContext.AdvanceAsync callers.

using System.Collections.Concurrent;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal sealed class StepContext
{
    public ConcurrentDictionary<string, ConcurrentQueue<MessageEnvelope>> QueuedMessages { get; } = [];

    public bool HasMessages => !this.QueuedMessages.IsEmpty && this.QueuedMessages.Values.Any(messageQueue => !messageQueue.IsEmpty);

    public ConcurrentQueue<MessageEnvelope> MessagesFor(string target)
    {
        return this.QueuedMessages.GetOrAdd(target, _ => new ConcurrentQueue<MessageEnvelope>());
    }
}

// Minimal reimplementation of Microsoft.Agents.AI.Workflows internal StateManager
// Original: Execution/StateManager.cs (274 lines, uses StateScope/UpdateKey/StateUpdate/Microsoft.Shared.Diagnostics)
// This stub provides only the 5 methods called by TestWorkflowContext: ClearState, WriteState, ReadState, ReadOrInitState, ReadKeys.
// Backed by a simple Dictionary<string, Dictionary<string, object?>>.

using System.Collections.Concurrent;
using Microsoft.Agents.AI.Workflows.Execution;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

/// <summary>In-memory state manager for workflow unit tests. Thread-safe, no persistence.</summary>
internal sealed class StateManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _scopes = new();

    private static string ScopeKey(ScopeId scopeId) => $"{scopeId.ExecutorId}::{scopeId.ScopeName}";

    private ConcurrentDictionary<string, object?> GetScope(ScopeId scopeId)
        => this._scopes.GetOrAdd(ScopeKey(scopeId), _ => new());

    public ValueTask ClearStateAsync(ScopeId scopeId)
    {
        this.GetScope(scopeId).Clear();
        return default;
    }

    public ValueTask WriteStateAsync<T>(ScopeId scopeId, string key, T? value)
    {
        this.GetScope(scopeId)[key] = value;
        return default;
    }

    public ValueTask<T?> ReadStateAsync<T>(ScopeId scopeId, string key)
    {
        if (this.GetScope(scopeId).TryGetValue(key, out object? value) && value is T typed)
        {
            return new(typed);
        }

        return new(default(T?));
    }

    public ValueTask<T> ReadOrInitStateAsync<T>(ScopeId scopeId, string key, Func<T> initialStateFactory)
    {
        var scope = this.GetScope(scopeId);
        if (scope.TryGetValue(key, out object? value) && value is T typed)
        {
            return new(typed);
        }

        T init = initialStateFactory();
        scope[key] = init;
        return new(init);
    }

    public ValueTask<HashSet<string>> ReadKeysAsync(ScopeId scopeId)
        => new(new HashSet<string>(this.GetScope(scopeId).Keys));
}

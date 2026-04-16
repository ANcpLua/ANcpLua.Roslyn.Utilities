// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Checkpointing;

/// <summary>Drop-in JsonCheckpointStore with no filesystem — useful for checkpoint unit tests.</summary>
internal sealed class InMemoryJsonStore : JsonCheckpointStore
{
    private readonly Dictionary<string, SessionCheckpointCache<JsonElement>> _store = [];

    private SessionCheckpointCache<JsonElement> EnsureSessionStore(string sessionId)
    {
        if (!this._store.TryGetValue(sessionId, out SessionCheckpointCache<JsonElement>? runStore))
        {
            runStore = this._store[sessionId] = new();
        }

        return runStore;
    }

    public override ValueTask<CheckpointInfo> CreateCheckpointAsync(string sessionId, JsonElement value, CheckpointInfo? parent = null) =>
        new(this.EnsureSessionStore(sessionId).Add(sessionId, value));

    public override ValueTask<JsonElement> RetrieveCheckpointAsync(string sessionId, CheckpointInfo key)
    {
        if (!this.EnsureSessionStore(sessionId).TryGet(key, out JsonElement result))
        {
            throw new KeyNotFoundException($"Could not retrieve checkpoint with id {key.CheckpointId} for session {sessionId}");
        }

        return new(result);
    }

    public override ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(string sessionId, CheckpointInfo? withParent = null) =>
        new(this.EnsureSessionStore(sessionId).Index);
}

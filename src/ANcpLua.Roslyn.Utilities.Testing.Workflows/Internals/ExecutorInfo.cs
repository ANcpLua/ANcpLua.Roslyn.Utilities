// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Checkpointing/ExecutorInfo.cs

// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows.Internals;

internal sealed record class ExecutorInfo(TypeId ExecutorType, string ExecutorId)
{
    public bool IsMatch<T>() where T : Executor =>
        this.ExecutorType.IsMatch<T>()
            && this.ExecutorId == typeof(T).Name;

    public bool IsMatch(Executor executor) =>
        this.ExecutorType.IsMatch(executor.GetType())
            && this.ExecutorId == executor.Id;

    public bool IsMatch(ExecutorBinding binding) =>
        this.ExecutorType.IsMatch(binding.ExecutorType)
            && this.ExecutorId == binding.Id;
}

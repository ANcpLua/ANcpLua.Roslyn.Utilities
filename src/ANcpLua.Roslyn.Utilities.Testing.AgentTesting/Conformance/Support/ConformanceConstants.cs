// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;

/// <summary>Tunables for the conformance test suite (retry counts, delays).</summary>
public static class ConformanceConstants
{
    /// <summary>Default retry count for <c>RetryFact</c>-decorated conformance tests.</summary>
    public const int RetryCount = 3;

    /// <summary>Default delay between retries, in milliseconds.</summary>
    public const int RetryDelayMs = 5000;
}

// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance;

/// <summary>
///     Tiny payload type used by <see cref="StructuredOutputRunTests{TFixture}" /> to verify
///     JSON-schema response formats end-to-end.
/// </summary>
public sealed class CityInfo
{
    /// <summary>Name of the city returned by the model.</summary>
    public string? Name { get; set; }
}

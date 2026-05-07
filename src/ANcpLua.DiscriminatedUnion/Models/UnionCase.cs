namespace ANcpLua.Analyzers.DiscriminatedUnion.Models;

/// <summary>
///     Equatable description of one case inside a discriminated-union root. Carries everything the
///     output generator needs to emit the partial declaration, the <c>Match</c> override, and the
///     <c>Switch</c> override without re-touching the semantic model.
/// </summary>
internal readonly record struct UnionCase(
    string Name,
    string ParameterName);

namespace ANcpLua.Analyzers.DiscriminatedUnion.Models;

/// <summary>
///     Equatable description of one [DiscriminatedUnion] usage. Holds enough information to emit
///     the <c>Match</c>/<c>Switch</c> dispatchers and the sealed-partial-record case overrides.
/// </summary>
internal readonly record struct UnionModel(
    string RootNamespace,
    string RootName,
    string TypeParameterList,
    string FullyQualifiedRoot,
    EquatableArray<UnionCase> Cases);

namespace ANcpLua.Analyzers.ExtensibleEnumMirror.Models;

/// <summary>
///     Pure equatable description of one [ExtensibleEnumMirror] usage. Holds enough information to
///     emit the mirror enum + helper methods without re-touching the semantic model.
/// </summary>
internal readonly record struct MirrorModel(
    string MarkerNamespace,
    string MarkerName,
    string MarkerKeyword,
    string TargetFullyQualified,
    string TargetSimpleName,
    EquatableArray<string> KnownMembers);

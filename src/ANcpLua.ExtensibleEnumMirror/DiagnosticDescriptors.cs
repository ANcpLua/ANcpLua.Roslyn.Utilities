namespace ANcpLua.Analyzers.ExtensibleEnumMirror;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MarkerMustBePartial = new(
        "AL0200",
        "Marker type must be partial",
        "Marker type '{0}' must be declared partial to use ExtensibleEnumMirrorAttribute",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MissingTargetType = new(
        "AL0201",
        "Missing target type",
        "ExtensibleEnumMirrorAttribute requires a typeof() argument",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor InvalidTarget = new(
        "AL0202",
        "Invalid extensible-enum target",
        "Type '{0}' is not a string-backed extensible-enum struct (expected: non-generic readonly struct that implements IEquatable<Self>)",
        "Usage",
        DiagnosticSeverity.Error,
        true);
}

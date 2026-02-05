namespace ANcpLua.AotReflection;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor InvalidTarget = new(
        "KRIN001",
        "Invalid AOT reflection target",
        "AotReflectionAttribute can only be applied to classes or structs",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "KRIN002",
        "Type must be partial",
        "Type '{0}' must be declared partial to use AotReflectionAttribute",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IndexerNotSupported = new(
        "KRIN003",
        "Indexer properties are not supported",
        "Indexer property '{0}' is not supported by AOT reflection and will be skipped",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericMethodNotSupported = new(
        "KRIN004",
        "Generic methods are not supported",
        "Generic method '{0}' is not supported by AOT reflection and will be skipped",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}

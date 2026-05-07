namespace ANcpLua.Analyzers.DiscriminatedUnion;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor RootMustBePartialRecord = new(
        "AL0300",
        "DiscriminatedUnion root must be a partial record",
        "Type '{0}' must be declared as 'partial record' to use [DiscriminatedUnion]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RootMustHaveCases = new(
        "AL0301",
        "DiscriminatedUnion root must declare at least one nested partial record case",
        "Type '{0}' is marked [DiscriminatedUnion] but contains no nested partial record cases",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CaseMustBeNestedPartialRecord = new(
        "AL0302",
        "DiscriminatedUnion case must be a partial record nested in the union root",
        "Member '{0}' inside [DiscriminatedUnion] root '{1}' must be declared as 'partial record' to participate as a case",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor RootMustNotHavePrimaryCtor = new(
        "AL0303",
        "DiscriminatedUnion root must not declare primary-constructor parameters",
        "Type '{0}' is marked [DiscriminatedUnion] and must not declare a primary constructor — the generator emits a private parameterless constructor that closes the hierarchy to nested cases",
        "Usage",
        DiagnosticSeverity.Error,
        true);
}

namespace ANcpLua.Roslyn.Utilities.Testing.Analysis;

/// <summary>
///     Pure helper that builds a human-readable hint explaining why a generator step was not cached.
/// </summary>
/// <remarks>
///     <para>
///         The hint priority is: <c>Modified</c> first (an equality failure, the most actionable case),
///         then <c>New</c>, then <c>Removed</c>. <c>Modified</c> is the only branch that depends on the
///         model's equality kind, because the other two are about the upstream pipeline producing a
///         different number of items rather than equality breaking on existing items.
///     </para>
///     <para>
///         The legend constant exposes the meaning of the C/U/M/N/R counters used in
///         <c>StepFormatter.FormatBreakdown</c>, so a user reading the failure message for the first
///         time does not have to guess.
///     </para>
/// </remarks>
internal static class CachingHintBuilder
{
    public const string Legend = "Legend: C=Cached U=Unchanged | M=Modified N=New R=Removed";

    public static string BuildHint(ModelEqualityKind equalityKind, int modified, int @new, int removed)
    {
        if (modified > 0) return BuildModifiedHint(equalityKind);
        if (@new > 0) return "new outputs appeared (upstream pipeline produced additional items)";
        if (removed > 0) return "outputs removed (upstream pipeline produced fewer items)";
        return "step re-computed";
    }

    private static string BuildModifiedHint(ModelEqualityKind equalityKind) => equalityKind switch
    {
        ModelEqualityKind.Equatable =>
            "IEquatable<T> is implemented but Equals returned false for structurally-equivalent instances; "
            + "check for reference-type fields compared with ReferenceEquals or captured Compilation/ISymbol state",

        ModelEqualityKind.EqualsOverride =>
            "model lacks IEquatable<T>; Roslyn fell back to object.Equals override which returned false "
            + "(implement IEquatable<T> for reliable caching)",

        ModelEqualityKind.ReferenceEquality =>
            "no value equality on model — object.Equals degenerates to ReferenceEquals; "
            + "override Equals/GetHashCode or implement IEquatable<T>",

        ModelEqualityKind.Unknown =>
            "output equality broke between runs (model type could not be inspected)",

        _ => "output equality broke between runs"
    };
}

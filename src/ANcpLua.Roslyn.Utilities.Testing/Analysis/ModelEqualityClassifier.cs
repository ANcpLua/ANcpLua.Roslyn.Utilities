using System.Reflection;

namespace ANcpLua.Roslyn.Utilities.Testing.Analysis;

/// <summary>
///     Classifies a model type's equality contract for diagnostic purposes.
/// </summary>
/// <remarks>
///     <para>
///         Roslyn's incremental generator pipeline uses
///         <see cref="System.Collections.Generic.EqualityComparer{T}.Default" /> to compare step outputs
///         between runs. The comparer prefers <see cref="IEquatable{T}" /> when implemented and falls back
///         to <see cref="object.Equals(object)" /> otherwise. Knowing which path applies tells the caching
///         diagnostic which fix is appropriate when a step is re-computed unexpectedly.
///     </para>
/// </remarks>
internal enum ModelEqualityKind
{
    /// <summary>The model type is unknown (no outputs were available to inspect).</summary>
    Unknown,

    /// <summary>
    ///     The model is a reference type with no <see cref="IEquatable{T}" /> implementation and
    ///     no <see cref="object.Equals(object)" /> override. <see cref="object.Equals(object)" /> degenerates
    ///     to <see cref="object.ReferenceEquals(object, object)" />, which never matches across runs.
    /// </summary>
    ReferenceEquality,

    /// <summary>
    ///     The model overrides <see cref="object.Equals(object)" /> but does not implement
    ///     <see cref="IEquatable{T}" />. Roslyn will use the override, but the missing strongly-typed
    ///     equality is a code smell and often coincides with broken equality semantics.
    /// </summary>
    EqualsOverride,

    /// <summary>
    ///     The model implements <see cref="IEquatable{T}" /> for itself. If the step is still
    ///     re-computed, the implementation returned <see langword="false" /> for structurally-equivalent
    ///     instances — typically due to reference-comparing a captured field.
    /// </summary>
    Equatable
}

/// <summary>
///     Pure helper that inspects a runtime <see cref="Type" /> and reports its equality kind.
/// </summary>
/// <remarks>
///     Used by caching diagnostic formatters to give actionable hints. Kept separate from the
///     formatters so it can be unit-tested without spinning up a generator pipeline.
/// </remarks>
internal static class ModelEqualityClassifier
{
    public static ModelEqualityKind Classify(Type? type)
    {
        if (type is null) return ModelEqualityKind.Unknown;

        if (ImplementsIEquatableForSelf(type)) return ModelEqualityKind.Equatable;

        return OverridesObjectEquals(type)
            ? ModelEqualityKind.EqualsOverride
            : ModelEqualityKind.ReferenceEquality;
    }

    private static bool ImplementsIEquatableForSelf(Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;
            if (iface.GetGenericTypeDefinition() != typeof(IEquatable<>)) continue;
            if (iface.GetGenericArguments()[0] == type) return true;
        }

        return false;
    }

    private static bool OverridesObjectEquals(Type type)
    {
        var equalsMethod = type.GetMethod(
            nameof(Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(object)],
            modifiers: null);

        if (equalsMethod is null) return false;

        var declaringType = equalsMethod.DeclaringType;
        // object.Equals → no override. ValueType.Equals → reflection-based, not a deliberate override.
        return declaringType != typeof(object) && declaringType != typeof(ValueType);
    }
}

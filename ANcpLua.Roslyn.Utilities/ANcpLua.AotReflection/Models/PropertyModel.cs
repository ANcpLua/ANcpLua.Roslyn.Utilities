namespace ANcpLua.AotReflection;

internal readonly record struct PropertyModel(
    string Name,
    string TypeFullyQualified,
    string ContainingTypeFullyQualified,
    bool IsStatic,
    bool IsNullable,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    string Accessibility)
    : IEquatable<PropertyModel>;

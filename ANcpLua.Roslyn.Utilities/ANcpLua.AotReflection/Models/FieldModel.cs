namespace ANcpLua.AotReflection;

internal readonly record struct FieldModel(
    string Name,
    string TypeFullyQualified,
    string ContainingTypeFullyQualified,
    bool IsStatic,
    bool IsReadOnly,
    bool IsConst,
    string? ConstValue,
    string Accessibility)
    : IEquatable<FieldModel>;

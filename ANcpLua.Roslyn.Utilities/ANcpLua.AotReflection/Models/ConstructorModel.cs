namespace ANcpLua.AotReflection;

internal readonly record struct ConstructorModel(
    string ContainingTypeFullyQualified,
    EquatableArray<ParameterModel> Parameters,
    string Accessibility)
    : IEquatable<ConstructorModel>;

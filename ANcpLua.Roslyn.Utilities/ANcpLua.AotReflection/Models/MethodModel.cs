namespace ANcpLua.AotReflection;

internal readonly record struct MethodModel(
    string Name,
    string ReturnTypeFullyQualified,
    string ContainingTypeFullyQualified,
    EquatableArray<ParameterModel> Parameters,
    bool IsStatic,
    bool IsAsync,
    bool IsExtension,
    bool IsGeneric,
    bool ReturnsVoid,
    string Accessibility)
    : IEquatable<MethodModel>;

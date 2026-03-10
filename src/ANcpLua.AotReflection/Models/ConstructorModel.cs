namespace ANcpLua.Analyzers.AotReflection.Models;

internal readonly record struct ConstructorModel(
    string ContainingTypeFullyQualified,
    EquatableArray<ParameterModel> Parameters,
    string Accessibility);
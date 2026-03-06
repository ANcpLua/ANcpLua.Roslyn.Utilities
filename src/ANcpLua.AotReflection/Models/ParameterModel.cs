namespace ANcpLua.Analyzers.AotReflection.Models;

internal readonly record struct ParameterModel(
    string Name,
    string TypeFullyQualified,
    bool IsNullable,
    bool HasDefaultValue,
    string? DefaultValueLiteral)
    : IEquatable<ParameterModel>;
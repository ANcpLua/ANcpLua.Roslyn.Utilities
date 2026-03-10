namespace ANcpLua.Analyzers.AotReflection.Models;

internal readonly record struct FieldModel(
    string Name,
    string TypeFullyQualified,
    string ContainingTypeFullyQualified,
    bool IsStatic,
    bool IsReadOnly,
    bool IsConst,
    string? ConstValue,
    string Accessibility);
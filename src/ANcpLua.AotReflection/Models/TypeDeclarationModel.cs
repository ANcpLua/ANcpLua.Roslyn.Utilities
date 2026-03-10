namespace ANcpLua.Analyzers.AotReflection.Models;

internal readonly record struct TypeDeclarationModel(
    string Name,
    string Keyword,
    string Modifiers,
    string TypeParameters,
    EquatableArray<string> ConstraintClauses);
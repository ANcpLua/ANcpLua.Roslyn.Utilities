using ANcpLua.Analyzers.AotReflection.Models;

namespace ANcpLua.Analyzers.AotReflection.Extraction;

internal static class FieldExtractor
{
    public static DiagnosticFlow<EquatableArray<FieldModel>> ExtractFields(
        INamedTypeSymbol type,
        AotReflectionOptions options,
        CancellationToken cancellationToken)
    {
        var fields = new List<FieldModel>();
        var diagnostics = new List<DiagnosticInfo>();
        var constMatch = Match.Field().Const();

        var members = options.IncludeInherited
            ? type.GetAllMembers()
            : type.GetMembers();

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not IFieldSymbol field) continue;

            if (field.IsImplicitlyDeclared) continue;

            if (field.AssociatedSymbol is not null) continue;

            if (!options.IncludePrivate && field.DeclaredAccessibility != Accessibility.Public) continue;

            var isConst = constMatch.Matches(field) || field.IsConst;
            var constValue = isConst && field.HasConstantValue
                ? LiteralFormatter.FormatConstant(field.ConstantValue, field.Type)
                : null;

            fields.Add(new FieldModel(
                field.Name,
                field.Type.GetFullyQualifiedName(),
                type.GetFullyQualifiedName(),
                field.IsStatic,
                field.IsReadOnly,
                isConst,
                constValue,
                field.DeclaredAccessibility.ToAccessibilityString()));
        }

        var flow = DiagnosticFlow.Ok(fields.Count is 0 ? default : fields.ToArray().ToEquatableArray());
        foreach (var diagnostic in diagnostics) flow = flow.Warn(diagnostic);

        return flow;
    }
}
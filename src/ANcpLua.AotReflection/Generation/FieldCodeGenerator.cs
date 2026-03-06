using ANcpLua.Analyzers.AotReflection.Models;

namespace ANcpLua.Analyzers.AotReflection.Generation;

internal static class FieldCodeGenerator
{
    public static void WriteFieldMetadataArray(IndentedStringBuilder sb, TypeModel type)
    {
        if (type.Fields.IsEmpty)
        {
            sb.AppendLine(
                "Fields = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.FieldMetadata>(),");
            return;
        }

        sb.AppendLine("Fields = new global::ANcpLua.Analyzers.AotReflection.FieldMetadata[]");
        sb.BeginBlock();

        foreach (var field in type.Fields)
        {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.FieldMetadata");
            sb.BeginBlock();

            sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(field.Name)},");
            sb.AppendLine($"Type = {GenerationHelpers.GetTypeOf(field.TypeFullyQualified)},");
            sb.AppendLine($"IsStatic = {GenerationHelpers.BooleanLiteral(field.IsStatic)},");
            sb.AppendLine($"IsReadOnly = {GenerationHelpers.BooleanLiteral(field.IsReadOnly)},");
            sb.AppendLine($"IsConst = {GenerationHelpers.BooleanLiteral(field.IsConst)},");

            var constValue = field.IsConst && field.ConstValue is not null ? field.ConstValue : "null";
            sb.AppendLine($"ConstValue = {constValue},");

            sb.AppendLine(
                $"ReflectionInfo = {GenerationHelpers.GetTypeOf(field.ContainingTypeFullyQualified)}.GetField({GenerationHelpers.StringLiteral(field.Name)}, {GenerationHelpers.BindingFlagsAll}),");
            sb.AppendLine($"Getter = {GetGetterExpression(field)},");
            sb.AppendLine($"Setter = {GetSetterExpression(field)}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    private static string GetGetterExpression(FieldModel field)
    {
        if (field is { IsConst: true, ConstValue: not null }) return $"_ => {field.ConstValue}";

        return field.IsStatic
            ? $"_ => {field.ContainingTypeFullyQualified}.{field.Name}"
            : $"obj => (({field.ContainingTypeFullyQualified})obj!).{field.Name}";
    }

    private static string GetSetterExpression(FieldModel field)
    {
        if (field.IsConst || field.IsReadOnly) return "null";

        var castValue = $"({field.TypeFullyQualified})value!";

        return field.IsStatic
            ? $"(_, value) => {field.ContainingTypeFullyQualified}.{field.Name} = {castValue}"
            : $"(obj, value) => (({field.ContainingTypeFullyQualified})obj!).{field.Name} = {castValue}";
    }
}
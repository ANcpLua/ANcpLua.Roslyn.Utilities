using ANcpLua.Analyzers.AotReflection.Models;

namespace ANcpLua.Analyzers.AotReflection.Generation;

internal static class PropertyCodeGenerator
{
    public static void WritePropertyMetadataArray(IndentedStringBuilder sb, TypeModel type)
    {
        if (type.Properties.IsEmpty)
        {
            sb.AppendLine(
                "Properties = global::System.Array.Empty<global::ANcpLua.Analyzers.AotReflection.PropertyMetadata>(),");
            return;
        }

        sb.AppendLine("Properties = new global::ANcpLua.Analyzers.AotReflection.PropertyMetadata[]");
        sb.BeginBlock();

        foreach (var property in type.Properties)
        {
            sb.AppendLine("new global::ANcpLua.Analyzers.AotReflection.PropertyMetadata");
            sb.BeginBlock();

            sb.AppendLine($"Name = {GenerationHelpers.StringLiteral(property.Name)},");
            sb.AppendLine($"Type = {GenerationHelpers.GetTypeOf(property.TypeFullyQualified)},");
            sb.AppendLine(
                $"ReflectionInfo = {GenerationHelpers.GetTypeOf(property.ContainingTypeFullyQualified)}.GetProperty({GenerationHelpers.StringLiteral(property.Name)}, {GenerationHelpers.BindingFlagsAll}),");
            sb.AppendLine($"IsStatic = {GenerationHelpers.BooleanLiteral(property.IsStatic)},");
            sb.AppendLine($"IsNullable = {GenerationHelpers.BooleanLiteral(property.IsNullable)},");

            sb.AppendLine($"Getter = {GetGetterExpression(property)},");
            sb.AppendLine($"Setter = {GetSetterExpression(property)}");

            sb.EndBlock("},");
        }

        sb.EndBlock("},");
    }

    private static string GetGetterExpression(PropertyModel property)
    {
        if (!property.HasGetter) return "null";

        return property.IsStatic
            ? $"_ => {property.ContainingTypeFullyQualified}.{property.Name}"
            : $"obj => (({property.ContainingTypeFullyQualified})obj!).{property.Name}";
    }

    private static string GetSetterExpression(PropertyModel property)
    {
        if (!property.HasSetter || property.IsInitOnly) return "null";

        var castValue = $"({property.TypeFullyQualified})value!";

        return property.IsStatic
            ? $"(_, value) => {property.ContainingTypeFullyQualified}.{property.Name} = {castValue}"
            : $"(obj, value) => (({property.ContainingTypeFullyQualified})obj!).{property.Name} = {castValue}";
    }
}
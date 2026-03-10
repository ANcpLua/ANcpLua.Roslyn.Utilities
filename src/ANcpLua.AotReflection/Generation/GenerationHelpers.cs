namespace ANcpLua.Analyzers.AotReflection.Generation;

internal static class GenerationHelpers
{
    private const string GlobalPrefix = "global::";

    public const string BindingFlagsAll =
        "global::System.Reflection.BindingFlags.Public | " +
        "global::System.Reflection.BindingFlags.NonPublic | " +
        "global::System.Reflection.BindingFlags.Instance | " +
        "global::System.Reflection.BindingFlags.Static";

    public static string BooleanLiteral(bool value)
    {
        return value ? "true" : "false";
    }

    public static string StringLiteral(string value)
    {
        return SymbolDisplay.FormatLiteral(value, true);
    }

    public static (string Namespace, string Name) GetNamespaceAndName(string fullyQualifiedType)
    {
        var type = RemoveGlobalPrefix(fullyQualifiedType);
        var lastDotIndex = type.LastIndexOf('.');

        return lastDotIndex <= 0
            ? (string.Empty, type)
            : (type.Substring(0, lastDotIndex), type.Substring(lastDotIndex + 1));
    }

    public static string GetTypeOf(string fullyQualifiedType)
    {
        return $"typeof({fullyQualifiedType})";
    }

    private static string RemoveGlobalPrefix(string fullyQualifiedType)
    {
        return fullyQualifiedType.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? fullyQualifiedType.Substring(GlobalPrefix.Length)
            : fullyQualifiedType;
    }
}

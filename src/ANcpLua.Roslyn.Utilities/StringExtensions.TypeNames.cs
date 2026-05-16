namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class StringExtensions
{
    private const string GlobalPrefix = "global::";
    private const string NullableOpenPrefix = "System.Nullable<";

    // BCL type name -> C# keyword alias. Both fully-qualified and short names
    // map to the same alias, so the table is the single source of truth — adding
    // a new alias is one Dictionary entry instead of a new switch arm.
    private static readonly Dictionary<string, string> s_csharpKeywordAliases = new(StringComparer.Ordinal)
    {
        ["System.Int32"] = "int", ["Int32"] = "int",
        ["System.Int64"] = "long", ["Int64"] = "long",
        ["System.Int16"] = "short", ["Int16"] = "short",
        ["System.Byte"] = "byte", ["Byte"] = "byte",
        ["System.SByte"] = "sbyte", ["SByte"] = "sbyte",
        ["System.UInt32"] = "uint", ["UInt32"] = "uint",
        ["System.UInt64"] = "ulong", ["UInt64"] = "ulong",
        ["System.UInt16"] = "ushort", ["UInt16"] = "ushort",
        ["System.Single"] = "float", ["Single"] = "float",
        ["System.Double"] = "double", ["Double"] = "double",
        ["System.Decimal"] = "decimal", ["Decimal"] = "decimal",
        ["System.Boolean"] = "bool", ["Boolean"] = "bool",
        ["System.String"] = "string", ["String"] = "string",
        ["System.Char"] = "char", ["Char"] = "char",
        ["System.Object"] = "object", ["Object"] = "object",
        ["System.Void"] = "void", ["Void"] = "void"
    };

    // Type names considered "primitive" for JSON serialization purposes.
    // Stored as the comparable (alias-resolved) form, so IsPrimitiveJsonType is a single lookup.
    private static readonly HashSet<string> s_primitiveJsonComparableNames =
    [
        "string", "int", "long", "bool", "double", "decimal"
    ];

    /// <summary>
    ///     Removes the <c>global::</c> prefix from a fully-qualified type name.
    /// </summary>
    /// <param name="typeFqn">The fully-qualified type name.</param>
    /// <returns>The type name without the global:: prefix.</returns>
    public static string StripGlobalPrefix(this string typeFqn)
    {
        return typeFqn.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? typeFqn[GlobalPrefix.Length..]
            : typeFqn;
    }

    /// <summary>
    ///     Normalizes a type name by removing all <c>global::</c> prefixes (including inside generics)
    ///     and trailing nullable marker.
    /// </summary>
    /// <param name="typeFqn">The fully-qualified type name.</param>
    /// <returns>The normalized type name.</returns>
    public static string NormalizeTypeName(this string typeFqn)
    {
        var result = typeFqn.Replace(GlobalPrefix, string.Empty);

        if (result.EndsWith("?", StringComparison.Ordinal))
            result = result[..^1];

        return result;
    }

    /// <summary>
    ///     Unwraps <c>Nullable&lt;T&gt;</c> or nullable reference type annotation to get the underlying type.
    /// </summary>
    /// <param name="typeFqn">The fully-qualified type name.</param>
    /// <param name="unwrap">If <c>false</c>, returns the type name unchanged.</param>
    /// <returns>The unwrapped type name, or the original if not nullable.</returns>
    public static string UnwrapNullable(this string typeFqn, bool unwrap = true)
    {
        if (!unwrap)
            return typeFqn;

        if (typeFqn.EndsWith("?", StringComparison.Ordinal))
            return typeFqn[..^1];

        var normalized = typeFqn.NormalizeTypeName();
        if (normalized.StartsWith(NullableOpenPrefix, StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
            return normalized[NullableOpenPrefix.Length..^1];

        return typeFqn;
    }

    /// <summary>
    ///     Extracts the short type name from a fully-qualified name.
    /// </summary>
    /// <param name="typeFqn">The fully-qualified type name.</param>
    /// <returns>The short type name (e.g., "List" from "System.Collections.Generic.List").</returns>
    /// <example>
    ///     <code>
    /// "global::System.Collections.Generic.List".ExtractShortTypeName() // returns "List"
    /// "int[]".ExtractShortTypeName() // returns "int[]"
    /// </code>
    /// </example>
    public static string ExtractShortTypeName(this string typeFqn)
    {
        var normalized = typeFqn.StripGlobalPrefix();

        var isArray = normalized.EndsWith("[]", StringComparison.Ordinal);
        var baseName = isArray ? normalized[..^2] : normalized;

        if (baseName.StartsWith("::", StringComparison.Ordinal))
            baseName = baseName[2..];

        var lastDot = baseName.LastIndexOf('.');
        var shortName = lastDot >= 0 ? baseName[(lastDot + 1)..] : baseName;

        return isArray ? shortName + "[]" : shortName;
    }

    /// <summary>
    ///     Gets the C# keyword alias for a BCL type name, or <c>null</c> if none exists.
    /// </summary>
    /// <param name="typeName">The type name (e.g., "System.Int32" or "Int32").</param>
    /// <returns>The C# keyword (e.g., "int"), or <c>null</c> if no keyword exists.</returns>
    public static string? GetCSharpKeyword(this string typeName)
    {
        return GetCSharpKeywordCore(typeName.NormalizeTypeName());
    }

    private static string? GetCSharpKeywordCore(string normalizedTypeName)
    {
        return s_csharpKeywordAliases.TryGetValue(normalizedTypeName, out var keyword) ? keyword : null;
    }

    private static string GetComparableTypeName(string typeName)
    {
        var normalized = typeName.NormalizeTypeName();
        return GetCSharpKeywordCore(normalized) ?? normalized;
    }

    /// <summary>
    ///     Compares two type names for equality, handling <c>global::</c> prefixes and C# keyword aliases.
    /// </summary>
    /// <param name="type1">The first type name.</param>
    /// <param name="type2">The second type name.</param>
    /// <returns><c>true</c> if the type names are equivalent; otherwise, <c>false</c>.</returns>
    public static bool TypeNamesEqual(this string type1, string type2)
    {
        return GetComparableTypeName(type1) == GetComparableTypeName(type2);
    }

    /// <summary>
    ///     Checks if the type name represents <see cref="string" />.
    /// </summary>
    /// <param name="typeFqn">The type name to check.</param>
    /// <returns><c>true</c> if the type is string; otherwise, <c>false</c>.</returns>
    public static bool IsStringType(this string typeFqn)
    {
        return typeFqn.TypeNamesEqual("string");
    }

    /// <summary>
    ///     Checks if the type name represents a primitive JSON type that doesn't need explicit registration.
    /// </summary>
    /// <param name="typeFqn">The type name to check.</param>
    /// <returns><c>true</c> if the type is a primitive JSON type; otherwise, <c>false</c>.</returns>
    public static bool IsPrimitiveJsonType(this string typeFqn)
    {
        return s_primitiveJsonComparableNames.Contains(GetComparableTypeName(typeFqn));
    }

    /// <summary>
    ///     Strips a suffix from the end of a string if present.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <returns>The string without the suffix if it was present; otherwise, the original string.</returns>
    public static string StripSuffix(this string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;
    }

    /// <summary>
    ///     Strips a prefix from the start of a string if present.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <param name="prefix">The prefix to remove.</param>
    /// <returns>The string without the prefix if it was present; otherwise, the original string.</returns>
    public static string StripPrefix(this string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..]
            : value;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Reads MSBuild-provided values surfaced through Roslyn analyzer config options.
///     MSBuild injects "virtual" keys into <see cref="AnalyzerConfigOptions" />:
///     - <c>build_property.&lt;PropertyName&gt;</c> for MSBuild properties
///     - <c>build_metadata.&lt;ItemGroup&gt;.&lt;MetadataName&gt;</c> for item metadata (commonly AdditionalFiles)
///     Conventions in this helper:
///     - Missing keys or empty/whitespace values are treated as "unset" and return <see langword="null" />.
///     - Typed helpers parse the common MSBuild forms (e.g. boolean "true/false" and "1/0").
/// </summary>
public static class AnalyzerConfigOptionsProviderExtensions
{
    private const string BuildPropertyPrefix = "build_property.";
    private const string BuildMetadataPrefix = "build_metadata.";
    private const string DefaultAdditionalFilesGroup = "AdditionalFiles";

    /// <summary>
    ///     Gets a config value by exact <paramref name="key" />. Returns <see langword="null" /> when missing or whitespace.
    /// </summary>
    public static string? GetValueOrNull(this AnalyzerConfigOptions options, string key)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    /// <summary>
    ///     Reads an MSBuild property from <see cref="AnalyzerConfigOptionsProvider.GlobalOptions" />.
    ///     Example: <c>build_property.TargetFramework</c>
    ///     With prefix: <c>build_property.MyPrefix_TargetFramework</c>
    /// </summary>
    public static string? GetGlobalProperty(this AnalyzerConfigOptionsProvider provider, string name,
        string? prefix = null)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        return name is null
            ? throw new ArgumentNullException(nameof(name))
            : provider.GlobalOptions.GetValueOrNull(BuildPropertyKey(name, prefix));
    }

    /// <summary>
    ///     Reads metadata for an <see cref="AdditionalText" /> item (default group: <c>AdditionalFiles</c>).
    ///     Example: <c>build_metadata.AdditionalFiles.MyMetadata</c>
    /// </summary>
    public static string? GetAdditionalTextMetadata(
        this AnalyzerConfigOptionsProvider provider,
        AdditionalText text,
        string name,
        string? group = null,
        string? prefix = null)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (name is null) throw new ArgumentNullException(nameof(name));

        group ??= DefaultAdditionalFilesGroup;

        return provider.GetOptions(text).GetValueOrNull(BuildMetadataKey(group, name, prefix));
    }

    /// <summary>
    ///     Reads an MSBuild property or throws when missing/unset.
    /// </summary>
    public static string GetRequiredGlobalProperty(this AnalyzerConfigOptionsProvider provider, string name,
        string? prefix = null)
    {
        return provider.GetGlobalProperty(name, prefix)
               ?? throw new InvalidOperationException($"{CompositeName(name, prefix)} MSBuild property is required.");
    }

    /// <summary>
    ///     Reads <see cref="AdditionalText" /> metadata or throws when missing/unset.
    /// </summary>
    public static string GetRequiredAdditionalTextMetadata(
        this AnalyzerConfigOptionsProvider provider,
        AdditionalText text,
        string name,
        string? group = null,
        string? prefix = null)
    {
        return provider.GetAdditionalTextMetadata(text, name, group, prefix)
               ?? throw new InvalidOperationException(
                   $"{CompositeName(name, prefix)} metadata for AdditionalText is required.");
    }

    /// <summary>
    ///     Tries to read and parse an MSBuild property as a boolean.
    ///     Accepts: "true"/"false" (case-insensitive) and "1"/"0".
    /// </summary>
    public static bool TryGetGlobalBool(this AnalyzerConfigOptionsProvider provider, string name, out bool value,
        string? prefix = null)
    {
        return TryParseMsBuildBoolean(provider.GetGlobalProperty(name, prefix), out value);
    }

    /// <summary>
    ///     Reads a boolean MSBuild property or returns <paramref name="defaultValue" /> when missing/invalid.
    /// </summary>
    public static bool GetGlobalBoolOrDefault(this AnalyzerConfigOptionsProvider provider, string name,
        bool defaultValue, string? prefix = null)
    {
        return provider.TryGetGlobalBool(name, out var value, prefix) ? value : defaultValue;
    }

    /// <summary>
    ///     Tries to read and parse an MSBuild property as an integer (invariant culture).
    /// </summary>
    public static bool TryGetGlobalInt(this AnalyzerConfigOptionsProvider provider, string name, out int value,
        string? prefix = null)
    {
        var text = provider.GetGlobalProperty(name, prefix);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    ///     Reads an integer MSBuild property or returns <paramref name="defaultValue" /> when missing/invalid.
    /// </summary>
    public static int GetGlobalIntOrDefault(this AnalyzerConfigOptionsProvider provider, string name, int defaultValue,
        string? prefix = null)
    {
        return provider.TryGetGlobalInt(name, out var value, prefix) ? value : defaultValue;
    }

    /// <summary>
    ///     True when running under an IDE design-time build.
    ///     Heuristics:
    ///     - SDK-style: DesignTimeBuild=true
    ///     - Legacy:    BuildingProject=false
    /// </summary>
    public static bool IsDesignTimeBuild(this AnalyzerConfigOptionsProvider provider)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        // If DesignTimeBuild is true => design-time
        if (TryParseMsBuildBoolean(provider.GetGlobalProperty("DesignTimeBuild"), out var isDesignTime) && isDesignTime)
            return true;

        // If BuildingProject is false => design-time (legacy signal)
        if (TryParseMsBuildBoolean(provider.GetGlobalProperty("BuildingProject"), out var isBuildingProject))
            return !isBuildingProject;

        return false;
    }

    private static string BuildPropertyKey(string name, string? prefix)
    {
        return BuildPropertyPrefix + CompositeName(name, prefix);
    }

    private static string BuildMetadataKey(string group, string name, string? prefix)
    {
        return BuildMetadataPrefix + group + "." + CompositeName(name, prefix);
    }

    private static string CompositeName(string name, string? prefix)
    {
        return prefix is null ? name : prefix + "_" + name;
    }

    private static bool TryParseMsBuildBoolean(string? text, out bool value)
    {
        if (text is null)
        {
            value = false;
            return false;
        }

        if (bool.TryParse(text, out value))
            return true;

        // MSBuild sometimes uses 1/0 for boolean-ish values
        if (text.Length == 1)
            switch (text[0])
            {
                case '1':
                    value = true;
                    return true;
                case '0':
                    value = false;
                    return true;
            }

        value = false;
        return false;
    }
}
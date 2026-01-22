using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for reading MSBuild-provided values surfaced through Roslyn analyzer config options.
/// </summary>
/// <remarks>
///     <para>
///         MSBuild injects "virtual" keys into <see cref="AnalyzerConfigOptions" />:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <c>build_property.&lt;PropertyName&gt;</c> for MSBuild properties
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>build_metadata.&lt;ItemGroup&gt;.&lt;MetadataName&gt;</c> for item metadata (commonly
///                 AdditionalFiles)
///             </description>
///         </item>
///     </list>
///     <para>
///         Conventions in this helper:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Missing keys or empty/whitespace values are treated as "unset" and return <c>null</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Typed helpers parse the common MSBuild forms (e.g. boolean "true/false" and "1/0").
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="AnalyzerConfigOptions" />
/// <seealso cref="AnalyzerConfigOptionsProvider" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AnalyzerConfigOptionsProviderExtensions
{
    private const string BuildPropertyPrefix = "build_property.";
    private const string BuildMetadataPrefix = "build_metadata.";
    private const string DefaultAdditionalFilesGroup = "AdditionalFiles";

    /// <summary>
    ///     Gets a configuration value by its exact key.
    /// </summary>
    /// <param name="options">The <see cref="AnalyzerConfigOptions" /> to read from.</param>
    /// <param name="key">The exact key to look up.</param>
    /// <returns>
    ///     The value associated with <paramref name="key" />, or <c>null</c> if the key is missing
    ///     or the value is empty or whitespace.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="options" /> or <paramref name="key" /> is <c>null</c>.
    /// </exception>
    /// <seealso cref="GetGlobalProperty" />
    /// <seealso cref="GetAdditionalTextMetadata" />
    public static string? GetValueOrNull(this AnalyzerConfigOptions options, string key)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    /// <summary>
    ///     Reads an MSBuild property from the global options.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    ///     For example, if <paramref name="prefix" /> is <c>"MyPrefix"</c> and <paramref name="name" /> is
    ///     <c>"TargetFramework"</c>,
    ///     the resulting key is <c>build_property.MyPrefix_TargetFramework</c>.
    /// </param>
    /// <returns>
    ///     The value of the MSBuild property, or <c>null</c> if not found or empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="provider" /> or <paramref name="name" /> is <c>null</c>.
    /// </exception>
    /// <seealso cref="GetRequiredGlobalProperty" />
    /// <seealso cref="TryGetGlobalBool" />
    /// <seealso cref="TryGetGlobalInt" />
    public static string? GetGlobalProperty(this AnalyzerConfigOptionsProvider provider, string name,
        string? prefix = null)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        return name is null
            ? throw new ArgumentNullException(nameof(name))
            : provider.GlobalOptions.GetValueOrNull(BuildPropertyKey(name, prefix));
    }

    /// <summary>
    ///     Reads metadata for an <see cref="AdditionalText" /> item.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="text">The <see cref="AdditionalText" /> file to get metadata for.</param>
    /// <param name="name">The metadata name (without the <c>build_metadata.</c> prefix).</param>
    /// <param name="group">
    ///     The item group name. Defaults to <c>"AdditionalFiles"</c> if not specified.
    /// </param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>
    ///     The metadata value, or <c>null</c> if not found or empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="provider" />, <paramref name="text" />, or <paramref name="name" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     The resulting key format is <c>build_metadata.{group}.{prefix}_{name}</c> when prefix is specified,
    ///     or <c>build_metadata.{group}.{name}</c> otherwise.
    /// </remarks>
    /// <seealso cref="GetRequiredAdditionalTextMetadata" />
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
    ///     Reads an MSBuild property or throws when missing or unset.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>The value of the MSBuild property.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the property is not found, empty, or whitespace.
    /// </exception>
    /// <seealso cref="GetGlobalProperty" />
    public static string GetRequiredGlobalProperty(this AnalyzerConfigOptionsProvider provider, string name,
        string? prefix = null) =>
        provider.GetGlobalProperty(name, prefix)
        ?? throw new InvalidOperationException($"{CompositeName(name, prefix)} MSBuild property is required.");

    /// <summary>
    ///     Reads <see cref="AdditionalText" /> metadata or throws when missing or unset.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="text">The <see cref="AdditionalText" /> file to get metadata for.</param>
    /// <param name="name">The metadata name (without the <c>build_metadata.</c> prefix).</param>
    /// <param name="group">
    ///     The item group name. Defaults to <c>"AdditionalFiles"</c> if not specified.
    /// </param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>The metadata value.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the metadata is not found, empty, or whitespace.
    /// </exception>
    /// <seealso cref="GetAdditionalTextMetadata" />
    public static string GetRequiredAdditionalTextMetadata(
        this AnalyzerConfigOptionsProvider provider,
        AdditionalText text,
        string name,
        string? group = null,
        string? prefix = null) =>
        provider.GetAdditionalTextMetadata(text, name, group, prefix)
        ?? throw new InvalidOperationException(
            $"{CompositeName(name, prefix)} metadata for AdditionalText is required.");

    /// <summary>
    ///     Tries to read and parse an MSBuild property as a boolean.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the parsed boolean value.
    ///     When this method returns <c>false</c>, contains <c>false</c>.
    /// </param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the property was found and successfully parsed as a boolean; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Accepts the following values (case-insensitive):
    ///     <list type="bullet">
    ///         <item>
    ///             <description><c>"true"</c> or <c>"1"</c> for <c>true</c></description>
    ///         </item>
    ///         <item>
    ///             <description><c>"false"</c> or <c>"0"</c> for <c>false</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GetGlobalBoolOrDefault" />
    public static bool TryGetGlobalBool(this AnalyzerConfigOptionsProvider provider, string name, out bool value,
        string? prefix = null) =>
        TryParseMsBuildBoolean(provider.GetGlobalProperty(name, prefix), out value);

    /// <summary>
    ///     Reads a boolean MSBuild property or returns a default value when missing or invalid.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="defaultValue">The value to return if the property is missing or cannot be parsed.</param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>
    ///     The parsed boolean value, or <paramref name="defaultValue" /> if the property is missing or invalid.
    /// </returns>
    /// <seealso cref="TryGetGlobalBool" />
    public static bool GetGlobalBoolOrDefault(this AnalyzerConfigOptionsProvider provider, string name,
        bool defaultValue, string? prefix = null) =>
        provider.TryGetGlobalBool(name, out var value, prefix) ? value : defaultValue;

    /// <summary>
    ///     Tries to read and parse an MSBuild property as an integer.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the parsed integer value.
    ///     When this method returns <c>false</c>, contains <c>0</c>.
    /// </param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the property was found and successfully parsed as an integer; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Parsing uses <see cref="CultureInfo.InvariantCulture" /> and <see cref="NumberStyles.Integer" />.
    /// </remarks>
    /// <seealso cref="GetGlobalIntOrDefault" />
    public static bool TryGetGlobalInt(this AnalyzerConfigOptionsProvider provider, string name, out int value,
        string? prefix = null)
    {
        var text = provider.GetGlobalProperty(name, prefix);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    ///     Reads an integer MSBuild property or returns a default value when missing or invalid.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <param name="name">The MSBuild property name (without the <c>build_property.</c> prefix).</param>
    /// <param name="defaultValue">The value to return if the property is missing or cannot be parsed.</param>
    /// <param name="prefix">
    ///     An optional prefix to prepend to <paramref name="name" /> with an underscore separator.
    /// </param>
    /// <returns>
    ///     The parsed integer value, or <paramref name="defaultValue" /> if the property is missing or invalid.
    /// </returns>
    /// <seealso cref="TryGetGlobalInt" />
    public static int GetGlobalIntOrDefault(this AnalyzerConfigOptionsProvider provider, string name, int defaultValue,
        string? prefix = null) =>
        provider.TryGetGlobalInt(name, out var value, prefix) ? value : defaultValue;

    /// <summary>
    ///     Determines whether the current build is an IDE design-time build.
    /// </summary>
    /// <param name="provider">The <see cref="AnalyzerConfigOptionsProvider" /> to read from.</param>
    /// <returns>
    ///     <c>true</c> if the build is a design-time build; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="provider" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Uses the following heuristics to detect design-time builds:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 SDK-style projects: <c>DesignTimeBuild=true</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Legacy projects: <c>BuildingProject=false</c>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
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

    private static string BuildPropertyKey(string name, string? prefix) => BuildPropertyPrefix + CompositeName(name, prefix);

    private static string BuildMetadataKey(string group, string name, string? prefix) => BuildMetadataPrefix + group + "." + CompositeName(name, prefix);

    private static string CompositeName(string name, string? prefix) => prefix is null ? name : prefix + "_" + name;

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

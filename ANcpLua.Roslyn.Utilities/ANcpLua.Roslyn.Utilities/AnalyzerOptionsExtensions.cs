using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides fluent extension methods for <see cref="AnalyzerOptions" /> to access configuration values.
/// </summary>
/// <remarks>
///     <para>
///         These extensions simplify reading configuration values from <c>.editorconfig</c> files and other
///         analyzer configuration sources in Roslyn analyzers and source generators.
///     </para>
///     <list type="bullet">
///         <item><description>Type-safe access to boolean, integer, string, and enum configuration values</description></item>
///         <item><description>Automatic fallback to default values when configuration is missing or unparseable</description></item>
///         <item><description>Syntax tree-scoped configuration lookup for file-specific settings</description></item>
///     </list>
/// </remarks>
/// <seealso cref="AnalyzerConfigOptionsProviderExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class AnalyzerOptionsExtensions
{
    /// <summary>
    ///     Tries to get a configuration value for a specific syntax tree.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="syntaxTree">The syntax tree to get configuration for.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the configuration value;
    ///     otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the configuration value was found; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="GetConfigurationValue(AnalyzerOptions, SyntaxTree, string, string)" />
    public static bool TryGetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        [NotNullWhen(true)] out string? value)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return configuration.TryGetValue(key, out value);
    }

    /// <summary>
    ///     Gets a boolean configuration value, returning the default if not found or unparseable.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="syntaxTree">The syntax tree to get configuration for.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or cannot be parsed.</param>
    /// <returns>
    ///     The parsed boolean value if found and parseable; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method supports standard boolean parsing (<c>true</c>/<c>false</c>) as well as
    ///         numeric representations (<c>1</c> for <c>true</c>, <c>0</c> for <c>false</c>).
    ///     </para>
    /// </remarks>
    /// <seealso cref="TryGetConfigurationValue" />
    public static bool GetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        bool defaultValue) =>
        !options.TryGetConfigurationValue(syntaxTree, key, out var value)
            ? defaultValue
            : ParseBoolean(value, defaultValue);

    /// <summary>
    ///     Gets an integer configuration value, returning the default if not found or unparseable.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="syntaxTree">The syntax tree to get configuration for.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or cannot be parsed.</param>
    /// <returns>
    ///     The parsed integer value if found and parseable; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Parsing uses <see cref="NumberStyles.Integer" /> with <see cref="CultureInfo.InvariantCulture" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="TryGetConfigurationValue" />
    public static int GetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        int defaultValue)
    {
        if (!options.TryGetConfigurationValue(syntaxTree, key, out var value))
            return defaultValue;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    ///     Gets a string configuration value, returning the default if not found.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="syntaxTree">The syntax tree to get configuration for.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>
    ///     The configuration value if found; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <seealso cref="TryGetConfigurationValue" />
    public static string GetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        string defaultValue) =>
        options.TryGetConfigurationValue(syntaxTree, key, out var value)
            ? value
            : defaultValue;

    /// <summary>
    ///     Gets an enum configuration value, returning the default if not found or unparseable.
    /// </summary>
    /// <typeparam name="T">The enum type to parse the value as.</typeparam>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="syntaxTree">The syntax tree to get configuration for.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or cannot be parsed.</param>
    /// <returns>
    ///     The parsed enum value if found and parseable; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Parsing is case-insensitive to allow flexible configuration file formatting.
    ///     </para>
    /// </remarks>
    /// <seealso cref="TryGetConfigurationValue" />
    public static T GetConfigurationValue<T>(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        T defaultValue) where T : struct, Enum
    {
        if (!options.TryGetConfigurationValue(syntaxTree, key, out var value))
            return defaultValue;

        return Enum.TryParse<T>(value, true, out var result)
            ? result
            : defaultValue;
    }

    private static bool ParseBoolean(string value, bool defaultValue)
    {
        if (bool.TryParse(value, out var result))
            return result;

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => defaultValue
        };
    }
}

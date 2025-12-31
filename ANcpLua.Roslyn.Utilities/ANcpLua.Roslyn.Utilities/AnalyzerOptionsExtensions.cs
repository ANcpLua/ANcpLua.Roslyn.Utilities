using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Fluent extension methods for <see cref="AnalyzerOptions" /> to access configuration values.
/// </summary>
public static class AnalyzerOptionsExtensions
{
    /// <summary>
    ///     Tries to get a configuration value for a specific syntax tree.
    /// </summary>
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
    public static bool GetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        bool defaultValue)
    {
        if (!options.TryGetConfigurationValue(syntaxTree, key, out var value))
            return defaultValue;

        return ParseBoolean(value, defaultValue);
    }

    /// <summary>
    ///     Gets an integer configuration value, returning the default if not found or unparseable.
    /// </summary>
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
    public static string GetConfigurationValue(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        string defaultValue)
    {
        return options.TryGetConfigurationValue(syntaxTree, key, out var value)
            ? value
            : defaultValue;
    }

    /// <summary>
    ///     Gets an enum configuration value, returning the default if not found or unparseable.
    /// </summary>
    public static T GetConfigurationValue<T>(
        this AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key,
        T defaultValue) where T : struct, Enum
    {
        if (!options.TryGetConfigurationValue(syntaxTree, key, out var value))
            return defaultValue;

        return Enum.TryParse<T>(value, ignoreCase: true, out var result)
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

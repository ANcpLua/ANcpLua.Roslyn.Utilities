using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="AnalyzerConfigOptionsProvider" /> to retrieve MSBuild properties.
/// </summary>
public static class AnalyzerConfigOptionsProviderExtensions
{
    private static string GetFullName(string name, string? prefix = null)
    {
        return prefix == null
            ? name
            : $"{prefix}_{name}";
    }

    /// <summary>
    ///     Returns the value of the global option, or null if the option is missing or an empty string.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string? GetOption(
        this AnalyzerConfigOptions options,
        string key)
    {
        options = options ?? throw new ArgumentNullException(nameof(options));
        key = key ?? throw new ArgumentNullException(nameof(key));

        return
            options.TryGetValue(key, out var result) &&
            !string.IsNullOrWhiteSpace(result)
                ? result
                : null;
    }

    /// <param name="provider"></param>
    extension(AnalyzerConfigOptionsProvider provider)
    {
        /// <summary>
        ///     Returns the value of the global option, or null if the option is missing or an empty string.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string? GetGlobalOption(string name,
            string? prefix = null)
        {
            provider = provider ?? throw new ArgumentNullException(nameof(provider));
            name = name ?? throw new ArgumentNullException(nameof(name));

            return provider.GlobalOptions.GetOption($"build_property.{GetFullName(name, prefix)}");
        }

        /// <summary>
        ///     Returns the value of the <see cref="AdditionalText" /> option, or null if the option is missing or an empty string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        /// <param name="group">Default: AdditionalFiles</param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string? GetOption(AdditionalText text,
            string name,
            string? group = null,
            string? prefix = null)
        {
            provider = provider ?? throw new ArgumentNullException(nameof(provider));
            name = name ?? throw new ArgumentNullException(nameof(name));
            group ??= "AdditionalFiles";

            return provider.GetOptions(text).GetOption($"build_metadata.{group}.{GetFullName(name, prefix)}");
        }

        /// <summary>
        ///     Returns the value of the global option, or throws an <see cref="InvalidOperationException" />.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GetRequiredGlobalOption(string name,
            string? prefix = null)
        {
            return
                provider.GetGlobalOption(name, prefix) ??
                throw new InvalidOperationException($"{GetFullName(name, prefix)} MSBuild property is required.");
        }

        /// <summary>
        ///     Returns the value of the <see cref="AdditionalText" /> option, or throws an
        ///     <see cref="InvalidOperationException" />.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GetRequiredOption(AdditionalText text,
            string name,
            string? prefix = null)
        {
            return
                provider.GetOption(text, name, prefix) ??
                throw new InvalidOperationException(
                    $"{GetFullName(name, prefix)} metadata for AdditionalText is required.");
        }

        /// <summary>
        ///     Returns true if generator running in design-time.
        /// </summary>
        /// <returns></returns>
        public bool IsDesignTime()
        {
            var isBuildingProjectValue = provider.GetGlobalOption("BuildingProject"); // legacy projects
            var isDesignTimeBuildValue = provider.GetGlobalOption("DesignTimeBuild"); // sdk-style projects

            return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
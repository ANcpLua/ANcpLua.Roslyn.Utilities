// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.
// Portions from https://github.com/Sergio0694/PolySharp

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

    #region AnalyzerConfigOptions extensions

    /// <summary>
    ///     Returns the value of the option, or null if the option is missing or an empty string.
    /// </summary>
    /// <param name="options">The analyzer config options.</param>
    /// <param name="key">The option key.</param>
    /// <returns>The option value or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options or key is null.</exception>
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

    #endregion

    #region AnalyzerConfigOptionsProvider extensions

    /// <summary>
    ///     Returns the value of the global option, or null if the option is missing or an empty string.
    /// </summary>
    /// <param name="provider">The analyzer config options provider.</param>
    /// <param name="name">The option name.</param>
    /// <param name="prefix">Optional prefix.</param>
    /// <returns>The option value or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when provider or name is null.</exception>
    public static string? GetGlobalOption(
        this AnalyzerConfigOptionsProvider provider,
        string name,
        string? prefix = null)
    {
        provider = provider ?? throw new ArgumentNullException(nameof(provider));
        name = name ?? throw new ArgumentNullException(nameof(name));

        return provider.GlobalOptions.GetOption($"build_property.{GetFullName(name, prefix)}");
    }

    /// <summary>
    ///     Returns the value of the <see cref="AdditionalText" /> option, or null if missing or empty.
    /// </summary>
    /// <param name="provider">The analyzer config options provider.</param>
    /// <param name="text">The additional text file.</param>
    /// <param name="name">The option name.</param>
    /// <param name="group">Default: AdditionalFiles.</param>
    /// <param name="prefix">Optional prefix.</param>
    /// <returns>The option value or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when provider or name is null.</exception>
    public static string? GetOption(
        this AnalyzerConfigOptionsProvider provider,
        AdditionalText text,
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
    /// <param name="provider">The analyzer config options provider.</param>
    /// <param name="name">The option name.</param>
    /// <param name="prefix">Optional prefix.</param>
    /// <returns>The required option value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the option is missing.</exception>
    public static string GetRequiredGlobalOption(
        this AnalyzerConfigOptionsProvider provider,
        string name,
        string? prefix = null)
    {
        return
            provider.GetGlobalOption(name, prefix) ??
            throw new InvalidOperationException($"{GetFullName(name, prefix)} MSBuild property is required.");
    }

    /// <summary>
    ///     Returns the value of the <see cref="AdditionalText" /> option, or throws an exception.
    /// </summary>
    /// <param name="provider">The analyzer config options provider.</param>
    /// <param name="text">The additional text file.</param>
    /// <param name="name">The option name.</param>
    /// <param name="prefix">Optional prefix.</param>
    /// <returns>The required option value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the option is missing.</exception>
    public static string GetRequiredOption(
        this AnalyzerConfigOptionsProvider provider,
        AdditionalText text,
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
    /// <param name="provider">The analyzer config options provider.</param>
    /// <returns>True if running in design-time build.</returns>
    public static bool IsDesignTime(this AnalyzerConfigOptionsProvider provider)
    {
        var isBuildingProjectValue = provider.GetGlobalOption("BuildingProject"); // legacy projects
        var isDesignTimeBuildValue = provider.GetGlobalOption("DesignTimeBuild"); // sdk-style projects

        return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
               || string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Checks whether the input property has a valid <see cref="bool" /> value.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider" /> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <param name="propertyValue">The resulting property value, if invalid.</param>
    /// <returns>Whether the target property is a valid <see cref="bool" /> value.</returns>
    public static bool IsValidMSBuildProperty(
        this AnalyzerConfigOptionsProvider options,
        string propertyName,
        [NotNullWhen(false)] out string? propertyValue)
    {
        return
            !options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out propertyValue) ||
            string.Equals(propertyValue, string.Empty, StringComparison.Ordinal) ||
            string.Equals(propertyValue, bool.TrueString, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyValue, bool.FalseString, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Gets the value of a <see cref="bool" /> MSBuild property.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider" /> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <returns>The value of the specified MSBuild property.</returns>
    public static bool GetBoolMSBuildProperty(this AnalyzerConfigOptionsProvider options, string propertyName)
    {
        return
            options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue) &&
            string.Equals(propertyValue, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Gets the value of an MSBuild property representing a semicolon-separated list of strings.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider" /> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <returns>The value of the specified MSBuild property.</returns>
    public static ImmutableArray<string> GetStringArrayMSBuildProperty(
        this AnalyzerConfigOptionsProvider options,
        string propertyName)
    {
        if (options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue))
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            foreach (string part in propertyValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    builder.Add(trimmed);
                }
            }

            return builder.ToImmutable();
        }

        return [];
    }

    #endregion
}

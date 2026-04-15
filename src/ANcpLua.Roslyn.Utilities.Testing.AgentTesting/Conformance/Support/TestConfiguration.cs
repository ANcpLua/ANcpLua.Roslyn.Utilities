// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — Support/TestConfiguration.cs

using Microsoft.Extensions.Configuration;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Support;

/// <summary>
/// Helper for loading test configuration settings.
/// Reads from <c>testsettings.development.json</c> (optional), environment variables, then UserSecrets.
/// </summary>
public sealed class TestConfiguration
{
    private static readonly IConfiguration s_configuration = new ConfigurationBuilder()
        .AddJsonFile(path: "testsettings.development.json", optional: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<TestConfiguration>()
        .Build();

    /// <summary>Gets a configuration value by its flat key name.</summary>
    public static string? GetValue(string key) => s_configuration[key];

    /// <summary>Gets a required configuration value by its flat key name.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the configuration value is not found.</exception>
    public static string GetRequiredValue(string key) =>
        s_configuration[key] ?? throw new InvalidOperationException($"Configuration key '{key}' is required but was not found.");
}

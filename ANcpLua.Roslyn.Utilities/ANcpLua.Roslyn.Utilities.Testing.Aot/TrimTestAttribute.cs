using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks a test method for trimming validation using PublishTrimmed=true.
/// </summary>
/// <remarks>
/// The test method must return an int where 0 indicates success.
/// This attribute uses PublishTrimmed=true for testing trimming support, not AOT compilation.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TrimTestAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the platform on which this test should be skipped.
    /// Format: "win", "linux", "osx", etc.
    /// </summary>
    public string? SkipOnPlatform { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier for which this test should run.
    /// Example: "win-x64", "linux-arm64", etc.
    /// </summary>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the feature switches to disable during trimming.
    /// </summary>
    public string[]? DisabledFeatureSwitches { get; set; }

    /// <summary>
    /// Gets or sets the trimming mode for this test.
    /// Default: TrimMode.Full
    /// </summary>
    public TrimMode TrimMode { get; set; } = TrimMode.Full;

    /// <summary>
    /// Gets or sets the build configuration for this test.
    /// Default: "Release"
    /// </summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the timeout in seconds for this test.
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

using System;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Marks a method as an AOT (Ahead-of-Time) test.
/// </summary>
/// <remarks>
/// <para>
/// The decorated method must return <see cref="int"/>, with return value 0 indicating success.
/// </para>
/// <para>
/// The method may be static or an instance method on a class with a parameterless constructor.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AotTestAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the platform on which this test should be skipped.
    /// </summary>
    /// <remarks>
    /// Valid values: "osx", "windows", "linux". If null, the test runs on all platforms.
    /// </remarks>
    public string? SkipOnPlatform { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier (RID) for this test.
    /// </summary>
    /// <remarks>
    /// Examples: "win-x64", "osx-arm64", "linux-x64".
    /// If null, the RID is auto-detected from the current runtime.
    /// </remarks>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the array of feature switches to disable for this test.
    /// </summary>
    /// <remarks>
    /// Use constants from <see cref="FeatureSwitches"/> class. If null or empty, no switches are disabled.
    /// </remarks>
    public string[]? DisabledFeatureSwitches { get; set; }

    /// <summary>
    /// Gets or sets the build configuration for this test.
    /// </summary>
    /// <remarks>
    /// Default is "Release". Common values: "Release", "Debug".
    /// </remarks>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// Gets or sets the timeout in seconds for publishing and executing this test.
    /// </summary>
    /// <remarks>
    /// Default is 300 seconds (5 minutes).
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 300;
}

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Feature switch constants for AOT (Ahead-of-Time) compilation testing.
/// These constants represent the names of runtime feature switches that control
/// the availability of various .NET runtime features when AOT compilation is used.
/// </summary>
public static class FeatureSwitches
{
    /// <summary>
    /// Gets the feature switch name for controlling JSON reflection support in System.Text.Json.
    /// When set to <c>false</c>, the JSON serializer will not use reflection for type handling.
    /// </summary>
    public const string JsonReflection = "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault";

    /// <summary>
    /// Gets the feature switch name for controlling debugger support.
    /// When set to <c>false</c>, debugger functionality is not available at runtime.
    /// </summary>
    public const string DebuggerSupport = "System.Diagnostics.Debugger.IsSupported";

    /// <summary>
    /// Gets the feature switch name for controlling EventSource support.
    /// When set to <c>false</c>, EventSource functionality is disabled at runtime.
    /// </summary>
    public const string EventSourceSupport = "System.Diagnostics.Tracing.EventSource.IsSupported";

    /// <summary>
    /// Gets the feature switch name for controlling Metrics support.
    /// When set to <c>false</c>, diagnostic metrics functionality is not available.
    /// </summary>
    public const string MetricsSupport = "System.Diagnostics.Metrics.Meter.IsSupported";

    /// <summary>
    /// Gets the feature switch name for controlling XML serialization support.
    /// When set to <c>false</c>, XmlSerializer functionality is disabled.
    /// </summary>
    public const string XmlSerialization = "System.Xml.XmlSerializer.IsEnabled";

    /// <summary>
    /// Gets the feature switch name for controlling unsafe binary formatter support.
    /// When set to <c>false</c>, binary formatter serialization is disabled for security.
    /// </summary>
    public const string BinaryFormatter = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

    /// <summary>
    /// Gets the feature switch name for controlling invariant globalization mode.
    /// When set to <c>true</c>, the runtime uses invariant culture behavior only.
    /// </summary>
    public const string InvariantGlobalization = "System.Globalization.Invariant";

    /// <summary>
    /// Gets the feature switch name for controlling HTTP activity propagation.
    /// When set to <c>false</c>, HTTP client requests will not propagate distributed tracing context.
    /// </summary>
    public const string HttpActivityPropagation = "System.Net.Http.EnableActivityPropagation";

    /// <summary>
    /// Gets the feature switch name for controlling startup hooks support.
    /// When set to <c>false</c>, startup hooks cannot be used for initialization.
    /// </summary>
    public const string StartupHooks = "System.StartupHookProvider.IsSupported";
}

using System;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Provides runtime detection for AOT compilation mode.
/// </summary>
public static class AotRuntime
{
    private static readonly bool s_isDynamicCodeSupported = CheckDynamicCodeSupport();

    /// <summary>
    /// Gets a value indicating whether the application is running as Native AOT.
    /// Returns <c>true</c> when dynamic code generation is not supported (AOT mode),
    /// <c>false</c> when running with JIT compilation.
    /// </summary>
    /// <remarks>
    /// This property wraps <c>RuntimeFeature.IsDynamicCodeSupported</c>.
    /// In Native AOT, dynamic code generation (Reflection.Emit, etc.) is not available.
    /// </remarks>
    public static bool IsNativeAot => !s_isDynamicCodeSupported;

    /// <summary>
    /// Gets a value indicating whether the runtime supports dynamic code generation.
    /// Returns <c>true</c> for JIT mode, <c>false</c> for Native AOT.
    /// </summary>
    public static bool IsDynamicCodeSupported => s_isDynamicCodeSupported;

    private static bool CheckDynamicCodeSupport()
    {
        // RuntimeFeature.IsDynamicCodeSupported is available in .NET Core 3.0+
        // For older runtimes, we assume dynamic code is supported (JIT mode)
        var runtimeFeatureType = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.RuntimeFeature");
        if (runtimeFeatureType == null)
            return true; // Older runtime, assume JIT

        var prop = runtimeFeatureType.GetProperty("IsDynamicCodeSupported", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (prop == null)
            return true; // Property doesn't exist, assume JIT

        return (bool)(prop.GetValue(null) ?? true);
    }
}

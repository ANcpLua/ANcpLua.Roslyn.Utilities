namespace ANcpLua.Roslyn.Utilities.Testing.Aot;

/// <summary>
/// Specifies the trim mode for AOT testing.
/// </summary>
public enum TrimMode
{
    /// <summary>
    /// Only trim assemblies marked with IsTrimmable=true.
    /// </summary>
    Partial = 0,

    /// <summary>
    /// Trim all assemblies (default in .NET 8+).
    /// </summary>
    Full = 1
}

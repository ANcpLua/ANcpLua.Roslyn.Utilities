using System.Diagnostics;
using System.Reflection;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for extracting version information from assemblies.
/// </summary>
/// <remarks>
///     <para>
///         Strips build metadata (the git SHA after the <c>+</c> sign in informational versions)
///         for cleaner version strings in telemetry. For example:
///         <c>1.5.0-alpha.1.40+807f703e1b4d9874a92bd86d9f2d4ebe5b5d52e4</c> becomes
///         <c>1.5.0-alpha.1.40</c>.
///     </para>
///     <para>
///         Based on the pattern from <c>opentelemetry-dotnet-contrib</c>.
///     </para>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AssemblyVersionExtensions
{
    /// <summary>
    ///     Gets the package version from the assembly's informational version attribute.
    /// </summary>
    /// <param name="assembly">The assembly to read version information from.</param>
    /// <returns>
    ///     The informational version with build metadata (after <c>+</c>) stripped.
    /// </returns>
    /// <remarks>
    ///     Asserts that the assembly has an <see cref="AssemblyInformationalVersionAttribute" />.
    ///     Use <see cref="TryGetPackageVersion" /> when the attribute may be absent.
    /// </remarks>
    public static string GetPackageVersion(this Assembly assembly)
    {
        Debug.Assert(assembly is not null, "assembly was null");
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Debug.Assert(!string.IsNullOrEmpty(informationalVersion),
            "AssemblyInformationalVersionAttribute was not found");
        return ParsePackageVersion(informationalVersion!);
    }

    /// <summary>
    ///     Tries to get the package version from the assembly's informational version attribute.
    /// </summary>
    /// <param name="assembly">The assembly to read version information from.</param>
    /// <param name="packageVersion">
    ///     When this method returns <c>true</c>, contains the informational version with build
    ///     metadata stripped; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the assembly has a non-empty informational version; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetPackageVersion(this Assembly assembly,
        [NotNullWhen(true)] out string? packageVersion)
    {
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informationalVersion))
        {
            packageVersion = null;
            return false;
        }

        packageVersion = ParsePackageVersion(informationalVersion!);
        return true;
    }

    /// <summary>
    ///     Strips build metadata (the portion after <c>+</c>) from an informational version string.
    /// </summary>
    private static string ParsePackageVersion(string informationalVersion)
    {
        var indexOfPlusSign = informationalVersion.IndexOf('+');
        return indexOfPlusSign > 0
            ? informationalVersion.Substring(0, indexOfPlusSign)
            : informationalVersion;
    }
}

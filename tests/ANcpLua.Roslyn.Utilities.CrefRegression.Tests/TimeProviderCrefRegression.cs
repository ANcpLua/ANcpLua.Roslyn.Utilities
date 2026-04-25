// Locks the fix from commit "fix(polyfill): rename TimeProvider shim to TimeProviderShim".
// If `internal class TimeProvider` ever returns to namespace System inside the lib DLL,
// this cref resolves ambiguously (BCL System.TimeProvider + lib's leaked type) and CS0419
// fires. With TreatWarningsAsErrors=true (inherited) + GenerateDocumentationFile=true (set
// in the csproj), CS0419 becomes a build break — CI regression locked in.

namespace ANcpLua.Roslyn.Utilities.CrefRegression.Tests;

/// <summary>
///     Compile-time witness. Touching this type forces the C# compiler to resolve the cref
///     against both the BCL and any types reachable via referenced assemblies, including
///     the lib DLL.
/// </summary>
public static class TimeProviderCrefWitness
{
    /// <summary>
    ///     Returns <see cref="TimeProvider" />.System for the caller. The cref must resolve
    ///     unambiguously to <c>System.TimeProvider</c> from the BCL.
    /// </summary>
    /// <returns>The system <see cref="TimeProvider" />.</returns>
    public static TimeProvider GetSystemTimeProvider() => TimeProvider.System;
}

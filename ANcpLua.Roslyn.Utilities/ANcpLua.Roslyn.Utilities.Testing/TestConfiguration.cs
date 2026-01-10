using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Thread-safe configuration settings for generator tests.
/// </summary>
/// <remarks>
///     <para>
///         All mutable configuration uses <see cref="AsyncLocal{T}" /> to ensure thread safety
///         during parallel test execution. Each test thread maintains its own configuration state.
///     </para>
///     <para>
///         Use <see cref="WithLanguageVersion" />, <see cref="WithReferenceAssemblies" />, or
///         <see cref="WithAdditionalReferences" /> to temporarily override settings within a scope.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Override language version for a specific test
/// using (TestConfiguration.WithLanguageVersion(LanguageVersion.CSharp11))
/// {
///     await source.ShouldGenerate&lt;MyGenerator&gt;("Output.g.cs", expected);
/// }
/// </code>
/// </example>
public static class TestConfiguration
{
    private static readonly ReferenceAssemblies Net100Assemblies = new(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"),
        Path.Combine("ref", "net10.0"));

    private static readonly AsyncLocal<LanguageVersion?> LanguageVersionOverride = new();
    private static readonly AsyncLocal<ReferenceAssemblies?> ReferenceAssembliesOverride = new();

    private static readonly AsyncLocal<ImmutableArray<PortableExecutableReference>?> AdditionalReferencesOverride =
        new();

    /// <summary>
    ///     The C# language version to use for tests. Thread-safe via AsyncLocal.
    /// </summary>
    public static LanguageVersion LanguageVersion =>
        LanguageVersionOverride.Value ?? LanguageVersion.Preview;

    /// <summary>
    ///     The reference assemblies to use for tests (.NET 10). Thread-safe via AsyncLocal.
    /// </summary>
    public static ReferenceAssemblies ReferenceAssemblies =>
        ReferenceAssembliesOverride.Value ?? Net100Assemblies;

    /// <summary>
    ///     Additional references to include in compilations. Thread-safe via AsyncLocal.
    /// </summary>
    public static ImmutableArray<PortableExecutableReference> AdditionalReferences =>
        AdditionalReferencesOverride.Value ?? ImmutableArray<PortableExecutableReference>.Empty;

    /// <summary>
    ///     Creates a scope that temporarily overrides the language version.
    /// </summary>
    /// <param name="version">The language version to use within the scope.</param>
    /// <returns>A disposable that restores the previous value when disposed.</returns>
    public static IDisposable WithLanguageVersion(LanguageVersion version)
    {
        var previous = LanguageVersionOverride.Value;
        LanguageVersionOverride.Value = version;
        return new ConfigurationScope(() => LanguageVersionOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily overrides the reference assemblies.
    /// </summary>
    /// <param name="assemblies">The reference assemblies to use within the scope.</param>
    /// <returns>A disposable that restores the previous value when disposed.</returns>
    public static IDisposable WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        var previous = ReferenceAssembliesOverride.Value;
        ReferenceAssembliesOverride.Value = assemblies;
        return new ConfigurationScope(() => ReferenceAssembliesOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily sets additional references.
    /// </summary>
    /// <param name="references">The additional references to include in compilations.</param>
    /// <returns>A disposable that restores the previous value when disposed.</returns>
    public static IDisposable WithAdditionalReferences(ImmutableArray<PortableExecutableReference> references)
    {
        var previous = AdditionalReferencesOverride.Value;
        AdditionalReferencesOverride.Value = references;
        return new ConfigurationScope(() => AdditionalReferencesOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily sets additional references from types.
    /// </summary>
    /// <param name="types">Types whose assemblies should be included as references.</param>
    /// <returns>A disposable that restores the previous value when disposed.</returns>
    public static IDisposable WithAdditionalReferences(params Type[] types)
    {
        var references = types
            .Select(t => t.Assembly.Location)
            .Where(loc => !string.IsNullOrEmpty(loc))
            .Distinct()
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray();

        return WithAdditionalReferences(references);
    }

    private sealed class ConfigurationScope(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _restore();
        }
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides thread-safe configuration settings for Roslyn incremental generator tests.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TestConfiguration" /> is designed for parallel test execution. All mutable configuration
///         uses <see cref="AsyncLocal{T}" /> to ensure each test thread maintains its own isolated configuration state.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="LanguageVersion" /> - Controls the C# language version for test compilations
///                 (defaults to <see cref="Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview" />).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ReferenceAssemblies" /> - Specifies the reference assemblies for test compilations
///                 (defaults to .NET 10.0).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="AdditionalReferences" /> - Additional <see cref="PortableExecutableReference" /> instances
///                 to include in compilations (defaults to empty).
///             </description>
///         </item>
///     </list>
///     <para>
///         Use the <c>With*</c> methods to temporarily override settings within a <c>using</c> scope.
///         The original values are automatically restored when the scope is disposed.
///     </para>
/// </remarks>
/// <example>
///     <para>Override language version for a specific test:</para>
///     <code>
/// using (TestConfiguration.WithLanguageVersion(LanguageVersion.CSharp11))
/// {
///     await source.ShouldGenerate&lt;MyGenerator&gt;("Output.g.cs", expected);
/// }
/// // Original language version is restored here
/// </code>
/// </example>
/// <example>
///     <para>Add additional type references for a test:</para>
///     <code>
/// using (TestConfiguration.WithAdditionalReferences(typeof(MyAttribute)))
/// {
///     var result = await Test&lt;MyGenerator&gt;.Run(source);
///     result.IsClean().Verify();
/// }
/// </code>
/// </example>
/// <seealso cref="GeneratorTestEngine{TGenerator}" />
/// <seealso cref="Test{TGenerator}" />
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
    ///     Gets the C# language version to use for test compilations.
    /// </summary>
    /// <value>
    ///     The configured <see cref="Microsoft.CodeAnalysis.CSharp.LanguageVersion" />, or
    ///     <see cref="Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview" /> if not overridden.
    /// </value>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Thread-safe via <see cref="AsyncLocal{T}" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use <see cref="WithLanguageVersion" /> to temporarily override this value.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="WithLanguageVersion" />
    public static LanguageVersion LanguageVersion =>
        LanguageVersionOverride.Value ?? LanguageVersion.Preview;

    /// <summary>
    ///     Gets the reference assemblies to use for test compilations.
    /// </summary>
    /// <value>
    ///     The configured <see cref="Microsoft.CodeAnalysis.Testing.ReferenceAssemblies" />, or
    ///     .NET 10.0 reference assemblies if not overridden.
    /// </value>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Thread-safe via <see cref="AsyncLocal{T}" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use <see cref="WithReferenceAssemblies" /> to temporarily override this value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The default .NET 10.0 assemblies use the <c>Microsoft.NETCore.App.Ref</c> package.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="WithReferenceAssemblies" />
    public static ReferenceAssemblies ReferenceAssemblies =>
        ReferenceAssembliesOverride.Value ?? Net100Assemblies;

    /// <summary>
    ///     Gets additional metadata references to include in test compilations.
    /// </summary>
    /// <value>
    ///     The configured additional <see cref="PortableExecutableReference" /> instances, or
    ///     an empty array if not overridden.
    /// </value>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Thread-safe via <see cref="AsyncLocal{T}" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use <see cref="WithAdditionalReferences(ImmutableArray{PortableExecutableReference})" />
    ///                 or <see cref="WithAdditionalReferences(Type[])" /> to temporarily override this value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 These references are added on top of <see cref="ReferenceAssemblies" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="WithAdditionalReferences(ImmutableArray{PortableExecutableReference})" />
    /// <seealso cref="WithAdditionalReferences(Type[])" />
    public static ImmutableArray<PortableExecutableReference> AdditionalReferences =>
        AdditionalReferencesOverride.Value ?? ImmutableArray<PortableExecutableReference>.Empty;

    /// <summary>
    ///     Creates a scope that temporarily overrides the C# language version for test compilations.
    /// </summary>
    /// <param name="version">
    ///     The <see cref="Microsoft.CodeAnalysis.CSharp.LanguageVersion" /> to use within the scope.
    /// </param>
    /// <returns>
    ///     An <see cref="IDisposable" /> that restores the previous language version when disposed.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The scope is thread-local; other test threads are not affected.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Scopes can be nested; disposing restores the immediately previous value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use a <c>using</c> statement to ensure proper cleanup.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Test with C# 11 features
    /// using (TestConfiguration.WithLanguageVersion(LanguageVersion.CSharp11))
    /// {
    ///     var result = await Test&lt;MyGenerator&gt;.Run("""
    ///         var x = "Hello"u8;  // C# 11 UTF-8 string literal
    ///         """);
    ///     result.IsClean().Verify();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="LanguageVersion" />
    public static IDisposable WithLanguageVersion(LanguageVersion version)
    {
        var previous = LanguageVersionOverride.Value;
        LanguageVersionOverride.Value = version;
        return new ConfigurationScope(() => LanguageVersionOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily overrides the reference assemblies for test compilations.
    /// </summary>
    /// <param name="assemblies">
    ///     The <see cref="Microsoft.CodeAnalysis.Testing.ReferenceAssemblies" /> to use within the scope.
    /// </param>
    /// <returns>
    ///     An <see cref="IDisposable" /> that restores the previous reference assemblies when disposed.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The scope is thread-local; other test threads are not affected.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Scopes can be nested; disposing restores the immediately previous value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use a <c>using</c> statement to ensure proper cleanup.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Test against .NET 8.0
    /// var net8 = new ReferenceAssemblies(
    ///     "net8.0",
    ///     new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
    ///     Path.Combine("ref", "net8.0"));
    ///
    /// using (TestConfiguration.WithReferenceAssemblies(net8))
    /// {
    ///     var result = await Test&lt;MyGenerator&gt;.Run(source);
    ///     result.IsClean().Verify();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ReferenceAssemblies" />
    public static IDisposable WithReferenceAssemblies(ReferenceAssemblies assemblies)
    {
        var previous = ReferenceAssembliesOverride.Value;
        ReferenceAssembliesOverride.Value = assemblies;
        return new ConfigurationScope(() => ReferenceAssembliesOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily sets additional metadata references for test compilations.
    /// </summary>
    /// <param name="references">
    ///     The additional <see cref="PortableExecutableReference" /> instances to include in compilations.
    /// </param>
    /// <returns>
    ///     An <see cref="IDisposable" /> that restores the previous additional references when disposed.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The scope is thread-local; other test threads are not affected.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Scopes can be nested; disposing restores the immediately previous value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Use a <c>using</c> statement to ensure proper cleanup.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 These references are added on top of <see cref="ReferenceAssemblies" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var customRef = MetadataReference.CreateFromFile(typeof(MyType).Assembly.Location);
    /// var refs = ImmutableArray.Create(customRef);
    ///
    /// using (TestConfiguration.WithAdditionalReferences(refs))
    /// {
    ///     var result = await Test&lt;MyGenerator&gt;.Run(source);
    ///     result.IsClean().Verify();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AdditionalReferences" />
    /// <seealso cref="WithAdditionalReferences(Type[])" />
    public static IDisposable WithAdditionalReferences(ImmutableArray<PortableExecutableReference> references)
    {
        var previous = AdditionalReferencesOverride.Value;
        AdditionalReferencesOverride.Value = references;
        return new ConfigurationScope(() => AdditionalReferencesOverride.Value = previous);
    }

    /// <summary>
    ///     Creates a scope that temporarily sets additional metadata references based on types' assemblies.
    /// </summary>
    /// <param name="types">
    ///     Types whose containing assemblies should be included as references.
    ///     Each type's assembly location is resolved and added as a <see cref="PortableExecutableReference" />.
    /// </param>
    /// <returns>
    ///     An <see cref="IDisposable" /> that restores the previous additional references when disposed.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The scope is thread-local; other test threads are not affected.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Scopes can be nested; disposing restores the immediately previous value.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Types with empty or null assembly locations are silently skipped.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Duplicate assembly locations are automatically deduplicated.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 This is a convenience overload that delegates to
    ///                 <see cref="WithAdditionalReferences(ImmutableArray{PortableExecutableReference})" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Add references for custom attributes used by the generator
    /// using (TestConfiguration.WithAdditionalReferences(
    ///     typeof(MyAttribute),
    ///     typeof(AnotherAttribute)))
    /// {
    ///     var result = await Test&lt;MyGenerator&gt;.Run("""
    ///         [My]
    ///         [Another]
    ///         public partial class Foo { }
    ///         """);
    ///     result.IsClean().Verify();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AdditionalReferences" />
    /// <seealso cref="WithAdditionalReferences(ImmutableArray{PortableExecutableReference})" />
    public static IDisposable WithAdditionalReferences(params Type[] types)
    {
        var references = types
            .Select(static t => t.Assembly.Location)
            .Where(static loc => !string.IsNullOrEmpty(loc))
            .Distinct()
            .Select(static loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray();

        return WithAdditionalReferences(references);
    }

    /// <summary>
    ///     Represents a disposable scope for configuration overrides.
    /// </summary>
    /// <param name="restore">The action to invoke when the scope is disposed to restore the previous configuration.</param>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The restore action is invoked exactly once, on the first call to <see cref="Dispose" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Subsequent calls to <see cref="Dispose" /> are no-ops.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private sealed class ConfigurationScope(Action restore) : IDisposable
    {
        private readonly Action _restore = restore;
        private bool _disposed;

        /// <summary>
        ///     Restores the previous configuration value if not already disposed.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _restore();
        }
    }
}
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="SyntaxValueProvider" /> that simplify common patterns
///     in Roslyn incremental source generators.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide filtered syntax providers that target specific syntax node types,
///         reducing boilerplate code in generator implementations.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Use <see cref="ForAttributeWithMetadataNameOfClassesAndRecords" /> to filter for classes
///                 and records with specific attributes.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="DetectVersion" /> to retrieve version information from MSBuild properties.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SyntaxValueProvider" />
/// <seealso cref="IncrementalValuesProvider{TValue}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SyntaxValueProviderExtensions
{
    /// <summary>
    ///     Creates an <see cref="IncrementalValuesProvider{T}" /> for classes and records
    ///     that are decorated with the specified attribute.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method filters the syntax provider to only include <see cref="ClassDeclarationSyntax" />
    ///         and <see cref="RecordDeclarationSyntax" /> nodes that have at least one attribute list.
    ///         It then uses <see cref="SyntaxValueProvider.ForAttributeWithMetadataName" /> to find
    ///         nodes decorated with the specified attribute.
    ///     </para>
    ///     <para>
    ///         The returned provider yields <see cref="GeneratorAttributeSyntaxContext" /> instances
    ///         that contain both the syntax node and semantic information.
    ///     </para>
    /// </remarks>
    /// <param name="source">The syntax value provider to extend.</param>
    /// <param name="fullyQualifiedMetadataName">
    ///     The fully qualified metadata name of the attribute to search for
    ///     (e.g., <c>"MyNamespace.MyAttribute"</c>).
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}" /> of <see cref="GeneratorAttributeSyntaxContext" />
    ///     for each class or record declaration that has the specified attribute.
    /// </returns>
    public static IncrementalValuesProvider<GeneratorAttributeSyntaxContext>
        ForAttributeWithMetadataNameOfClassesAndRecords(
            this SyntaxValueProvider source,
            string fullyQualifiedMetadataName)
    {
        return source
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName,
                static (node, _) =>
                    node is ClassDeclarationSyntax
                    {
                        AttributeLists.Count: > 0
                    } or RecordDeclarationSyntax
                    {
                        AttributeLists.Count: > 0
                    },
                static (context, _) => context);
    }

    /// <summary>
    ///     Detects the version string from MSBuild properties for use in generated code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method retrieves the version from the <c>RecognizeFramework_Version</c> MSBuild property.
    ///         If the property is not set, it falls back to the version of the calling assembly.
    ///     </para>
    ///     <para>
    ///         This is commonly used in source generators to embed version information in generated code
    ///         or to set fixed versions in snapshot tests.
    ///     </para>
    /// </remarks>
    /// <param name="context">The incremental generator initialization context.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> of <see cref="string" /> containing the detected version,
    ///     either from the MSBuild property or from the calling assembly.
    /// </returns>
    public static IncrementalValueProvider<string> DetectVersion(
        this IncrementalGeneratorInitializationContext context)
    {
        var defaultVersion = $"{Assembly.GetCallingAssembly().GetName().Version}";

        return context.AnalyzerConfigOptionsProvider
            .Select<AnalyzerConfigOptionsProvider, string>((options, _) =>
                options.GetGlobalProperty("Version", "RecognizeFramework") ?? defaultVersion);
    }
}

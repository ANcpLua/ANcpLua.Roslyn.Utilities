using System.Reflection;
using ANcpLua.Roslyn.Utilities.Models;
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
///                 Use <see cref="SelectAllAttributes" /> to extract all attributes from a syntax context.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="SelectManyAllAttributesOfCurrentClassSyntax" /> to filter attributes
///                 that belong to the current class declaration only.
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
/// <seealso cref="ClassWithAttributesContext" />
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
    /// <seealso cref="SelectAllAttributes" />
    /// <seealso cref="SelectManyAllAttributesOfCurrentClassSyntax" />
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
    ///     Transforms a provider of <see cref="GeneratorAttributeSyntaxContext" /> into a provider
    ///     of <see cref="ClassWithAttributesContext" /> containing all attributes from the context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method extracts the semantic model, all matched attributes, the class declaration syntax,
    ///         and the named type symbol from each <see cref="GeneratorAttributeSyntaxContext" />.
    ///     </para>
    ///     <para>
    ///         The target node is expected to be a <see cref="ClassDeclarationSyntax" /> and
    ///         the target symbol is expected to be an <see cref="INamedTypeSymbol" />.
    ///     </para>
    /// </remarks>
    /// <param name="source">
    ///     The source provider of <see cref="GeneratorAttributeSyntaxContext" /> instances.
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}" /> of <see cref="ClassWithAttributesContext" />
    ///     containing the semantic model, all attributes, class syntax, and type symbol.
    /// </returns>
    /// <seealso cref="ForAttributeWithMetadataNameOfClassesAndRecords" />
    /// <seealso cref="SelectManyAllAttributesOfCurrentClassSyntax" />
    /// <seealso cref="ClassWithAttributesContext" />
    public static IncrementalValuesProvider<ClassWithAttributesContext>
        SelectAllAttributes(
            this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source)
    {
        return source
            .Select(static (context, _) => new ClassWithAttributesContext(
                context.SemanticModel,
                context.Attributes,
                (ClassDeclarationSyntax)context.TargetNode,
                (INamedTypeSymbol)context.TargetSymbol));
    }

    private static string RemoveNameof(this string value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));

        return value.Contains("nameof(")
            ? value[(value.LastIndexOf('.') + 1)..]
                .TrimEnd(')', ' ')
            : value;
    }

    private static AttributeSyntax? TryFindAttributeSyntax(
        this MemberDeclarationSyntax classSyntax,
        AttributeData attribute)
    {
        var constructorArgs = attribute.ConstructorArguments;
        var name = constructorArgs.Length > 0 ? constructorArgs[0].Value?.ToString() : null;

        foreach (var attributeList in classSyntax.AttributeLists)
        foreach (var attr in attributeList.Attributes)
        {
            var arguments = attr.ArgumentList?.Arguments;
            if (arguments is not { Count: > 0 })
                continue;

            var firstArg = arguments.Value[0].ToString().Trim('"').RemoveNameof();
            if (firstArg == name)
                return attr;
        }

        return null;
    }

    /// <summary>
    ///     Transforms a provider of <see cref="GeneratorAttributeSyntaxContext" /> into a provider
    ///     of <see cref="ClassWithAttributesContext" />, filtering to only include attributes
    ///     that are syntactically present on the current class declaration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="SelectAllAttributes" />, this method filters attributes to only those
    ///         that are actually declared on the current class syntax node. This is useful when working
    ///         with inherited attributes or when the same attribute type appears on multiple declarations.
    ///     </para>
    ///     <para>
    ///         Each attribute on the class produces a separate <see cref="ClassWithAttributesContext" />
    ///         in the output, allowing for individual processing of each attribute occurrence.
    ///     </para>
    /// </remarks>
    /// <param name="source">
    ///     The source provider of <see cref="GeneratorAttributeSyntaxContext" /> instances.
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}" /> of <see cref="ClassWithAttributesContext" />
    ///     for each attribute that is syntactically present on the current class declaration.
    /// </returns>
    /// <seealso cref="SelectAllAttributes" />
    /// <seealso cref="ForAttributeWithMetadataNameOfClassesAndRecords" />
    /// <seealso cref="ClassWithAttributesContext" />
    public static IncrementalValuesProvider<ClassWithAttributesContext>
        SelectManyAllAttributesOfCurrentClassSyntax(
            this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source)
    {
        return source
            .SelectMany(static (context, _) => FilterAttributesOfCurrentClass(context));
    }

    private static ImmutableArray<ClassWithAttributesContext> FilterAttributesOfCurrentClass(
        GeneratorAttributeSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var builder = ImmutableArray.CreateBuilder<ClassWithAttributesContext>();

        foreach (var attribute in context.Attributes)
        {
            var attributeSyntax = classSyntax.TryFindAttributeSyntax(attribute);
            if (attributeSyntax is not null)
                builder.Add(new ClassWithAttributesContext(
                    context.SemanticModel,
                    [attribute],
                    classSyntax,
                    targetSymbol));
        }

        return builder.ToImmutable();
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
using System;
using System.Linq;
using System.Reflection;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="SyntaxValueProvider" />.
/// </summary>
public static class SyntaxValueProviderExtensions
{
    /// <summary>
    ///     Creates an <see cref="IncrementalValuesProvider{T}" /> for classes and records with the specified attribute.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="fullyQualifiedMetadataName"></param>
    /// <returns></returns>
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
    ///     Selects all attributes from a generator attribute syntax context.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
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
        this ClassDeclarationSyntax classSyntax,
        AttributeData attribute)
    {
        var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

        return classSyntax.AttributeLists
            .SelectMany(static x => x.Attributes)
            .FirstOrDefault(x =>
                x.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"').RemoveNameof() == name);
    }

    /// <summary>
    ///     Selects attributes of the current class syntax.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IncrementalValuesProvider<ClassWithAttributesContext>
        SelectManyAllAttributesOfCurrentClassSyntax(
            this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source)
    {
        return source
            .SelectMany(static (context, _) => context.Attributes
                .Where(x =>
                {
                    var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
                    var attributeSyntax = classSyntax.TryFindAttributeSyntax(x);

                    return attributeSyntax != null;
                })
                .Select(x => new ClassWithAttributesContext(
                    context.SemanticModel,
                    [x],
                    (ClassDeclarationSyntax)context.TargetNode,
                    (INamedTypeSymbol)context.TargetSymbol)));
    }

    /// <summary>
    ///     Returns version from RecognizeFramework_Version MSBuild property.
    ///     Usually used to set fixed version in SnapshotTests.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IncrementalValueProvider<string> DetectVersion(
        this IncrementalGeneratorInitializationContext context)
    {
        var defaultVersion = $"{Assembly.GetCallingAssembly().GetName().Version}";

        return context.AnalyzerConfigOptionsProvider
            .Select<AnalyzerConfigOptionsProvider, string>((options, _) =>
                options.GetGlobalOption("Version", "RecognizeFramework") ?? defaultVersion);
    }
}
using System.Collections.Immutable;
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
            string fullyQualifiedMetadataName) =>
        source
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

    /// <summary>
    ///     Selects all attributes from a generator attribute syntax context.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IncrementalValuesProvider<ClassWithAttributesContext>
        SelectAllAttributes(
            this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source) =>
        source
            .Select(static (context, _) => new ClassWithAttributesContext(
                context.SemanticModel,
                context.Attributes,
                (ClassDeclarationSyntax)context.TargetNode,
                (INamedTypeSymbol)context.TargetSymbol));

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
    ///     Selects attributes of the current class syntax.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IncrementalValuesProvider<ClassWithAttributesContext>
        SelectManyAllAttributesOfCurrentClassSyntax(
            this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source) =>
        source
            .SelectMany(static (context, _) => FilterAttributesOfCurrentClass(context));

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
            {
                builder.Add(new ClassWithAttributesContext(
                    context.SemanticModel,
                    [attribute],
                    classSyntax,
                    targetSymbol));
            }
        }

        return builder.ToImmutable();
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
                options.GetGlobalProperty("Version", "RecognizeFramework") ?? defaultVersion);
    }
}

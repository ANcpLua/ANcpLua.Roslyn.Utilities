using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
/// Factory methods for creating configured <see cref="GeneratorDriver"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a consistent way to create generator drivers with proper configuration
/// for testing. It handles:
/// <list type="bullet">
///   <item><description>Generator instantiation and source generator wrapping</description></item>
///   <item><description>Step tracking configuration for caching validation</description></item>
///   <item><description>Parse options from <see cref="TestConfiguration"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a driver with step tracking enabled
/// var driver = GeneratorDriverFactory.CreateDriver&lt;MyGenerator&gt;(trackSteps: true);
///
/// // Run the driver
/// driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
///
/// // Analyze results
/// var result = driver.GetRunResult();
/// </code>
/// </example>
public static class GeneratorDriverFactory
{
    /// <summary>
    /// Creates a <see cref="GeneratorDriver"/> configured for the specified generator.
    /// </summary>
    /// <typeparam name="TGenerator">
    /// The generator type. Must implement <see cref="IIncrementalGenerator"/>
    /// and have a parameterless constructor.
    /// </typeparam>
    /// <param name="trackSteps">
    /// If <c>true</c>, enables pipeline step tracking for caching analysis.
    /// This is required for <see cref="GeneratorCachingReport"/> and related functionality.
    /// </param>
    /// <returns>
    /// A configured <see cref="GeneratorDriver"/> ready to be run against a compilation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The driver is configured with:
    /// <list type="bullet">
    ///   <item><description>The specified generator wrapped as a source generator</description></item>
    ///   <item><description>Step tracking as specified by <paramref name="trackSteps"/></description></item>
    ///   <item><description>Parse options from <see cref="TestConfiguration.LanguageVersion"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Step tracking has a small performance overhead, so it should only be enabled
    /// when caching validation is needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For generation tests (no step tracking needed)
    /// var driver = GeneratorDriverFactory.CreateDriver&lt;MyGenerator&gt;(false);
    ///
    /// // For caching tests (step tracking required)
    /// var driver = GeneratorDriverFactory.CreateDriver&lt;MyGenerator&gt;(true);
    /// </code>
    /// </example>
    public static GeneratorDriver CreateDriver<TGenerator>(bool trackSteps)
        where TGenerator : IIncrementalGenerator, new()
    {
        var parseOptions = new CSharpParseOptions(TestConfiguration.LanguageVersion);
        return CSharpGeneratorDriver.Create(
            [new TGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps),
            parseOptions: parseOptions);
    }
}
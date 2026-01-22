using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Analyzes generator pipeline steps for caching validation.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for extracting and categorizing tracked steps
///         from generator runs. It distinguishes between observable user steps (which should
///         be validated for caching) and infrastructure sink steps (which are internal).
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Extracts tracked steps from <see cref="GeneratorDriverRunResult" /> instances
///                 for caching analysis.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Identifies infrastructure steps (e.g., <c>RegisterSourceOutput</c>,
///                 <c>RegisterImplementationSourceOutput</c>) that should be excluded from
///                 user-facing caching reports.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Identifies infrastructure files (e.g., embedded attributes, polyfills)
///                 that are auto-generated framework artifacts.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="GeneratorCachingReport" />
/// <seealso cref="GeneratorStepAnalysis" />
/// <seealso cref="StepClassification" />
internal static class GeneratorStepAnalyzer
{
    /// <summary>
    ///     Extracts tracked steps from a generator run result.
    /// </summary>
    /// <param name="result">
    ///     The <see cref="GeneratorDriverRunResult" /> to analyze. Must have tracking enabled
    ///     via <c>WithTrackingName</c> on pipeline steps for meaningful results.
    /// </param>
    /// <returns>
    ///     A dictionary mapping step names to their execution data as an
    ///     <see cref="ImmutableArray{T}" /> of <see cref="IncrementalGeneratorRunStep" />.
    ///     Steps from all generators in the result are merged by name.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Steps are grouped by their tracking name, allowing multiple generators
    ///                 to contribute to the same logical step.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The returned dictionary includes both infrastructure steps and user steps.
    ///                 Use <see cref="IsInfrastructureStep" /> to filter infrastructure steps.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="GeneratorCachingReport.Create" />
    public static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExtractSteps(
        GeneratorDriverRunResult result)
    {
        return result.Results.SelectMany(static x => x.TrackedSteps)
            .GroupBy(static kv => kv.Key)
            .ToDictionary(static g => g.Key, static g => g
                .SelectMany(static kv => kv.Value)
                .ToImmutableArray());
    }

    /// <summary>
    ///     Determines if a step is an infrastructure step (sink).
    /// </summary>
    /// <param name="stepName">The name of the step to check.</param>
    /// <returns>
    ///     <see langword="true" /> if this is an infrastructure step that should be excluded
    ///     from user-facing caching reports; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Infrastructure steps are internal pipeline termination points that register
    ///         output with the generator driver. These include:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <c>RegisterSourceOutput</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>RegisterImplementationSourceOutput</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>RegisterPostInitializationOutput</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>SourceOutput</c>
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         These steps are excluded from caching validation because they represent
    ///         the final output registration rather than intermediate pipeline processing.
    ///     </para>
    /// </remarks>
    /// <seealso cref="StepClassification.IsInfrastructureStep" />
    /// <seealso cref="StepClassification.IsSinkStep" />
    public static bool IsInfrastructureStep(string stepName) => StepClassification.IsInfrastructureStep(stepName);

    /// <summary>
    ///     Determines if a file is an infrastructure file (e.g., embedded attributes).
    /// </summary>
    /// <param name="fileName">The name of the file to check (typically the hint name).</param>
    /// <returns>
    ///     <see langword="true" /> if this is an infrastructure file that should be excluded
    ///     from user-facing output validation; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Infrastructure files are auto-generated framework artifacts that are not
    ///         part of the user's intended generator output. These include:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Files ending with <c>Attribute.g.cs</c> or <c>Attributes.g.cs</c></description>
    ///         </item>
    ///         <item>
    ///             <description>Files containing <c>EmbeddedAttribute</c></description>
    ///         </item>
    ///         <item>
    ///             <description>Files containing <c>Polyfill</c></description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Excluding these files ensures that caching reports and output validation
    ///         focus on the meaningful generator output rather than framework boilerplate.
    ///     </para>
    /// </remarks>
    /// <seealso cref="StepClassification.IsInfrastructureFile" />
    public static bool IsInfrastructureFile(string fileName) => StepClassification.IsInfrastructureFile(fileName);
}

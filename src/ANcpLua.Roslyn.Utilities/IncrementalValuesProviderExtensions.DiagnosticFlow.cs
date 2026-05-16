using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using InvalidOperationException = System.InvalidOperationException;

namespace ANcpLua.Roslyn.Utilities;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class IncrementalValuesProviderExtensions
{
    // ========== DiagnosticFlow Pipeline Integration ==========

    /// <summary>
    ///     Reports all diagnostics from flows and continues with successful values only.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is part of the DiagnosticFlow pipeline integration for railway-oriented programming.
    ///         It processes each flow by:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Reporting all diagnostics (errors and warnings) from each flow.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Filtering to only flows where <see cref="DiagnosticFlow{T}.IsSuccess" /> is <c>true</c>.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Extracting and returning the successful values.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the flow value.</typeparam>
    /// <param name="source">The provider of diagnostic flows.</param>
    /// <param name="context">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only successful values.
    /// </returns>
    /// <seealso
    ///     cref="ReportAndContinue{T}(IncrementalValueProvider{DiagnosticFlow{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso
    ///     cref="ReportAndStop{T}(IncrementalValuesProvider{DiagnosticFlow{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="DiagnosticFlow{T}" />
    public static IncrementalValuesProvider<T> ReportAndContinue<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        IncrementalGeneratorInitializationContext context)
    {
        // Report all diagnostics
        context.RegisterSourceOutput(
            source.SelectMany(static (flow, _) => flow.Diagnostics.IsEmpty
                ? ImmutableArray<DiagnosticInfo>.Empty
                : flow.Diagnostics.AsImmutableArray()),
            static (ctx, diagnostic) => ctx.ReportDiagnostic(diagnostic));

        // Return only successful values using pattern matching to avoid null-forgiving operator
        return source
            .SelectMany(static (flow, _) => flow is { IsSuccess: true, Value: { } value }
                ? [value]
                : ImmutableArray<T>.Empty);
    }

    /// <summary>
    ///     Reports all diagnostics from a single flow and continues with value if successful.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is part of the DiagnosticFlow pipeline integration for railway-oriented programming.
    ///         It processes the flow by:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Reporting all diagnostics from the flow.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returning the value if successful, or default if failed.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the flow value.</typeparam>
    /// <param name="source">The provider of a single diagnostic flow.</param>
    /// <param name="context">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> containing the value if successful,
    ///     or <c>null</c>/<c>default</c> if failed.
    /// </returns>
    /// <seealso
    ///     cref="ReportAndContinue{T}(IncrementalValuesProvider{DiagnosticFlow{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="DiagnosticFlow{T}" />
    public static IncrementalValueProvider<T?> ReportAndContinue<T>(
        this IncrementalValueProvider<DiagnosticFlow<T>> source,
        IncrementalGeneratorInitializationContext context)
    {
        // Report diagnostics
        context.RegisterSourceOutput(
            source.SelectMany(static (flow, _) => flow.Diagnostics.IsEmpty
                ? ImmutableArray<DiagnosticInfo>.Empty
                : flow.Diagnostics.AsImmutableArray()),
            static (ctx, diagnostic) => ctx.ReportDiagnostic(diagnostic));

        // Return value or default
        return source.Select(static (flow, _) => flow.IsSuccess ? flow.Value : default);
    }

    /// <summary>
    ///     Reports diagnostics and stops processing if any errors exist.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method behaves similarly to
    ///         <see
    ///             cref="ReportAndContinue{T}(IncrementalValuesProvider{DiagnosticFlow{T}}, IncrementalGeneratorInitializationContext)" />
    ///         ,
    ///         reporting all diagnostics and returning only successful values. Use this when you want to
    ///         emphasize that processing stops for failed flows.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the flow value.</typeparam>
    /// <param name="source">The provider of diagnostic flows.</param>
    /// <param name="context">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only values from flows without errors.
    /// </returns>
    /// <seealso
    ///     cref="ReportAndContinue{T}(IncrementalValuesProvider{DiagnosticFlow{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="DiagnosticFlow{T}" />
    public static IncrementalValuesProvider<T> ReportAndStop<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        IncrementalGeneratorInitializationContext context)
    {
        // Report errors and warnings
        context.RegisterSourceOutput(
            source.SelectMany(static (flow, _) => flow.Diagnostics.IsEmpty
                ? ImmutableArray<DiagnosticInfo>.Empty
                : flow.Diagnostics.AsImmutableArray()),
            static (ctx, diagnostic) => ctx.ReportDiagnostic(diagnostic));

        // Only continue with values that have no errors using pattern matching to avoid null-forgiving operator
        return source
            .SelectMany(static (flow, _) => flow is { IsSuccess: true, Value: { } value }
                ? [value]
                : ImmutableArray<T>.Empty);
    }

    /// <summary>
    ///     Collects flows into a single flow. All must succeed for result to succeed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method aggregates multiple flows into a single flow containing an array of all values.
    ///         The resulting flow:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Succeeds only if all input flows succeed.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Contains all diagnostics from all input flows.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Contains an array of all successful values.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the flow values.</typeparam>
    /// <param name="source">The provider of diagnostic flows to collect.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing a single flow containing
    ///     an <see cref="ImmutableArray{T}" /> of all values.
    /// </returns>
    /// <seealso cref="DiagnosticFlow.Collect{T}(IEnumerable{DiagnosticFlow{T}})" />
    public static IncrementalValueProvider<DiagnosticFlow<ImmutableArray<T>>> CollectFlows<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source)
    {
        return source.Collect()
            .Select(static (flows, _) => DiagnosticFlow.Collect(flows));
    }

    /// <summary>
    ///     Transforms values into flows using a selector that returns a <see cref="DiagnosticFlow{T}" />.
    /// </summary>
    /// <remarks>
    ///     Use this to begin a flow-based pipeline where transformations may produce diagnostics.
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values.</typeparam>
    /// <typeparam name="TResult">The type of the flow result values.</typeparam>
    /// <param name="source">The provider of source values.</param>
    /// <param name="selector">A function that transforms a source value into a diagnostic flow.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of diagnostic flows.
    /// </returns>
    /// <seealso
    ///     cref="SelectFlow{TSource, TResult}(IncrementalValuesProvider{TSource}, Func{TSource, DiagnosticFlow{TResult}})" />
    /// <seealso
    ///     cref="ThenFlow{TSource, TResult}(IncrementalValuesProvider{DiagnosticFlow{TSource}}, Func{TSource, DiagnosticFlow{TResult}})" />
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> SelectFlow<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, CancellationToken, DiagnosticFlow<TResult>> selector)
    {
        return source.Select(selector);
    }

    /// <summary>
    ///     Transforms values into flows using a simple selector that returns a <see cref="DiagnosticFlow{T}" />.
    /// </summary>
    /// <remarks>
    ///     This overload is for selectors that do not require a <see cref="CancellationToken" />.
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values.</typeparam>
    /// <typeparam name="TResult">The type of the flow result values.</typeparam>
    /// <param name="source">The provider of source values.</param>
    /// <param name="selector">A function that transforms a source value into a diagnostic flow.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of diagnostic flows.
    /// </returns>
    /// <seealso
    ///     cref="SelectFlow{TSource, TResult}(IncrementalValuesProvider{TSource}, Func{TSource, CancellationToken, DiagnosticFlow{TResult}})" />
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> SelectFlow<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, DiagnosticFlow<TResult>> selector)
    {
        return source.Select((value, _) => selector(value));
    }

    /// <summary>
    ///     Chains flow transformations, applying a selector to the value of each successful flow.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method enables monadic composition of flows. For each input flow:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If the flow has failed, it passes through unchanged.</description>
    ///             </item>
    ///             <item>
    ///                 <description>If the flow is successful, the selector is applied to its value.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Diagnostics from both the original flow and the selector result are combined.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="TSource">The type of the source flow values.</typeparam>
    /// <typeparam name="TResult">The type of the result flow values.</typeparam>
    /// <param name="source">The provider of source diagnostic flows.</param>
    /// <param name="selector">A function that transforms a successful value into a new flow.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of chained diagnostic flows.
    /// </returns>
    /// <seealso
    ///     cref="SelectFlow{TSource, TResult}(IncrementalValuesProvider{TSource}, Func{TSource, DiagnosticFlow{TResult}})" />
    /// <seealso cref="DiagnosticFlow{T}.Then{TResult}(Func{T, DiagnosticFlow{TResult}})" />
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> ThenFlow<TSource, TResult>(
        this IncrementalValuesProvider<DiagnosticFlow<TSource>> source,
        Func<TSource, DiagnosticFlow<TResult>> selector)
    {
        return source.Select((flow, _) => flow.Then(selector));
    }

    /// <summary>
    ///     Adds a warning to all flows conditionally based on their values.
    /// </summary>
    /// <remarks>
    ///     For each flow, if the condition evaluates to <c>true</c> for the flow's value,
    ///     the specified warning diagnostic is added to the flow. Failed flows are passed through unchanged.
    /// </remarks>
    /// <typeparam name="T">The type of the flow values.</typeparam>
    /// <param name="source">The provider of diagnostic flows.</param>
    /// <param name="condition">A function that determines whether to add the warning based on the value.</param>
    /// <param name="warning">The warning diagnostic to add when the condition is true.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of flows with the conditional warning applied.
    /// </returns>
    /// <seealso cref="DiagnosticFlow{T}" />
    public static IncrementalValuesProvider<DiagnosticFlow<T>> WarnIf<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        Func<T, bool> condition,
        DiagnosticInfo warning)
    {
        return source.Select((flow, _) => flow.WarnIf(condition, warning));
    }

    /// <summary>
    ///     Filters flows with a predicate, failing those that do not match.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For each flow:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If already failed, passes through unchanged.</description>
    ///             </item>
    ///             <item>
    ///                 <description>If the predicate returns <c>true</c>, the flow passes through unchanged.</description>
    ///             </item>
    ///             <item>
    ///                 <description>If the predicate returns <c>false</c>, the flow fails with the specified diagnostic.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the flow values.</typeparam>
    /// <param name="source">The provider of diagnostic flows.</param>
    /// <param name="predicate">A function that determines whether a value should pass through.</param>
    /// <param name="onFail">The diagnostic to report when the predicate returns <c>false</c>.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of filtered flows.
    /// </returns>
    /// <seealso cref="DiagnosticFlow{T}" />
    public static IncrementalValuesProvider<DiagnosticFlow<T>> WhereFlow<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        Func<T, bool> predicate,
        DiagnosticInfo onFail)
    {
        return source.Select((flow, _) => flow.Where(predicate, onFail));
    }
}

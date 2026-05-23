using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using InvalidOperationException = System.InvalidOperationException;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for <see cref="Microsoft.CodeAnalysis.IncrementalValuesProvider{TValues}" /> and
///     <see cref="Microsoft.CodeAnalysis.IncrementalValueProvider{TValue}" /> to simplify common operations in incremental
///     source generators.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods for working with Roslyn's incremental generator pipeline,
///         including source output registration, diagnostic reporting, collection operations, and flow-based
///         transformations.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Source output methods for registering generated files with the compilation.</description>
///         </item>
///         <item>
///             <description>Exception and diagnostic reporting utilities for error handling.</description>
///         </item>
///         <item>
///             <description>LINQ-like operations (GroupBy, Distinct, Take, Skip, etc.) for pipeline data.</description>
///         </item>
///         <item>
///             <description>DiagnosticFlow integration for railway-oriented programming patterns.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Microsoft.CodeAnalysis.IncrementalValuesProvider{TValues}" />
/// <seealso cref="Microsoft.CodeAnalysis.IncrementalValueProvider{TValue}" />
/// <seealso cref="DiagnosticFlow{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static partial class IncrementalValuesProviderExtensions
{
    /// <summary>
    ///     Registers a source output for a collection of files with names.
    /// </summary>
    /// <remarks>
    ///     Empty files (where <see cref="FileWithName.IsEmpty" /> is <c>true</c>) are automatically skipped
    ///     and not added to the compilation output.
    /// </remarks>
    /// <param name="source">The provider of files to register as source output.</param>
    /// <param name="context">The generator initialization context for registering the output.</param>
    /// <seealso cref="AddSource(IncrementalValueProvider{FileWithName}, IncrementalGeneratorInitializationContext)" />
    /// <seealso
    ///     cref="AddSources(IncrementalValueProvider{EquatableArray{FileWithName}}, IncrementalGeneratorInitializationContext)" />
    public static void AddSource(
        this IncrementalValuesProvider<FileWithName> source,
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(source, static (context, file) =>
        {
            if (file.IsEmpty) return;

            context.AddSource(
                file.Name,
                file.Text);
        });
    }

    /// <summary>
    ///     Registers a source output for a single file with name.
    /// </summary>
    /// <remarks>
    ///     Empty files (where <see cref="FileWithName.IsEmpty" /> is <c>true</c>) are automatically skipped
    ///     and not added to the compilation output.
    /// </remarks>
    /// <param name="source">The provider of a single file to register as source output.</param>
    /// <param name="context">The generator initialization context for registering the output.</param>
    /// <seealso cref="AddSource(IncrementalValuesProvider{FileWithName}, IncrementalGeneratorInitializationContext)" />
    /// <seealso
    ///     cref="AddSources(IncrementalValueProvider{EquatableArray{FileWithName}}, IncrementalGeneratorInitializationContext)" />
    public static void AddSource(
        this IncrementalValueProvider<FileWithName> source,
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(source, static (context, file) =>
        {
            if (file.IsEmpty) return;

            context.AddSource(
                file.Name,
                file.Text);
        });
    }

    /// <summary>
    ///     Registers a nullable source output for a single file with name.
    /// </summary>
    /// <remarks>
    ///     Use this overload after result/diagnostic pipelines where failure is represented as
    ///     <c>null</c> instead of a sentinel <see cref="FileWithName" /> value.
    /// </remarks>
    /// <param name="source">The provider of a single nullable file to register as source output.</param>
    /// <param name="context">The generator initialization context for registering the output.</param>
    public static void AddSource(
        this IncrementalValueProvider<FileWithName?> source,
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(source, static (context, file) =>
        {
            if (file is not { IsEmpty: false } value) return;

            context.AddSource(
                value.Name,
                value.Text);
        });
    }

    /// <summary>
    ///     Registers a source output for a single collected array of files.
    ///     Use after <c>.Collect()</c> when you have one array containing all files.
    /// </summary>
    /// <remarks>
    ///     This method flattens the array and registers each file individually, skipping empty files.
    /// </remarks>
    /// <param name="source">A single value provider producing one array of files.</param>
    /// <param name="context">The generator initialization context for registering the output.</param>
    /// <example>
    ///     <code>
    /// // After Collect() - one array of files
    /// provider.Collect().Select(Transform).AddSources(context);
    /// </code>
    /// </example>
    /// <seealso
    ///     cref="AddSources(IncrementalValuesProvider{EquatableArray{FileWithName}}, IncrementalGeneratorInitializationContext)" />
    public static void AddSources(
        this IncrementalValueProvider<EquatableArray<FileWithName>> source,
        IncrementalGeneratorInitializationContext context)
    {
        source
            .SelectMany(static (x, _) => x)
            .AddSource(context);
    }

    /// <summary>
    ///     Registers a source output for multiple arrays of files (one array per input).
    ///     Use when each input produces its own array of files.
    /// </summary>
    /// <remarks>
    ///     This method flattens each array and registers each file individually, skipping empty files.
    /// </remarks>
    /// <param name="source">A values provider producing one array per input item.</param>
    /// <param name="context">The generator initialization context for registering the output.</param>
    /// <example>
    ///     <code>
    /// // Each class produces multiple files
    /// provider.Select(GenerateFilesForClass).AddSources(context);
    /// </code>
    /// </example>
    /// <seealso
    ///     cref="AddSources(IncrementalValueProvider{EquatableArray{FileWithName}}, IncrementalGeneratorInitializationContext)" />
    public static void AddSources(
        this IncrementalValuesProvider<EquatableArray<FileWithName>> source,
        IncrementalGeneratorInitializationContext context)
    {
        source
            .SelectMany(static (x, _) => x)
            .AddSource(context);
    }

    /// <summary>
    ///     Registers the canonical "collect → gate → exception-safe emit → AddSource" pipeline
    ///     used by per-concern source generators that produce one file from many discovered values.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Composes <see cref="CollectAsEquatableArray{TSource}" />, the Roslyn
    ///         <c>Combine</c> primitive,
    ///         <see cref="SelectAndReportExceptions{TSource, TResult}(IncrementalValueProvider{TSource}, Func{TSource, CancellationToken, TResult}, IncrementalGeneratorInitializationContext, string)" />,
    ///         and <see cref="AddSource(IncrementalValueProvider{FileWithName}, IncrementalGeneratorInitializationContext)" />
    ///         into a single declaration so the pipeline shape is enforced uniformly across generators.
    ///     </para>
    ///     <para>
    ///         When the gate is <c>false</c> or the collected input is empty, the emitter is skipped
    ///         and <see cref="FileWithName.Empty" /> flows through; <c>AddSource</c> drops empty files.
    ///         When the emitter throws, the exception is reported as a diagnostic with
    ///         <paramref name="diagnosticId" /> and the build continues without the file.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of discovered values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of discovered values to collect and pass to the emitter.</param>
    /// <param name="context">The generator initialization context for registering the source output.</param>
    /// <param name="gate">A boolean provider that must be <c>true</c> for the emitter to run.</param>
    /// <param name="emitter">A pure function that turns the collected values into a generated file.</param>
    /// <param name="diagnosticId">The diagnostic ID reported when the emitter throws. Defaults to <c>"SRE001"</c>.</param>
    /// <seealso
    ///     cref="RegisterCollectedEmitter{T}(IncrementalValuesProvider{T}, IncrementalGeneratorInitializationContext, IncrementalValueProvider{bool}, string, Func{ImmutableArray{T}, string}, string)" />
    /// <seealso cref="SelectAndReportExceptions{TSource, TResult}(IncrementalValueProvider{TSource}, Func{TSource, CancellationToken, TResult}, IncrementalGeneratorInitializationContext, string)" />
    public static void RegisterCollectedEmitter<T>(
        this IncrementalValuesProvider<T> source,
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> gate,
        Func<ImmutableArray<T>, FileWithName> emitter,
        string diagnosticId = "SRE001")
        where T : IEquatable<T>
    {
        source
            .CollectAsEquatableArray()
            .Combine(gate)
            .SelectAndCaptureExceptions((input, _) =>
                input.Right && !input.Left.IsDefaultOrEmpty
                    ? emitter(input.Left.AsImmutableArray())
                    : FileWithName.Empty, diagnosticId)
            .SelectAndReportDiagnostics(context)
            .AddSource(context);
    }

    /// <summary>
    ///     Convenience overload of
    ///     <see
    ///         cref="RegisterCollectedEmitter{T}(IncrementalValuesProvider{T}, IncrementalGeneratorInitializationContext, IncrementalValueProvider{bool}, Func{ImmutableArray{T}, FileWithName}, string)" />
    ///     for emitters that produce raw source text under a fixed file name.
    /// </summary>
    /// <typeparam name="T">The type of discovered values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of discovered values to collect and pass to the emitter.</param>
    /// <param name="context">The generator initialization context for registering the source output.</param>
    /// <param name="gate">A boolean provider that must be <c>true</c> for the emitter to run.</param>
    /// <param name="generatedFileName">The hint name for the emitted file (e.g. <c>"MyManifest.g.cs"</c>).</param>
    /// <param name="emitter">A pure function that turns the collected values into the generated source text. Returning <c>null</c> or empty suppresses emission.</param>
    /// <param name="diagnosticId">The diagnostic ID reported when the emitter throws. Defaults to <c>"SRE001"</c>.</param>
    public static void RegisterCollectedEmitter<T>(
        this IncrementalValuesProvider<T> source,
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> gate,
        string generatedFileName,
        Func<ImmutableArray<T>, string?> emitter,
        string diagnosticId = "SRE001")
        where T : IEquatable<T>
    {
        source.RegisterCollectedEmitter(
            context,
            gate,
            values =>
            {
                var text = emitter(values);
                return string.IsNullOrEmpty(text)
                    ? FileWithName.Empty
                    : new FileWithName(generatedFileName, text!);
            },
            diagnosticId);
    }

    /// <summary>
    ///     Collects all values from a provider into an <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <remarks>
    ///     This method is useful for scenarios where you need value equality semantics for caching
    ///     in the incremental generator pipeline.
    /// </remarks>
    /// <typeparam name="TSource">The type of elements in the collection. Must implement <see cref="System.IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of values to collect.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> that produces an <see cref="EquatableArray{T}" />
    ///     containing all collected values.
    /// </returns>
    /// <seealso cref="EquatableArray{T}" />
    public static IncrementalValueProvider<EquatableArray<TSource>> CollectAsEquatableArray<TSource>(
        this IncrementalValuesProvider<TSource> source)
        where TSource : IEquatable<TSource>
    {
        return source
            .Collect()
            .Select(static (x, _) => x.AsEquatableArray());
    }

    /// <summary>
    ///     Transforms a single value using a selector function and captures any exception as diagnostics.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method wraps the selector in a try-catch block. If an exception is thrown during
    ///         transformation, the returned <see cref="ResultWithDiagnostics{T}" /> contains
    ///         <see cref="ResultWithDiagnostics{T}.HasResult" /> set to <c>false</c> plus an error diagnostic.
    ///         This keeps failure explicit for single-value providers, where Roslyn cannot filter out only one bad item.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The provider of source values to transform.</param>
    /// <param name="selector">The transformation function that may throw exceptions.</param>
    /// <param name="id">The diagnostic ID to use when reporting exceptions. Defaults to <c>"SRE001"</c>.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> that produces a result/diagnostic envelope.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportExceptions{TSource, TResult}(IncrementalValueProvider{TSource}, Func{TSource, CancellationToken, TResult}, IncrementalGeneratorInitializationContext, string)" />
    public static IncrementalValueProvider<ResultWithDiagnostics<TResult>> SelectAndCaptureExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector,
        string id = "SRE001")
        where TResult : notnull
    {
        return source
            .Select<TSource, ResultWithDiagnostics<TResult>>((value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return new ResultWithDiagnostics<TResult>(selector(value, cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    // Cancellation must propagate so Roslyn can abort the pipeline.
                    throw;
                }
                catch (Exception exception)
                {
                    var diagnostic = GeneratorErrorInfo.From(exception).ToDiagnosticInfo(id);
                    return new ResultWithDiagnostics<TResult>(
                        default!,
                        ImmutableArray.Create(diagnostic).AsEquatableArray(),
                        false);
                }
            });
    }

    /// <summary>
    ///     Transforms a single value using a selector function, reports any exception as diagnostics, and returns the
    ///     explicit result/diagnostic envelope.
    /// </summary>
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The provider of source values to transform.</param>
    /// <param name="selector">The transformation function that may throw exceptions.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <param name="id">The diagnostic ID to use when reporting exceptions. Defaults to <c>"SRE001"</c>.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> that produces a result/diagnostic envelope.
    /// </returns>
    /// <seealso cref="SelectAndCaptureExceptions{TSource, TResult}" />
    public static IncrementalValueProvider<ResultWithDiagnostics<TResult>> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001")
        where TResult : notnull
    {
        var result = source.SelectAndCaptureExceptions(selector, id);

        initializationContext.RegisterSourceOutput(
            result.SelectMany(static (x, _) => x.Diagnostics),
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        return result;
    }

    /// <summary>
    ///     Filters values, reports associated diagnostics, and returns only successful results.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method processes <see cref="ResultWithDiagnostics{T}" /> values by:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Reporting all diagnostics from each result.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Filtering out results where <see cref="ResultWithDiagnostics{T}.HasResult" /> is <c>false</c>.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returning only successful result values.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="source">The provider of results with diagnostics.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only successful result values.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportDiagnostics{T}(IncrementalValueProvider{ResultWithDiagnostics{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="ResultWithDiagnostics{T}" />
    public static IncrementalValuesProvider<T> SelectAndReportDiagnostics<T>(
        this IncrementalValuesProvider<ResultWithDiagnostics<T>> source,
        IncrementalGeneratorInitializationContext initializationContext)
    {
        initializationContext.RegisterSourceOutput(
            source.SelectMany(static (x, _) => x.Diagnostics),
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        return source
            .Where(static x => x.HasResult)
            .Select(static (x, _) =>
                x.HasResult
                    ? x.Result
                    : throw new InvalidOperationException(
                        "Unexpected no-result value produced by SelectAndReportDiagnostics"));
    }

    /// <summary>
    ///     Reports associated diagnostics from a single result and returns the value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method processes a single <see cref="ResultWithDiagnostics{T}" /> value by:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Reporting all diagnostics from the result.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returning the result value, or <c>default</c> when <see cref="ResultWithDiagnostics{T}.HasResult" /> is <c>false</c>.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="source">The provider of a result with diagnostics.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> containing the result value, which may be <c>null</c>.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportDiagnostics{T}(IncrementalValuesProvider{ResultWithDiagnostics{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="ResultWithDiagnostics{T}" />
    public static IncrementalValueProvider<T?> SelectAndReportDiagnostics<T>(
        this IncrementalValueProvider<ResultWithDiagnostics<T>> source,
        IncrementalGeneratorInitializationContext initializationContext)
    {
        initializationContext.RegisterSourceOutput(
            source.SelectMany(static (x, _) => x.Diagnostics),
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        return source
            .Select(static (x, _) => x.HasResult ? x.Result : default);
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a provider of nullable value types and returns non-null values.
    /// </summary>
    /// <typeparam name="TSource">The underlying value type. Must be a struct.</typeparam>
    /// <param name="source">The provider of nullable values to filter.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only non-null values.
    /// </returns>
    /// <seealso cref="WhereNotNull{TSource}(IncrementalValuesProvider{TSource})" />
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source)
        where TSource : struct
    {
        return source
            .Where(static x => x is not null)
            .Select(static (x, _) => x ?? throw new InvalidOperationException("Unexpected null value in WhereNotNull"));
    }

    /// <summary>
    ///     Filters out <c>null</c> values from a provider of nullable reference types and returns non-null values.
    /// </summary>
    /// <typeparam name="TSource">The underlying reference type. Must be a class.</typeparam>
    /// <param name="source">The provider of nullable values to filter.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only non-null values.
    /// </returns>
    /// <seealso cref="WhereNotNull{TSource}(IncrementalValuesProvider{TSource?})" />
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source)
        where TSource : class
    {
        return source
            .Where(static x => x is not null)
            .Select(static (x, _) => x ?? throw new InvalidOperationException("Unexpected null value in WhereNotNull"));
    }

    /// <summary>
    ///     Transforms values using a selector function and reports any exceptions as diagnostics.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method wraps the selector in a try-catch block. If an exception is thrown during transformation,
    ///         it is caught and reported as a diagnostic, allowing the generator to continue processing other items.
    ///     </para>
    ///     <para>
    ///         Use this when you want to gracefully handle exceptions without failing the entire generator.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    /// <param name="source">The provider of source values to transform.</param>
    /// <param name="selector">The transformation function that may throw exceptions.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <param name="id">The diagnostic ID to use when reporting exceptions. Defaults to <c>"SRE001"</c>.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing successfully transformed values.
    ///     Failed transformations are filtered out after their exceptions are reported.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportExceptions{TSource, TResult}(IncrementalValueProvider{TSource}, Func{TSource, CancellationToken, TResult}, IncrementalGeneratorInitializationContext, string)" />
    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001")
    {
        var outputWithErrors = source
            .Select<TSource, (TResult? Value, GeneratorErrorInfo? Error)>((value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return (selector(value, cancellationToken), null);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation must propagate so Roslyn can abort the pipeline.
                    throw;
                }
                catch (Exception exception)
                {
                    return (default, GeneratorErrorInfo.From(exception));
                }
            });

        initializationContext.RegisterSourceOutput(outputWithErrors
                .Where(static x => x.Error is not null),
            (context, tuple) =>
            {
                if (tuple.Error is { } error)
                    context.ReportException(id, error);
            });

        return outputWithErrors
            .Where(static x => x.Error is null)
            .Select(static (x, _) => x.Value!);
    }
}

using System;
using System.Threading;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IncrementalValuesProvider{TValues}" /> and
///     <see cref="IncrementalValueProvider{TValue}" />.
/// </summary>
public static class IncrementalValuesProviderExtensions
{
    /// <summary>
    ///     Registers a source output for files with names.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="context"></param>
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
    ///     Registers a source output for a file with name.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="context"></param>
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
    ///     Registers a source output for a single collected array of files.
    ///     Use after <c>.Collect()</c> when you have one array containing all files.
    /// </summary>
    /// <param name="source">A single value provider producing one array of files.</param>
    /// <param name="context">The generator initialization context.</param>
    /// <example>
    ///     <code>
    /// // After Collect() - one array of files
    /// provider.Collect().Select(Transform).AddSources(context);
    /// </code>
    /// </example>
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
    /// <param name="source">A values provider producing one array per input item.</param>
    /// <param name="context">The generator initialization context.</param>
    /// <example>
    ///     <code>
    /// // Each class produces multiple files
    /// provider.Select(GenerateFilesForClass).AddSources(context);
    /// </code>
    /// </example>
    public static void AddSources(
        this IncrementalValuesProvider<EquatableArray<FileWithName>> source,
        IncrementalGeneratorInitializationContext context)
    {
        source
            .SelectMany(static (x, _) => x)
            .AddSource(context);
    }

    /// <summary>
    ///     Collects values into an <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValueProvider<EquatableArray<TSource>> CollectAsEquatableArray<TSource>(
        this IncrementalValuesProvider<TSource> source)
        where TSource : IEquatable<TSource>
    {
        return source
            .Collect()
            .Select(static (x, _) => x.AsEquatableArray());
    }

    /// <summary>
    ///     Selects values and reports exceptions as diagnostics.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <param name="initializationContext"></param>
    /// <param name="id"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001")
    {
        var outputWithErrors = source
            .Select<TSource, (TResult? Value, Exception? Exception)>((value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return (Value: selector(value, cancellationToken), Exception: null);
                }
                catch (Exception exception)
                {
                    return (Value: default, Exception: exception);
                }
            });

        initializationContext.RegisterSourceOutput(outputWithErrors,
            (context, tuple) =>
            {
                if (tuple.Exception is null) return;

                context.ReportException(id, tuple.Exception);
            });

        return outputWithErrors
            .Select(static (x, _) => x.Value!);
    }

    /// <summary>
    ///     Selects values and reports diagnostics.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="initializationContext"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IncrementalValuesProvider<T> SelectAndReportDiagnostics<T>(
        this IncrementalValuesProvider<ResultWithDiagnostics<T?>> source,
        IncrementalGeneratorInitializationContext initializationContext)
    {
        initializationContext.RegisterSourceOutput(
            source.SelectMany(static (x, _) => x.Diagnostics),
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        return source
            .Where(static x => x.Result is not null)
            .Select(static (x, _) => x.Result!);
    }

    /// <summary>
    ///     Selects a value and reports diagnostics.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="initializationContext"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IncrementalValueProvider<T?> SelectAndReportDiagnostics<T>(
        this IncrementalValueProvider<ResultWithDiagnostics<T?>> source,
        IncrementalGeneratorInitializationContext initializationContext)
    {
        initializationContext.RegisterSourceOutput(
            source.SelectMany(static (x, _) => x.Diagnostics),
            static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

        return source
            .Select(static (x, _) => x.Result);
    }

    /// <summary>
    ///     Selects values and reports exceptions as diagnostics.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <param name="initializationContext"></param>
    /// <param name="id"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source,
        Func<TSource, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001")
    {
        return source
            .SelectAndReportExceptions((x, _) => selector(x), initializationContext, id);
    }

    /// <summary>
    ///     Filters nullable values and select non-nullable values.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source)
        where TSource : struct
    {
        return source
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value);
    }

    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    extension<TSource>(IncrementalValuesProvider<TSource> source)
    {
        /// <summary>
        ///     Selects values and reports exceptions as diagnostics.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="initializationContext"></param>
        /// <param name="id"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public IncrementalValuesProvider<TResult> SelectAndReportExceptions<TResult>(
            Func<TSource, CancellationToken, TResult> selector,
            IncrementalGeneratorInitializationContext initializationContext,
            string id = "SRE001")
        {
            var outputWithErrors = source
                .Select<TSource, (TResult? Value, Exception? Exception)>((value, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        return (Value: selector(value, cancellationToken), Exception: null);
                    }
                    catch (Exception exception)
                    {
                        return (Value: default, Exception: exception);
                    }
                });

            initializationContext.RegisterSourceOutput(outputWithErrors
                    .Where(static x => x.Exception is not null),
                (context, tuple) => { context.ReportException(id, tuple.Exception!); });

            return outputWithErrors
                .Where(static x => x.Exception is null)
                .Select(static (x, _) => x.Value!);
        }

        /// <summary>
        ///     Selects values and reports exceptions as diagnostics.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="initializationContext"></param>
        /// <param name="id"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public IncrementalValuesProvider<TResult> SelectAndReportExceptions<TResult>(Func<TSource, TResult> selector,
            IncrementalGeneratorInitializationContext initializationContext,
            string id = "SRE001")
        {
            return source
                .SelectAndReportExceptions((x, _) => selector(x), initializationContext, id);
        }
    }
}
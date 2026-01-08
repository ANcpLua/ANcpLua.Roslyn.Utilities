using System.Collections.Immutable;
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
        IncrementalGeneratorInitializationContext context) =>
        context.RegisterSourceOutput(source, static (context, file) =>
        {
            if (file.IsEmpty) return;

            context.AddSource(
                file.Name,
                file.Text);
        });

    /// <summary>
    ///     Registers a source output for a file with name.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="context"></param>
    public static void AddSource(
        this IncrementalValueProvider<FileWithName> source,
        IncrementalGeneratorInitializationContext context) =>
        context.RegisterSourceOutput(source, static (context, file) =>
        {
            if (file.IsEmpty) return;

            context.AddSource(
                file.Name,
                file.Text);
        });

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
        IncrementalGeneratorInitializationContext context) =>
        source
            .SelectMany(static (x, _) => x)
            .AddSource(context);

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
        IncrementalGeneratorInitializationContext context) =>
        source
            .SelectMany(static (x, _) => x)
            .AddSource(context);

    /// <summary>
    ///     Collects values into an <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValueProvider<EquatableArray<TSource>> CollectAsEquatableArray<TSource>(
        this IncrementalValuesProvider<TSource> source)
        where TSource : IEquatable<TSource> =>
        source
            .Collect()
            .Select(static (x, _) => x.AsEquatableArray());

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
            .Select(static (x, _) =>
                x.Value ?? throw new InvalidOperationException("Unexpected null value in SelectAndReportExceptions"));
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
            .Select(static (x, _) =>
                x.Result ?? throw new InvalidOperationException(
                    "Unexpected null result produced by SelectAndReportDiagnostics"));
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
    ///     Filters nullable values and select non-nullable values.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source)
        where TSource : struct =>
        source
            .Where(static x => x is not null)
            .Select(static (x, _) => x ?? throw new InvalidOperationException("Unexpected null value in WhereNotNull"));

    /// <summary>
    ///     Filters nullable values and select non-nullable values.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source)
        where TSource : class =>
        source
            .Where(static x => x is not null)
            .Select(static (x, _) => x ?? throw new InvalidOperationException("Unexpected null value in WhereNotNull"));

    /// <summary>
    ///     Selects values and reports exceptions as diagnostics.
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="initializationContext"></param>
    /// <param name="id"></param>
    /// <param name="source"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source, Func<TSource, CancellationToken, TResult> selector,
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
            (context, tuple) =>
            {
                context.ReportException(id,
                    tuple.Exception ??
                    throw new InvalidOperationException(
                        "Unexpected null exception in SelectAndReportExceptions"));
            });

        return outputWithErrors
            .Where(static x => x.Exception is null)
            .Select(static (x, _) => x.Value ??
                                     throw new InvalidOperationException(
                                         "Unexpected null value in SelectAndReportExceptions"));
    }

    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TElement> Elements)> GroupBy<TSource, TKey, TElement>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : IEquatable<TKey>
        where TElement : IEquatable<TElement>
    {
        comparer ??= EqualityComparer<TKey>.Default;
        return source.Collect().SelectMany((values, _) =>
        {
            var map = new Dictionary<TKey, ImmutableArray<TElement>.Builder>(comparer);
            foreach (var value in values)
            {
                var key = keySelector(value);
                if (!map.TryGetValue(key, out var builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();
                    map.Add(key, builder);
                }

                builder.Add(elementSelector(value));
            }

            var result = ImmutableArray.CreateBuilder<(TKey, EquatableArray<TElement>)>(map.Count);
            foreach (var entry in map)
                result.Add((entry.Key, entry.Value.ToImmutable().AsEquatableArray()));
            return result.MoveToImmutable();
        });
    }

    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TSource> Elements)> GroupBy<TSource, TKey>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : IEquatable<TKey>
        where TSource : IEquatable<TSource> =>
        source.GroupBy(keySelector, static x => x, comparer);

    public static IncrementalValuesProvider<(T Value, int Index)> WithIndex<T>(
        this IncrementalValuesProvider<T> source) =>
        source.Collect().SelectMany((values, _) =>
        {
            var result = ImmutableArray.CreateBuilder<(T, int)>(values.Length);
            for (var i = 0; i < values.Length; i++)
                result.Add((values[i], i));
            return result.MoveToImmutable();
        });

    public static IncrementalValuesProvider<T> Distinct<T>(
        this IncrementalValuesProvider<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        return source.Collect().SelectMany((values, _) =>
        {
            var seen = new HashSet<T>(comparer);
            var result = ImmutableArray.CreateBuilder<T>();
            foreach (var value in values)
            {
                if (seen.Add(value))
                    result.Add(value);
            }

            return result.ToImmutable();
        });
    }

    public static IncrementalValueProvider<(EquatableArray<TLeft> Left, TRight Right)> CombineWithCollected<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
        where TLeft : IEquatable<TLeft> =>
        left.CollectAsEquatableArray().Combine(right);

    public static IncrementalValuesProvider<EquatableArray<T>> Batch<T>(
        this IncrementalValuesProvider<T> source,
        int batchSize)
        where T : IEquatable<T>
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

        return source.Collect().SelectMany((values, _) =>
        {
            if (values.IsEmpty)
                return ImmutableArray<EquatableArray<T>>.Empty;

            var batches = ImmutableArray.CreateBuilder<EquatableArray<T>>();
            var batch = ImmutableArray.CreateBuilder<T>(batchSize);

            foreach (var value in values)
            {
                batch.Add(value);
                if (batch.Count == batchSize)
                {
                    batches.Add(batch.ToImmutable().AsEquatableArray());
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                batches.Add(batch.ToImmutable().AsEquatableArray());

            return batches.ToImmutable();
        });
    }

    public static IncrementalValuesProvider<T> Take<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source.Where(_ => false);

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? values : values.Take(count).ToImmutableArray());
    }

    public static IncrementalValuesProvider<T> Skip<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source;

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? ImmutableArray<T>.Empty : values.Skip(count).ToImmutableArray());
    }

    public static IncrementalValueProvider<int> Count<T>(
        this IncrementalValuesProvider<T> source) =>
        source.Collect().Select((values, _) => values.Length);

    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source) =>
        source.Collect().Select((values, _) => !values.IsEmpty);

    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source,
        Func<T, bool> predicate) =>
        source.Collect().Select((values, _) =>
        {
            foreach (var value in values)
            {
                if (predicate(value))
                    return true;
            }

            return false;
        });

    public static IncrementalValueProvider<T?> FirstOrDefault<T>(
        this IncrementalValuesProvider<T> source) =>
        source.Collect().Select((values, _) => values.IsEmpty ? default : values[0]);

    // ========== DiagnosticFlow Pipeline Integration ==========

    /// <summary>
    /// Reports all diagnostics from flows and continues with successful values only.
    /// </summary>
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

        // Return only successful values
        return source
            .Where(static flow => flow.IsSuccess)
            .Select(static (flow, _) => flow.Value!);
    }

    /// <summary>
    /// Reports all diagnostics from a single flow and continues with value if successful.
    /// </summary>
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
    /// Reports diagnostics and stops processing if any errors exist.
    /// </summary>
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

        // Only continue with values that have no errors
        return source
            .Where(static flow => flow.IsSuccess)
            .Select(static (flow, _) => flow.Value!);
    }

    /// <summary>
    /// Collects flows into a single flow. All must succeed for result to succeed.
    /// </summary>
    public static IncrementalValueProvider<DiagnosticFlow<ImmutableArray<T>>> CollectFlows<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source) =>
        source.Collect().Select(static (flows, _) => DiagnosticFlow.Collect((IEnumerable<DiagnosticFlow<T>>)flows));

    /// <summary>
    /// Transform values into flows.
    /// </summary>
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> SelectFlow<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, CancellationToken, DiagnosticFlow<TResult>> selector) =>
        source.Select((value, ct) => selector(value, ct));

    /// <summary>
    /// Transform values into flows, with simple selector.
    /// </summary>
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> SelectFlow<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, DiagnosticFlow<TResult>> selector) =>
        source.Select((value, _) => selector(value));

    /// <summary>
    /// Chain flow transformations.
    /// </summary>
    public static IncrementalValuesProvider<DiagnosticFlow<TResult>> ThenFlow<TSource, TResult>(
        this IncrementalValuesProvider<DiagnosticFlow<TSource>> source,
        Func<TSource, DiagnosticFlow<TResult>> selector) =>
        source.Select((flow, _) => flow.Then(selector));

    /// <summary>
    /// Add warning to all flows conditionally.
    /// </summary>
    public static IncrementalValuesProvider<DiagnosticFlow<T>> WarnIf<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        Func<T, bool> condition,
        DiagnosticInfo warning) =>
        source.Select((flow, _) => flow.WarnIf(condition, warning));

    /// <summary>
    /// Filter flows with predicate, failing those that don't match.
    /// </summary>
    public static IncrementalValuesProvider<DiagnosticFlow<T>> WhereFlow<T>(
        this IncrementalValuesProvider<DiagnosticFlow<T>> source,
        Func<T, bool> predicate,
        DiagnosticInfo onFail) =>
        source.Select((flow, _) => flow.Where(predicate, onFail));
}
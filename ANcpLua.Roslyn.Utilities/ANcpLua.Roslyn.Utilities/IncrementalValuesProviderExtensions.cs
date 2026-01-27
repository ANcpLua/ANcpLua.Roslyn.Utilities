using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using InvalidOperationException = System.InvalidOperationException;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for <see cref="IncrementalValuesProvider{TValues}" /> and
///     <see cref="IncrementalValueProvider{TValue}" /> to simplify common operations in incremental source generators.
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
/// <seealso cref="IncrementalValuesProvider{TValues}" />
/// <seealso cref="IncrementalValueProvider{TValue}" />
/// <seealso cref="DiagnosticFlow{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class IncrementalValuesProviderExtensions
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
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="source">The provider of source values to transform.</param>
    /// <param name="selector">The transformation function that may throw exceptions.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <param name="id">The diagnostic ID to use when reporting exceptions. Defaults to <c>"SRE001"</c>.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> that produces the transformed value,
    ///     or throws if the transformation failed.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportExceptions{TSource, TResult}(IncrementalValuesProvider{TSource}, Func{TSource, CancellationToken, TResult}, IncrementalGeneratorInitializationContext, string)" />
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
    ///                 <description>Filtering out results where the value is <c>null</c>.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returning only non-null result values.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="source">The provider of results with diagnostics.</param>
    /// <param name="initializationContext">The generator initialization context for reporting diagnostics.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only non-null result values.
    /// </returns>
    /// <seealso
    ///     cref="SelectAndReportDiagnostics{T}(IncrementalValueProvider{ResultWithDiagnostics{T}}, IncrementalGeneratorInitializationContext)" />
    /// <seealso cref="ResultWithDiagnostics{T}" />
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
    ///                 <description>Returning the result value (which may be <c>null</c>).</description>
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

    /// <summary>
    ///     Groups values by a key and projects each element using a selector.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method collects all values, groups them by the specified key, and returns
    ///         tuples containing the key and an <see cref="EquatableArray{T}" /> of projected elements.
    ///     </para>
    ///     <para>
    ///         The grouping is performed in-memory after collecting all values, which may impact
    ///         incremental caching for large datasets.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TElement">The type of the projected elements. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of source values to group.</param>
    /// <param name="keySelector">A function to extract the grouping key from each source value.</param>
    /// <param name="elementSelector">A function to project each source value into an element.</param>
    /// <param name="comparer">An optional equality comparer for keys. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples, each containing a key
    ///     and an <see cref="EquatableArray{T}" /> of elements with that key.
    /// </returns>
    /// <seealso
    ///     cref="GroupBy{TSource, TKey}(IncrementalValuesProvider{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey})" />
    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TElement> Elements)> GroupBy<TSource, TKey,
        TElement>(
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

    /// <summary>
    ///     Groups values by a key without projecting elements.
    /// </summary>
    /// <remarks>
    ///     This is a convenience overload that uses the source values directly as elements.
    /// </remarks>
    /// <typeparam name="TSource">The type of the source values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of source values to group.</param>
    /// <param name="keySelector">A function to extract the grouping key from each source value.</param>
    /// <param name="comparer">An optional equality comparer for keys. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples, each containing a key
    ///     and an <see cref="EquatableArray{T}" /> of source values with that key.
    /// </returns>
    /// <seealso
    ///     cref="GroupBy{TSource, TKey, TElement}(IncrementalValuesProvider{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, IEqualityComparer{TKey})" />
    public static IncrementalValuesProvider<(TKey Key, EquatableArray<TSource> Elements)> GroupBy<TSource, TKey>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : IEquatable<TKey>
        where TSource : IEquatable<TSource>
    {
        return source.GroupBy(keySelector, static x => x, comparer);
    }

    /// <summary>
    ///     Projects each value with its zero-based index in the collection.
    /// </summary>
    /// <remarks>
    ///     This method collects all values first, then projects each with its index.
    ///     The index is determined by the order in which values appear in the collected array.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to index.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of tuples containing each value
    ///     paired with its zero-based index.
    /// </returns>
    public static IncrementalValuesProvider<(T Value, int Index)> WithIndex<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().SelectMany((values, _) =>
        {
            var result = ImmutableArray.CreateBuilder<(T, int)>(values.Length);
            for (var i = 0; i < values.Length; i++)
                result.Add((values[i], i));
            return result.MoveToImmutable();
        });
    }

    /// <summary>
    ///     Returns distinct values from the provider, removing duplicates.
    /// </summary>
    /// <remarks>
    ///     This method collects all values and returns only the first occurrence of each unique value.
    ///     The order of first occurrences is preserved.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to filter for uniqueness.</param>
    /// <param name="comparer">An optional equality comparer. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing only distinct values.
    /// </returns>
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
                if (seen.Add(value))
                    result.Add(value);

            return result.ToImmutable();
        });
    }

    /// <summary>
    ///     Combines a collected values provider with a single value provider.
    /// </summary>
    /// <remarks>
    ///     This is a convenience method that collects the left provider as an <see cref="EquatableArray{T}" />
    ///     and combines it with the right provider.
    /// </remarks>
    /// <typeparam name="TLeft">The type of the left values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <typeparam name="TRight">The type of the right value.</typeparam>
    /// <param name="left">The provider of values to collect.</param>
    /// <param name="right">The single value provider to combine with.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing a tuple of the collected array and the right value.
    /// </returns>
    public static IncrementalValueProvider<(EquatableArray<TLeft> Left, TRight Right)> CombineWithCollected<TLeft,
        TRight>(
        this IncrementalValuesProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
        where TLeft : IEquatable<TLeft> =>
        left.CollectAsEquatableArray().Combine(right);

    /// <summary>
    ///     Expressive alias for <c>IncrementalValueProvider.Combine</c>.
    /// </summary>
    public static IncrementalValueProvider<(TLeft Left, TRight Right)> CombineWith<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> left,
        IncrementalValueProvider<TRight> right) =>
        left.Combine(right);

    /// <summary>
    ///     Expressive alias for <c>IncrementalValuesProvider.Combine</c>.
    /// </summary>
    public static IncrementalValuesProvider<(TLeft Left, TRight Right)> CombineWith<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> left,
        IncrementalValueProvider<TRight> right) =>
        left.Combine(right);

    /// <summary>
    ///     Splits values into batches of a specified size.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values are collected and then split into fixed-size batches. The last batch may contain
    ///         fewer elements if the total count is not evenly divisible by the batch size.
    ///     </para>
    ///     <para>
    ///         An empty source produces no batches.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of the values. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The provider of values to batch.</param>
    /// <param name="batchSize">The maximum number of elements per batch. Must be positive.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> of <see cref="EquatableArray{T}" /> batches.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize" /> is less than or equal to zero.</exception>
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

    /// <summary>
    ///     Returns a specified number of values from the start of the provider.
    /// </summary>
    /// <remarks>
    ///     If the source contains fewer values than requested, all values are returned.
    ///     If <paramref name="count" /> is zero or negative, no values are returned.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <param name="count">The number of values to take from the start.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing at most <paramref name="count" /> values.
    /// </returns>
    /// <seealso cref="Skip{T}(IncrementalValuesProvider{T}, int)" />
    public static IncrementalValuesProvider<T> Take<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source.Where(_ => false);

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? values : [..values.Take(count)]);
    }

    /// <summary>
    ///     Skips a specified number of values from the start of the provider.
    /// </summary>
    /// <remarks>
    ///     If the source contains fewer values than the skip count, an empty result is returned.
    ///     If <paramref name="count" /> is zero or negative, all values are returned.
    /// </remarks>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <param name="count">The number of values to skip from the start.</param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{TValues}" /> containing values after skipping the specified count.
    /// </returns>
    /// <seealso cref="Take{T}(IncrementalValuesProvider{T}, int)" />
    public static IncrementalValuesProvider<T> Skip<T>(
        this IncrementalValuesProvider<T> source,
        int count)
    {
        if (count <= 0)
            return source;

        return source.Collect().SelectMany((values, _) =>
            values.Length <= count ? ImmutableArray<T>.Empty : [..values.Skip(count)]);
    }

    /// <summary>
    ///     Returns the count of values in the provider.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to count.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing the number of values.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<int> Count<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select((values, _) => values.Length);
    }

    /// <summary>
    ///     Determines whether the provider contains any values.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to check.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing <c>true</c> if the provider
    ///     contains at least one value; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T}, Func{T, bool})" />
    /// <seealso cref="Count{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select((values, _) => !values.IsEmpty);
    }

    /// <summary>
    ///     Determines whether any value in the provider satisfies a condition.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values to check.</param>
    /// <param name="predicate">A function to test each value for a condition.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing <c>true</c> if any value
    ///     satisfies the predicate; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}(IncrementalValuesProvider{T})" />
    public static IncrementalValueProvider<bool> Any<T>(
        this IncrementalValuesProvider<T> source,
        Func<T, bool> predicate)
    {
        return source.Collect().Select((values, _) =>
        {
            foreach (var value in values)
                if (predicate(value))
                    return true;

            return false;
        });
    }

    /// <summary>
    ///     Returns the first value in the provider, or a default value if the provider is empty.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The provider of values.</param>
    /// <returns>
    ///     An <see cref="IncrementalValueProvider{TValue}" /> producing the first value,
    ///     or the default value of <typeparamref name="T" /> if the provider is empty.
    /// </returns>
    public static IncrementalValueProvider<T?> FirstOrDefault<T>(
        this IncrementalValuesProvider<T> source)
    {
        return source.Collect().Select((values, _) => values.IsEmpty ? default : values[0]);
    }

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
                ? ImmutableArray.Create(value)
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
                ? ImmutableArray.Create(value)
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
        return source.Select((value, ct) => selector(value, ct));
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

using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Railway-oriented result type for incremental generator pipelines.
///     <para>
///         Carries a value <b>and</b> accumulated diagnostics through transformations,
///         enabling error handling without exceptions while preserving all warnings and errors.
///     </para>
/// </summary>
/// <typeparam name="T">The type of the wrapped value.</typeparam>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description><b>Immutable.</b> All operations return new instances; the original flow is never modified.</description>
///         </item>
///         <item>
///             <description>
///                 <b>Short-circuiting.</b> Once failed, subsequent <see cref="Then{TNext}" /> and
///                 <see cref="Select{TNext}" />
///                 operations preserve existing diagnostics without invoking transformations.
///             </description>
///         </item>
///         <item>
///             <description><b>Equatable.</b> Implements value equality for incremental generator caching.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="DiagnosticFlow" />
/// <seealso cref="DiagnosticFlowReportingExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct DiagnosticFlow<T> : IEquatable<DiagnosticFlow<T>>
{
    /// <summary>Gets the wrapped value, or <c>default</c> if this flow has failed.</summary>
    public T? Value { get; }

    /// <summary>Gets the accumulated diagnostics (warnings and errors) from all operations.</summary>
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    /// <summary>
    ///     Gets a value indicating whether this flow contains any error-severity diagnostics.
    /// </summary>
    /// <value><c>true</c> if any diagnostic has <see cref="DiagnosticSeverity.Error" />; otherwise, <c>false</c>.</value>
    public bool HasErrors
    {
        get
        {
            foreach (var d in Diagnostics.AsImmutableArray())
                // Defensive: guard against default(DiagnosticInfo) with null Descriptor
                if (d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error)
                    return true;
            return false;
        }
    }

    /// <summary>Gets a value indicating whether this flow has a non-null value and no errors.</summary>
    /// <value><c>true</c> if <see cref="Value" /> is not <c>null</c> and <see cref="HasErrors" /> is <c>false</c>.</value>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => Value is not null && !HasErrors;

    /// <summary>Gets a value indicating whether this flow has failed (null value or errors).</summary>
    /// <value>The inverse of <see cref="IsSuccess" />.</value>
    public bool IsFailed => !IsSuccess;

    internal DiagnosticFlow(T? value, EquatableArray<DiagnosticInfo> diagnostics)
    {
        Value = value;
        Diagnostics = diagnostics;
    }

    /// <summary>
    ///     Chains a transformation that produces a new <see cref="DiagnosticFlow{T}" />,
    ///     merging diagnostics from both operations.
    /// </summary>
    /// <typeparam name="TNext">The result type of the chained operation.</typeparam>
    /// <param name="next">
    ///     A function that transforms the current value into a new flow.
    ///     Only invoked if this flow is successful.
    /// </param>
    /// <returns>
    ///     A new <see cref="DiagnosticFlow{TNext}" /> containing the transformed value
    ///     and merged diagnostics, or a failed flow preserving existing diagnostics if this flow has errors.
    /// </returns>
    /// <seealso cref="Select{TNext}" />
    public DiagnosticFlow<TNext> Then<TNext>(Func<T, DiagnosticFlow<TNext>> next)
    {
        if (IsFailed || Value is null)
            return new DiagnosticFlow<TNext>(default, Diagnostics);

        var result = next(Value);
        return new DiagnosticFlow<TNext>(result.Value,
            DiagnosticFlowHelpers.MergeDiagnostics(Diagnostics, result.Diagnostics));
    }

    /// <summary>
    ///     Transforms the value using a simple mapping function, preserving diagnostics.
    /// </summary>
    /// <typeparam name="TNext">The result type of the mapping.</typeparam>
    /// <param name="map">
    ///     A function that transforms the current value.
    ///     Only invoked if this flow is successful.
    /// </param>
    /// <returns>
    ///     A new <see cref="DiagnosticFlow{TNext}" /> with the transformed value and same diagnostics,
    ///     or a failed flow preserving existing diagnostics if this flow has errors.
    /// </returns>
    /// <seealso cref="Then{TNext}" />
    public DiagnosticFlow<TNext> Select<TNext>(Func<T, TNext> map)
    {
        if (IsFailed || Value is null)
            return new DiagnosticFlow<TNext>(default, Diagnostics);

        return new DiagnosticFlow<TNext>(map(Value), Diagnostics);
    }

    /// <summary>
    ///     Appends a warning diagnostic to this flow without affecting success state.
    /// </summary>
    /// <param name="warning">The warning diagnostic to append.</param>
    /// <returns>A new flow with the warning appended to <see cref="Diagnostics" />.</returns>
    /// <seealso cref="WarnIf" />
    /// <seealso cref="Error" />
    public DiagnosticFlow<T> Warn(DiagnosticInfo warning) => new(Value, DiagnosticFlowHelpers.AppendDiagnostic(Diagnostics, warning));

    /// <summary>
    ///     Conditionally appends a warning diagnostic based on the current value.
    /// </summary>
    /// <param name="condition">A predicate evaluated against the current value.</param>
    /// <param name="warning">The warning diagnostic to append if <paramref name="condition" /> returns <c>true</c>.</param>
    /// <returns>A new flow with the warning appended if the condition is met; otherwise, this flow unchanged.</returns>
    /// <seealso cref="Warn" />
    /// <seealso cref="ErrorIf" />
    public DiagnosticFlow<T> WarnIf(Func<T, bool> condition, DiagnosticInfo warning)
    {
        if (Value is not null && condition(Value))
            return Warn(warning);
        return this;
    }

    /// <summary>
    ///     Appends an error diagnostic and sets the value to <c>default</c>, causing the flow to fail.
    /// </summary>
    /// <param name="error">The error diagnostic to append.</param>
    /// <returns>A new failed flow with the error appended to <see cref="Diagnostics" />.</returns>
    /// <seealso cref="ErrorIf" />
    /// <seealso cref="Warn" />
    public DiagnosticFlow<T> Error(DiagnosticInfo error) => new(default, DiagnosticFlowHelpers.AppendDiagnostic(Diagnostics, error));

    /// <summary>
    ///     Conditionally appends an error diagnostic based on the current value.
    /// </summary>
    /// <param name="condition">A predicate evaluated against the current value.</param>
    /// <param name="error">The error diagnostic to append if <paramref name="condition" /> returns <c>true</c>.</param>
    /// <returns>A new failed flow with the error if the condition is met; otherwise, this flow unchanged.</returns>
    /// <seealso cref="Error" />
    /// <seealso cref="WarnIf" />
    public DiagnosticFlow<T> ErrorIf(Func<T, bool> condition, DiagnosticInfo error)
    {
        if (Value is not null && condition(Value))
            return Error(error);
        return this;
    }

    /// <summary>
    ///     Filters the flow based on a predicate, failing with the specified diagnostic if not satisfied.
    /// </summary>
    /// <param name="predicate">A predicate that must return <c>true</c> for the flow to continue successfully.</param>
    /// <param name="onFail">The error diagnostic to report if <paramref name="predicate" /> returns <c>false</c>.</param>
    /// <returns>This flow unchanged if the predicate is satisfied; otherwise, a failed flow with <paramref name="onFail" />.</returns>
    public DiagnosticFlow<T> Where(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (IsFailed || Value is null)
            return this;

        return predicate(Value) ? this : Error(onFail);
    }

    /// <summary>
    ///     Executes a side-effect action if the flow is successful.
    /// </summary>
    /// <param name="action">The action to execute with the current value.</param>
    /// <returns>This flow unchanged (for chaining).</returns>
    public DiagnosticFlow<T> Do(Action<T> action)
    {
        if (IsSuccess && Value is not null)
            action(Value);
        return this;
    }

    /// <summary>
    ///     Returns the value if successful, or a default value if failed.
    /// </summary>
    /// <param name="defaultValue">The value to return if this flow has failed.</param>
    /// <returns>The <see cref="Value" /> if successful; otherwise, <paramref name="defaultValue" />.</returns>
    public T? ValueOrDefault(T? defaultValue = default) => IsSuccess ? Value : defaultValue;

    /// <summary>
    ///     Pattern matches on the flow state, invoking one of two functions.
    /// </summary>
    /// <typeparam name="TResult">The return type of both functions.</typeparam>
    /// <param name="onSuccess">Function invoked with the value if the flow is successful.</param>
    /// <param name="onFailed">Function invoked with the diagnostics if the flow has failed.</param>
    /// <returns>The result of whichever function is invoked.</returns>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<EquatableArray<DiagnosticInfo>, TResult> onFailed)
    {
        if (IsSuccess && Value is not null)
            return onSuccess(Value);
        return onFailed(Diagnostics);
    }

    /// <summary>
    ///     Implicitly converts a value to a successful <see cref="DiagnosticFlow{T}" />.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A successful flow containing <paramref name="value" /> with no diagnostics.</returns>
    public static implicit operator DiagnosticFlow<T>(T value) => DiagnosticFlow.Ok(value);

    /// <inheritdoc />
    public bool Equals(DiagnosticFlow<T> other) =>
        EqualityComparer<T?>.Default.Equals(Value, other.Value) &&
        Diagnostics.Equals(other.Diagnostics);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DiagnosticFlow<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = HashCombiner.Create();
        hash.Add(Value);
        hash.Add(Diagnostics);
        return hash.ToHashCode();
    }

    /// <summary>Determines whether two flows are equal.</summary>
    public static bool operator ==(DiagnosticFlow<T> left, DiagnosticFlow<T> right) => left.Equals(right);

    /// <summary>Determines whether two flows are not equal.</summary>
    public static bool operator !=(DiagnosticFlow<T> left, DiagnosticFlow<T> right) => !left.Equals(right);
}

/// <summary>
///     Factory methods for creating <see cref="DiagnosticFlow{T}" /> instances.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DiagnosticFlow
{
    /// <summary>
    ///     Creates a successful flow containing the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A successful <see cref="DiagnosticFlow{T}" /> with no diagnostics.</returns>
    public static DiagnosticFlow<T> Ok<T>(T value) => new(value, default);

    /// <summary>
    ///     Creates a failed flow with a single error diagnostic.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="error">The error diagnostic.</param>
    /// <returns>A failed <see cref="DiagnosticFlow{T}" /> containing the error.</returns>
    public static DiagnosticFlow<T> Fail<T>(DiagnosticInfo error) => new(default, ImmutableArray.Create(error).AsEquatableArray());

    /// <summary>
    ///     Creates a failed flow with multiple error diagnostics.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="errors">The error diagnostics.</param>
    /// <returns>A failed <see cref="DiagnosticFlow{T}" /> containing all errors.</returns>
    public static DiagnosticFlow<T> Fail<T>(params DiagnosticInfo[] errors) => new(default, errors.ToImmutableArray().AsEquatableArray());

    /// <summary>
    ///     Creates a failed flow with pre-collected error diagnostics.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="errors">The error diagnostics array.</param>
    /// <returns>A failed <see cref="DiagnosticFlow{T}" /> containing all errors.</returns>
    public static DiagnosticFlow<T> Fail<T>(EquatableArray<DiagnosticInfo> errors) => new(default, errors);

    /// <summary>
    ///     Creates a flow from a nullable reference, failing with the specified diagnostic if <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable value to wrap.</param>
    /// <param name="onNull">The error diagnostic to use if <paramref name="value" /> is <c>null</c>.</param>
    /// <returns>A successful flow if not null; otherwise, a failed flow with <paramref name="onNull" />.</returns>
    public static DiagnosticFlow<T> FromNullable<T>(T? value, DiagnosticInfo onNull) where T : class => value is not null ? Ok(value) : Fail<T>(onNull);

    /// <summary>
    ///     Creates a flow from a nullable value type, failing with the specified diagnostic if <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value to wrap.</param>
    /// <param name="onNull">The error diagnostic to use if <paramref name="value" /> has no value.</param>
    /// <returns>A successful flow if has value; otherwise, a failed flow with <paramref name="onNull" />.</returns>
    public static DiagnosticFlow<T> FromNullable<T>(T? value, DiagnosticInfo onNull) where T : struct => value.HasValue ? Ok(value.Value) : Fail<T>(onNull);

    /// <summary>
    ///     Creates a flow by invoking a factory, converting exceptions to diagnostics.
    /// </summary>
    /// <typeparam name="T">The type of the value to create.</typeparam>
    /// <param name="factory">A function that produces the value.</param>
    /// <param name="onException">A function that converts exceptions to error diagnostics.</param>
    /// <returns>A successful flow with the factory result, or a failed flow if an exception was thrown.</returns>
    public static DiagnosticFlow<T> Try<T>(Func<T> factory, Func<Exception, DiagnosticInfo> onException)
    {
        try
        {
            return Ok(factory());
        }
        catch (Exception ex)
        {
            return Fail<T>(onException(ex));
        }
    }

    /// <summary>
    ///     Combines two flows into a tuple, merging all diagnostics.
    /// </summary>
    /// <typeparam name="T1">The first flow's value type.</typeparam>
    /// <typeparam name="T2">The second flow's value type.</typeparam>
    /// <param name="first">The first flow.</param>
    /// <param name="second">The second flow.</param>
    /// <returns>
    ///     A successful flow with both values as a tuple if both succeed;
    ///     otherwise, a failed flow with merged diagnostics from both.
    /// </returns>
    public static DiagnosticFlow<(T1, T2)> Zip<T1, T2>(
        DiagnosticFlow<T1> first,
        DiagnosticFlow<T2> second)
    {
        var diagnostics = DiagnosticFlowHelpers.MergeDiagnostics(first.Diagnostics, second.Diagnostics);

        if (first.IsFailed || second.IsFailed || first.Value is null || second.Value is null)
            return new DiagnosticFlow<(T1, T2)>(default, diagnostics);

        return new DiagnosticFlow<(T1, T2)>((first.Value, second.Value), diagnostics);
    }

    /// <summary>
    ///     Combines three flows into a tuple, merging all diagnostics.
    /// </summary>
    /// <typeparam name="T1">The first flow's value type.</typeparam>
    /// <typeparam name="T2">The second flow's value type.</typeparam>
    /// <typeparam name="T3">The third flow's value type.</typeparam>
    /// <param name="first">The first flow.</param>
    /// <param name="second">The second flow.</param>
    /// <param name="third">The third flow.</param>
    /// <returns>
    ///     A successful flow with all values as a tuple if all succeed;
    ///     otherwise, a failed flow with merged diagnostics from all three.
    /// </returns>
    public static DiagnosticFlow<(T1, T2, T3)> Zip<T1, T2, T3>(
        DiagnosticFlow<T1> first,
        DiagnosticFlow<T2> second,
        DiagnosticFlow<T3> third)
    {
        var diagnostics = DiagnosticFlowHelpers.MergeDiagnostics(
            DiagnosticFlowHelpers.MergeDiagnostics(first.Diagnostics, second.Diagnostics),
            third.Diagnostics);

        if (first.IsFailed || second.IsFailed || third.IsFailed ||
            first.Value is null || second.Value is null || third.Value is null)
            return new DiagnosticFlow<(T1, T2, T3)>(default, diagnostics);

        return new DiagnosticFlow<(T1, T2, T3)>((first.Value, second.Value, third.Value), diagnostics);
    }

    /// <summary>
    ///     Collects multiple flows into a single flow containing all successful values.
    ///     <para>
    ///         If any flow has error-severity diagnostics, the result is a failed flow.
    ///         All diagnostics (warnings and errors) are preserved in the result.
    ///     </para>
    /// </summary>
    /// <typeparam name="TItem">The type of items in each flow.</typeparam>
    /// <param name="flows">The flows to collect.</param>
    /// <returns>
    ///     A flow containing an array of all successful values if no errors occurred;
    ///     otherwise, a failed flow with all accumulated diagnostics.
    /// </returns>
    /// <seealso cref="Sequence{T}" />
    public static DiagnosticFlow<ImmutableArray<TItem>> Collect<TItem>(
        IEnumerable<DiagnosticFlow<TItem>> flows)
    {
        var values = ImmutableArray.CreateBuilder<TItem>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (var flow in flows)
        {
            foreach (var d in flow.Diagnostics.AsImmutableArray())
                diagnostics.Add(d);

            if (flow is { IsSuccess: true, Value: not null })
                values.Add(flow.Value);
        }

        var hasErrors = false;
        foreach (var d in diagnostics)
            if (d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error)
            {
                hasErrors = true;
                break;
            }

        var diagArray = diagnostics.Count > 0 ? diagnostics.ToImmutable().AsEquatableArray() : default;

        return hasErrors ? new DiagnosticFlow<ImmutableArray<TItem>>(default, diagArray) : new DiagnosticFlow<ImmutableArray<TItem>>(values.ToImmutable(), diagArray);
    }

    /// <summary>
    ///     Alias for <see cref="Collect{TItem}" /> as an extension method.
    /// </summary>
    /// <typeparam name="T">The type of items in each flow.</typeparam>
    /// <param name="flows">The flows to sequence.</param>
    /// <returns>A flow containing all successful values, or a failed flow if any had errors.</returns>
    /// <seealso cref="Collect{TItem}" />
    public static DiagnosticFlow<ImmutableArray<T>> Sequence<T>(
        this IEnumerable<DiagnosticFlow<T>> flows) =>
        Collect(flows);
}

file static class DiagnosticFlowHelpers
{
    public static EquatableArray<DiagnosticInfo> MergeDiagnostics(
        EquatableArray<DiagnosticInfo> first,
        EquatableArray<DiagnosticInfo> second)
    {
        if (first.IsEmpty)
            return second;
        if (second.IsEmpty)
            return first;

        var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>(first.Length + second.Length);
        foreach (var item in first.AsImmutableArray())
            builder.Add(item);
        foreach (var item in second.AsImmutableArray())
            builder.Add(item);

        return builder.ToImmutable().AsEquatableArray();
    }

    public static EquatableArray<DiagnosticInfo> AppendDiagnostic(
        EquatableArray<DiagnosticInfo> array,
        DiagnosticInfo item)
    {
        if (array.IsEmpty)
            return ImmutableArray.Create(item).AsEquatableArray();

        var builder = ImmutableArray.CreateBuilder<DiagnosticInfo>(array.Length + 1);
        foreach (var existing in array.AsImmutableArray())
            builder.Add(existing);
        builder.Add(item);

        return builder.ToImmutable().AsEquatableArray();
    }
}

/// <summary>
///     Extension methods for reporting diagnostics from <see cref="DiagnosticFlow{T}" /> in source generators.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DiagnosticFlowReportingExtensions
{
    /// <summary>
    ///     Reports all diagnostics in the flow to the <see cref="SourceProductionContext" />.
    /// </summary>
    /// <typeparam name="T">The flow's value type.</typeparam>
    /// <param name="flow">The flow containing diagnostics to report.</param>
    /// <param name="context">The source production context to report to.</param>
    public static void Report<T>(this DiagnosticFlow<T> flow, SourceProductionContext context)
    {
        foreach (var diagnosticInfo in flow.Diagnostics.AsImmutableArray())
            context.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
    }

    /// <summary>
    ///     Reports all diagnostics and returns the value (or <c>default</c> if failed).
    /// </summary>
    /// <typeparam name="T">The flow's value type.</typeparam>
    /// <param name="flow">The flow to process.</param>
    /// <param name="context">The source production context to report to.</param>
    /// <returns>The <see cref="DiagnosticFlow{T}.Value" /> if successful; otherwise, <c>default</c>.</returns>
    public static T? ReportAndGet<T>(this DiagnosticFlow<T> flow, SourceProductionContext context)
    {
        flow.Report(context);
        return flow.Value;
    }

    /// <summary>
    ///     Reports all diagnostics and executes an action if the flow is successful.
    /// </summary>
    /// <typeparam name="T">The flow's value type.</typeparam>
    /// <param name="flow">The flow to process.</param>
    /// <param name="context">The source production context to report to.</param>
    /// <param name="onSuccess">Action to execute with the value if successful.</param>
    public static void ReportAndDo<T>(
        this DiagnosticFlow<T> flow,
        SourceProductionContext context,
        Action<T> onSuccess)
    {
        flow.Report(context);
        if (flow is { IsSuccess: true, Value: not null })
            onSuccess(flow.Value);
    }

    /// <summary>
    ///     Reports all diagnostics and adds the generated file as source if successful.
    /// </summary>
    /// <param name="flow">A flow containing a <see cref="FileWithName" />.</param>
    /// <param name="context">The source production context to report to and add source to.</param>
    public static void ReportAndAddSource(
        this DiagnosticFlow<FileWithName> flow,
        SourceProductionContext context)
    {
        flow.Report(context);
        if (flow is { IsSuccess: true, Value: { } file })
            context.AddSource(file.Name, file.Text);
    }

    /// <summary>
    ///     Reports all diagnostics and adds all generated files as sources if successful.
    /// </summary>
    /// <param name="flow">A flow containing multiple <see cref="FileWithName" /> instances.</param>
    /// <param name="context">The source production context to report to and add sources to.</param>
    public static void ReportAndAddSources(
        this DiagnosticFlow<ImmutableArray<FileWithName>> flow,
        SourceProductionContext context)
    {
        flow.Report(context);
        if (flow is { IsSuccess: true, Value: var files })
            foreach (var file in files)
                context.AddSource(file.Name, file.Text);
    }
}
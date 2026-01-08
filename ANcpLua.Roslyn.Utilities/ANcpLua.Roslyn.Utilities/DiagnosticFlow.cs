using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
/// Railway-oriented result type for generator pipelines.
/// Carries a value AND accumulated diagnostics through transformations.
/// </summary>
public readonly struct DiagnosticFlow<T> : IEquatable<DiagnosticFlow<T>>
{
    public T? Value { get; }
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool HasErrors
    {
        get
        {
            foreach (var d in Diagnostics.AsImmutableArray())
            {
                // Defensive: guard against default(DiagnosticInfo) with null Descriptor
                if (d.Descriptor?.DefaultSeverity == DiagnosticSeverity.Error)
                    return true;
            }
            return false;
        }
    }

    public bool IsSuccess => Value is not null && !HasErrors;
    public bool IsFailed => !IsSuccess;

    internal DiagnosticFlow(T? value, EquatableArray<DiagnosticInfo> diagnostics)
    {
        Value = value;
        Diagnostics = diagnostics;
    }

    public DiagnosticFlow<TNext> Then<TNext>(Func<T, DiagnosticFlow<TNext>> next)
    {
        if (IsFailed || Value is null)
            return new DiagnosticFlow<TNext>(default, Diagnostics);

        var result = next(Value);
        return new DiagnosticFlow<TNext>(result.Value, DiagnosticFlowHelpers.MergeDiagnostics(Diagnostics, result.Diagnostics));
    }

    public DiagnosticFlow<TNext> Select<TNext>(Func<T, TNext> map)
    {
        if (IsFailed || Value is null)
            return new DiagnosticFlow<TNext>(default, Diagnostics);

        return new DiagnosticFlow<TNext>(map(Value), Diagnostics);
    }

    public DiagnosticFlow<T> Warn(DiagnosticInfo warning) =>
        new(Value, DiagnosticFlowHelpers.AppendDiagnostic(Diagnostics, warning));

    public DiagnosticFlow<T> WarnIf(Func<T, bool> condition, DiagnosticInfo warning)
    {
        if (Value is not null && condition(Value))
            return Warn(warning);
        return this;
    }

    public DiagnosticFlow<T> Error(DiagnosticInfo error) =>
        new(default, DiagnosticFlowHelpers.AppendDiagnostic(Diagnostics, error));

    public DiagnosticFlow<T> ErrorIf(Func<T, bool> condition, DiagnosticInfo error)
    {
        if (Value is not null && condition(Value))
            return Error(error);
        return this;
    }

    public DiagnosticFlow<T> Where(Func<T, bool> predicate, DiagnosticInfo onFail)
    {
        if (IsFailed || Value is null)
            return this;

        return predicate(Value) ? this : Error(onFail);
    }

    public DiagnosticFlow<T> Do(Action<T> action)
    {
        if (IsSuccess && Value is not null)
            action(Value);
        return this;
    }

    public T? ValueOrDefault(T? defaultValue = default) =>
        IsSuccess ? Value : defaultValue;

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<EquatableArray<DiagnosticInfo>, TResult> onFailed)
    {
        if (IsSuccess && Value is not null)
            return onSuccess(Value);
        return onFailed(Diagnostics);
    }

    public static implicit operator DiagnosticFlow<T>(T value) => DiagnosticFlow.Ok(value);

    public bool Equals(DiagnosticFlow<T> other) =>
        EqualityComparer<T?>.Default.Equals(Value, other.Value) &&
        Diagnostics.Equals(other.Diagnostics);

    public override bool Equals(object? obj) =>
        obj is DiagnosticFlow<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Diagnostics);

    public static bool operator ==(DiagnosticFlow<T> left, DiagnosticFlow<T> right) => left.Equals(right);
    public static bool operator !=(DiagnosticFlow<T> left, DiagnosticFlow<T> right) => !left.Equals(right);
}

/// <summary>
/// Factory methods for DiagnosticFlow.
/// </summary>
public static class DiagnosticFlow
{
    public static DiagnosticFlow<T> Ok<T>(T value) =>
        new(value, default);

    public static DiagnosticFlow<T> Fail<T>(DiagnosticInfo error) =>
        new(default, ImmutableArray.Create(error).AsEquatableArray());

    public static DiagnosticFlow<T> Fail<T>(params DiagnosticInfo[] errors) =>
        new(default, errors.ToImmutableArray().AsEquatableArray());

    public static DiagnosticFlow<T> Fail<T>(EquatableArray<DiagnosticInfo> errors) =>
        new(default, errors);

    public static DiagnosticFlow<T> FromNullable<T>(T? value, DiagnosticInfo onNull) where T : class =>
        value is not null ? Ok(value) : Fail<T>(onNull);

    public static DiagnosticFlow<T> FromNullable<T>(T? value, DiagnosticInfo onNull) where T : struct =>
        value.HasValue ? Ok(value.Value) : Fail<T>(onNull);

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

    public static DiagnosticFlow<(T1, T2)> Zip<T1, T2>(
        DiagnosticFlow<T1> first,
        DiagnosticFlow<T2> second)
    {
        var diagnostics = DiagnosticFlowHelpers.MergeDiagnostics(first.Diagnostics, second.Diagnostics);

        if (first.IsFailed || second.IsFailed || first.Value is null || second.Value is null)
            return new DiagnosticFlow<(T1, T2)>(default, diagnostics);

        return new DiagnosticFlow<(T1, T2)>((first.Value, second.Value), diagnostics);
    }

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

    public static DiagnosticFlow<ImmutableArray<TItem>> Collect<TItem>(
        IEnumerable<DiagnosticFlow<TItem>> flows)
    {
        var values = ImmutableArray.CreateBuilder<TItem>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (var flow in flows)
        {
            foreach (var d in flow.Diagnostics.AsImmutableArray())
                diagnostics.Add(d);

            if (flow.IsSuccess && flow.Value is not null)
                values.Add(flow.Value);
        }

        var hasErrors = false;
        foreach (var d in diagnostics)
        {
            // Defensive: guard against default(DiagnosticInfo) with null Descriptor
            if (d.Descriptor?.DefaultSeverity == DiagnosticSeverity.Error)
            {
                hasErrors = true;
                break;
            }
        }

        var diagArray = diagnostics.Count > 0 ? diagnostics.ToImmutable().AsEquatableArray() : default;

        if (hasErrors)
            return new DiagnosticFlow<ImmutableArray<TItem>>(default, diagArray);

        return new DiagnosticFlow<ImmutableArray<TItem>>(values.ToImmutable(), diagArray);
    }

    public static DiagnosticFlow<ImmutableArray<T>> Sequence<T>(
        this IEnumerable<DiagnosticFlow<T>> flows) => Collect(flows);
}

/// <summary>
/// Internal helper methods for DiagnosticFlow operations.
/// </summary>
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
/// Extensions for reporting diagnostics from flows.
/// </summary>
public static class DiagnosticFlowReportingExtensions
{
    public static void Report<T>(this DiagnosticFlow<T> flow, SourceProductionContext context)
    {
        foreach (var diagnosticInfo in flow.Diagnostics.AsImmutableArray())
            context.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
    }

    public static T? ReportAndGet<T>(this DiagnosticFlow<T> flow, SourceProductionContext context)
    {
        flow.Report(context);
        return flow.Value;
    }

    public static void ReportAndDo<T>(
        this DiagnosticFlow<T> flow,
        SourceProductionContext context,
        Action<T> onSuccess)
    {
        flow.Report(context);
        if (flow.IsSuccess && flow.Value is not null)
            onSuccess(flow.Value);
    }

    public static void ReportAndAddSource(
        this DiagnosticFlow<FileWithName> flow,
        SourceProductionContext context)
    {
        flow.Report(context);
        if (flow.IsSuccess && flow.Value is { } file)
            context.AddSource(file.Name, file.Text);
    }

    public static void ReportAndAddSources(
        this DiagnosticFlow<ImmutableArray<FileWithName>> flow,
        SourceProductionContext context)
    {
        flow.Report(context);
        if (flow.IsSuccess && flow.Value is { } files)
        {
            foreach (var file in files)
                context.AddSource(file.Name, file.Text);
        }
    }
}

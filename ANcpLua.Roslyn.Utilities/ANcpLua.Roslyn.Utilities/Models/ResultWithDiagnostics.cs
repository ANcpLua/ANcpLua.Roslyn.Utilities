using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Represents a result with associated diagnostics.
/// </summary>
/// <param name="Result">The result value.</param>
/// <param name="Diagnostics">The diagnostics associated with the result.</param>
/// <typeparam name="T">The type of the result.</typeparam>
public readonly record struct ResultWithDiagnostics<T>(
    T Result,
    EquatableArray<Diagnostic> Diagnostics
)
{
    /// <summary>
    ///     Creates a result with no diagnostics.
    /// </summary>
    /// <param name="result">The result value.</param>
    public ResultWithDiagnostics(T result) : this(result, ImmutableArray<Diagnostic>.Empty.AsEquatableArray())
    {
    }
}

/// <summary>
///     Extension methods for creating <see cref="ResultWithDiagnostics{T}" /> instances.
/// </summary>
public static class ResultWithDiagnosticsExtensions
{
    /// <summary>
    ///     Converts a value to a <see cref="ResultWithDiagnostics{T}" /> with no diagnostics.
    /// </summary>
    /// <param name="result"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result)
    {
        return new ResultWithDiagnostics<T>(result);
    }

    /// <summary>
    ///     Converts a value to a <see cref="ResultWithDiagnostics{T}" /> with the specified diagnostics.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to include.</param>
    /// <param name="result"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result,
        ImmutableArray<Diagnostic> diagnostics)
    {
        return new ResultWithDiagnostics<T>(result, diagnostics.AsEquatableArray());
    }
}
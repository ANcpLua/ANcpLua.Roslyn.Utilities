using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Represents a result with associated diagnostics.
/// </summary>
/// <param name="Result">The result value.</param>
/// <param name="Diagnostics">The diagnostics associated with the result.</param>
/// <typeparam name="T">The type of the result.</typeparam>
/// <remarks>
///     Uses <see cref="DiagnosticInfo" /> instead of <c>Diagnostic</c> because
///     <c>Diagnostic</c> contains <c>Location</c> which uses reference equality,
///     breaking incremental generator caching.
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
readonly record struct ResultWithDiagnostics<T>(
    T Result,
    EquatableArray<DiagnosticInfo> Diagnostics
)
{
    /// <summary>
    ///     Creates a result with no diagnostics.
    /// </summary>
    /// <param name="result">The result value.</param>
    public ResultWithDiagnostics(T result) : this(result, ImmutableArray<DiagnosticInfo>.Empty.AsEquatableArray())
    {
    }
}

/// <summary>
///     Extension methods for creating <see cref="ResultWithDiagnostics{T}" /> instances.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class ResultWithDiagnosticsExtensions
{
    /// <summary>
    ///     Converts a value to a <see cref="ResultWithDiagnostics{T}" /> with no diagnostics.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>A result with no diagnostics.</returns>
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result) => new(result);

    /// <summary>
    ///     Converts a value to a <see cref="ResultWithDiagnostics{T}" /> with the specified diagnostics.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <param name="diagnostics">The diagnostics to include.</param>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <returns>A result with the specified diagnostics.</returns>
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result,
        ImmutableArray<DiagnosticInfo> diagnostics) =>
        new(result, diagnostics.AsEquatableArray());
}

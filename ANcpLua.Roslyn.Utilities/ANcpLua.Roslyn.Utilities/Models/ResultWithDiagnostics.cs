using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Represents a result value paired with associated diagnostics from a source generator operation.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
/// <param name="Result">The result value produced by the operation.</param>
/// <param name="Diagnostics">
///     The diagnostics collected during the operation, wrapped in an <see cref="EquatableArray{T}" />
///     to ensure proper equality comparisons for incremental generator caching.
/// </param>
/// <remarks>
///     <para>
///         This type is designed specifically for use in Roslyn incremental source generators where
///         both a result and associated diagnostics need to be propagated through the pipeline.
///     </para>
///     <para>
///         Key design decisions:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Uses <see cref="DiagnosticInfo" /> instead of <c>Diagnostic</c> because <c>Diagnostic</c>
///                 contains <c>Location</c> which uses reference equality, breaking incremental generator caching.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Diagnostics are stored in an <see cref="EquatableArray{T}" /> to ensure value-based equality
///                 comparisons work correctly for the incremental generator's caching mechanism.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Implemented as a <c>readonly record struct</c> for optimal performance and correct
///                 equality semantics.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="DiagnosticInfo" />
/// <seealso cref="EquatableArray{T}" />
/// <seealso cref="ResultWithDiagnosticsExtensions" />
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
    ///     Initializes a new instance of the <see cref="ResultWithDiagnostics{T}" /> struct with no diagnostics.
    /// </summary>
    /// <param name="result">The result value to wrap.</param>
    /// <remarks>
    ///     This constructor is a convenience overload that creates a result with an empty diagnostics collection.
    ///     Use this when the operation completed successfully without any warnings or errors to report.
    /// </remarks>
    /// <seealso cref="ResultWithDiagnosticsExtensions.ToResultWithDiagnostics{T}(T)" />
    public ResultWithDiagnostics(T result) : this(result, ImmutableArray<DiagnosticInfo>.Empty.AsEquatableArray())
    {
    }
}

/// <summary>
///     Provides extension methods for creating <see cref="ResultWithDiagnostics{T}" /> instances
///     from existing values.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods provide a fluent API for wrapping values in
///         <see cref="ResultWithDiagnostics{T}" /> containers, making it easier to integrate
///         with source generator pipelines.
///     </para>
///     <para>
///         Example usage:
///     </para>
///     <code>
///         // Wrap a value with no diagnostics
///         var result = myModel.ToResultWithDiagnostics();
///
///         // Wrap a value with diagnostics
///         var resultWithWarnings = myModel.ToResultWithDiagnostics(diagnostics);
///     </code>
/// </remarks>
/// <seealso cref="ResultWithDiagnostics{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class ResultWithDiagnosticsExtensions
{
    /// <summary>
    ///     Wraps the specified value in a <see cref="ResultWithDiagnostics{T}" /> with no associated diagnostics.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result value to wrap.</param>
    /// <returns>
    ///     A new <see cref="ResultWithDiagnostics{T}" /> containing the specified <paramref name="result" />
    ///     with an empty diagnostics collection.
    /// </returns>
    /// <remarks>
    ///     Use this method when an operation completes successfully without any diagnostics to report.
    /// </remarks>
    /// <seealso cref="ToResultWithDiagnostics{T}(T, ImmutableArray{DiagnosticInfo})" />
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result) => new(result);

    /// <summary>
    ///     Wraps the specified value in a <see cref="ResultWithDiagnostics{T}" /> with the provided diagnostics.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result value to wrap.</param>
    /// <param name="diagnostics">
    ///     The diagnostics to associate with the result. These are automatically converted
    ///     to an <see cref="EquatableArray{T}" /> for proper caching behavior.
    /// </param>
    /// <returns>
    ///     A new <see cref="ResultWithDiagnostics{T}" /> containing the specified <paramref name="result" />
    ///     and <paramref name="diagnostics" />.
    /// </returns>
    /// <remarks>
    ///     Use this method when an operation produces both a result and one or more diagnostics
    ///     (such as warnings or errors) that should be reported to the user.
    /// </remarks>
    /// <seealso cref="ToResultWithDiagnostics{T}(T)" />
    /// <seealso cref="DiagnosticInfo" />
    public static ResultWithDiagnostics<T> ToResultWithDiagnostics<T>(this T result,
        ImmutableArray<DiagnosticInfo> diagnostics) =>
        new(result, diagnostics.AsEquatableArray());
}

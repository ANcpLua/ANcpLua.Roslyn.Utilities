using ANcpLua.Roslyn.Utilities.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="SourceProductionContext" /> that simplify common source generation tasks.
/// </summary>
/// <remarks>
///     <para>
///         This class provides utilities for:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Adding generated source with standardized headers</description>
///         </item>
///         <item>
///             <description>Reporting diagnostics from <see cref="DiagnosticInfo" /> instances</description>
///         </item>
///         <item>
///             <description>Converting exceptions to diagnostics for error reporting</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SourceProductionContext" />
/// <seealso cref="DiagnosticInfo" />
/// <seealso cref="ResultWithDiagnostics{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SourceProductionContextExtensions
{
    /// <summary>
    ///     Adds source code with a standardized auto-generated header to the compilation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The generated header includes:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>An <c>&lt;auto-generated/&gt;</c> comment to suppress IDE warnings</description>
    ///         </item>
    ///         <item>
    ///             <description>A comment indicating the generator name</description>
    ///         </item>
    ///         <item>
    ///             <description>A <c>#nullable enable</c> directive</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="context">The source production context to add the source to.</param>
    /// <param name="hintName">
    ///     A unique hint name for the source file. This is used to identify the generated file
    ///     and should typically end with <c>.g.cs</c>.
    /// </param>
    /// <param name="sourceCode">The source code content to add (without the header).</param>
    /// <param name="generatorName">
    ///     Optional name of the generator to include in the header comment.
    ///     Defaults to <c>"ANcpLua.SourceGen"</c> if not specified.
    /// </param>
    /// <seealso cref="AddSourceWithHeader(SourceProductionContext, string, StringBuilder, string?)" />
    public static void AddSourceWithHeader(
        this SourceProductionContext context,
        string hintName,
        string sourceCode,
        string? generatorName = null)
    {
        var header = GenerateHeader(generatorName ?? "ANcpLua.SourceGen");
        var fullSource = header + sourceCode;
        context.AddSource(hintName, SourceText.From(fullSource, Encoding.UTF8));
    }

    /// <summary>
    ///     Adds source code from a <see cref="StringBuilder" /> with a standardized auto-generated header.
    /// </summary>
    /// <remarks>
    ///     This overload is useful when source code is built incrementally using a <see cref="StringBuilder" />.
    ///     The header format is identical to
    ///     <see cref="AddSourceWithHeader(SourceProductionContext, string, string, string?)" />.
    /// </remarks>
    /// <param name="context">The source production context to add the source to.</param>
    /// <param name="hintName">
    ///     A unique hint name for the source file. This is used to identify the generated file
    ///     and should typically end with <c>.g.cs</c>.
    /// </param>
    /// <param name="sourceBuilder">The <see cref="StringBuilder" /> containing the source code content.</param>
    /// <param name="generatorName">
    ///     Optional name of the generator to include in the header comment.
    ///     Defaults to <c>"ANcpLua.SourceGen"</c> if not specified.
    /// </param>
    /// <seealso cref="AddSourceWithHeader(SourceProductionContext, string, string, string?)" />
    public static void AddSourceWithHeader(
        this SourceProductionContext context,
        string hintName,
        StringBuilder sourceBuilder,
        string? generatorName = null)
    {
        context.AddSourceWithHeader(hintName, sourceBuilder.ToString(), generatorName);
    }

    /// <summary>
    ///     Reports a diagnostic from a <see cref="DiagnosticInfo" /> instance.
    /// </summary>
    /// <remarks>
    ///     <see cref="DiagnosticInfo" /> is an equatable representation of a diagnostic that can be safely
    ///     cached in incremental generator pipelines. This method converts it back to a <see cref="Diagnostic" />
    ///     and reports it through the context.
    /// </remarks>
    /// <param name="context">The source production context to report the diagnostic to.</param>
    /// <param name="diagnosticInfo">The diagnostic information to report.</param>
    /// <seealso cref="DiagnosticInfo" />
    /// <seealso cref="ReportDiagnostics{T}(SourceProductionContext, ResultWithDiagnostics{T})" />
    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticInfo diagnosticInfo)
    {
        context.ReportDiagnostic(diagnosticInfo.ToDiagnostic());
    }

    /// <summary>
    ///     Reports all diagnostics from a collection of <see cref="DiagnosticInfo" /> instances.
    /// </summary>
    /// <param name="context">The source production context to report diagnostics to.</param>
    /// <param name="diagnostics">The collection of diagnostics to report.</param>
    /// <seealso cref="DiagnosticInfo" />
    /// <seealso cref="EquatableArray{T}" />
    public static void ReportDiagnostics(this SourceProductionContext context,
        EquatableArray<DiagnosticInfo> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
    }

    /// <summary>
    ///     Reports all diagnostics from a <see cref="ResultWithDiagnostics{T}" /> instance.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="context">The source production context to report diagnostics to.</param>
    /// <param name="result">The result containing diagnostics to report.</param>
    /// <seealso cref="ResultWithDiagnostics{T}" />
    public static void ReportDiagnostics<T>(this SourceProductionContext context,
        ResultWithDiagnostics<T> result)
    {
        context.ReportDiagnostics(result.Diagnostics);
    }

    /// <summary>
    ///     Reports an exception as an error diagnostic.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is useful for catching and reporting exceptions that occur during source generation
    ///         without crashing the generator. The exception details are included in the diagnostic message.
    ///     </para>
    ///     <para>
    ///         The resulting diagnostic has <see cref="DiagnosticSeverity.Error" /> severity and
    ///         <see cref="Location.None" /> as its location.
    ///     </para>
    /// </remarks>
    /// <param name="context">The source production context to report the diagnostic to.</param>
    /// <param name="id">The diagnostic ID (e.g., <c>"GEN001"</c>).</param>
    /// <param name="exception">The exception to report.</param>
    /// <param name="prefix">
    ///     Optional prefix to prepend to the diagnostic ID.
    ///     For example, if <paramref name="prefix" /> is <c>"ANCP"</c> and <paramref name="id" /> is <c>"001"</c>,
    ///     the resulting diagnostic ID will be <c>"ANCP001"</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="id" /> or <paramref name="exception" /> is <c>null</c>.
    /// </exception>
    /// <seealso cref="ToDiagnostic(Exception, string, string?)" />
    public static void ReportException(
        this SourceProductionContext context,
        string id,
        Exception exception,
        string? prefix = null)
    {
        id = id ?? throw new ArgumentNullException(nameof(id));
        exception = exception ?? throw new ArgumentNullException(nameof(exception));

        context.ReportDiagnostic(exception.ToDiagnostic(id, prefix));
    }

    /// <summary>
    ///     Creates a <see cref="Diagnostic" /> from an exception.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The resulting diagnostic has the following properties:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Severity: <see cref="DiagnosticSeverity.Error" /></description>
    ///         </item>
    ///         <item>
    ///             <description>Category: <c>"Usage"</c></description>
    ///         </item>
    ///         <item>
    ///             <description>Location: <see cref="Location.None" /></description>
    ///         </item>
    ///         <item>
    ///             <description>Enabled by default: <c>true</c></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <param name="exception">The exception to convert to a diagnostic.</param>
    /// <param name="id">The diagnostic ID (e.g., <c>"GEN001"</c>).</param>
    /// <param name="prefix">
    ///     Optional prefix to prepend to the diagnostic ID.
    ///     For example, if <paramref name="prefix" /> is <c>"ANCP"</c> and <paramref name="id" /> is <c>"001"</c>,
    ///     the resulting diagnostic ID will be <c>"ANCP001"</c>.
    /// </param>
    /// <returns>
    ///     A <see cref="Diagnostic" /> representing the exception with the specified ID.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="exception" /> or <paramref name="id" /> is <c>null</c>.
    /// </exception>
    /// <seealso cref="ReportException(SourceProductionContext, string, Exception, string?)" />
    public static Diagnostic ToDiagnostic(
        this Exception exception,
        string id,
        string? prefix = null)
    {
        exception = exception ?? throw new ArgumentNullException(nameof(exception));
        id = id ?? throw new ArgumentNullException(nameof(id));

        if (prefix is not null) id = $"{prefix}{id}";

        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                "Exception: ",
                $"{exception}",
                "Usage",
                DiagnosticSeverity.Error,
                true),
            Location.None);
    }

    private static string GenerateHeader(string generatorName) =>
        $"""
         // <auto-generated/>
         // Generated by {generatorName}
         #nullable enable

         """;
}

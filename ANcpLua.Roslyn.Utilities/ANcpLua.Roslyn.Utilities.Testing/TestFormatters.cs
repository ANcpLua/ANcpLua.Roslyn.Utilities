using AwesomeAssertions.Execution;
using AwesomeAssertions.Formatting;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides custom formatters for test assertion output.
/// </summary>
/// <remarks>
///     <para>
///         This class registers specialized formatters with AwesomeAssertions (FluentAssertions fork)
///         to produce clear, readable output for Roslyn generator testing types.
///     </para>
///     <para>
///         The formatters handle:
///         <list type="bullet">
///             <item>
///                 <description><see cref="GeneratorCachingReport" /> - Shows generator name and validation status</description>
///             </item>
///             <item>
///                 <description><see cref="GeneratorStepAnalysis" /> - Shows step name, cache status, and performance</description>
///             </item>
///             <item>
///                 <description><see cref="ForbiddenTypeViolation" /> - Shows step name, forbidden type, and path</description>
///             </item>
///             <item>
///                 <description><see cref="Diagnostic" /> - Shows formatted diagnostic with location</description>
///             </item>
///             <item>
///                 <description><see cref="GeneratedSourceResult" /> - Shows hint name and content length</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The <see cref="Initialize" /> method is called automatically when any test extension is used,
///         but can also be called explicitly to ensure formatters are registered before running tests.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Formatters are initialized automatically, but can be called explicitly:
/// TestFormatters.Initialize();
/// 
/// // Apply formatter settings to an assertion scope:
/// using var scope = new AssertionScope("My Generator");
/// TestFormatters.ApplyToScope(scope);
/// </code>
/// </example>
public static class TestFormatters
{
    private static int _initialized;

    /// <summary>
    ///     Initializes and registers all custom formatters with AwesomeAssertions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is idempotent - calling it multiple times has no additional effect.
    ///         It uses <see cref="Interlocked.CompareExchange(ref int,int,int)" /> to ensure
    ///         thread-safe, exactly-once initialization.
    ///     </para>
    ///     <para>
    ///         Registered formatters:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <see cref="CachingReportFormatter" /> - Formats <see cref="GeneratorCachingReport" />
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="StepAnalysisFormatter" /> - Formats <see cref="GeneratorStepAnalysis" />
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="ForbiddenTypeViolationFormatter" /> - Formats
    ///                     <see cref="ForbiddenTypeViolation" />
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="DiagnosticFormatter" /> - Formats <see cref="Diagnostic" /></description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="GeneratedSourceResultFormatter" /> - Formats
    ///                     <see cref="GeneratedSourceResult" />
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) is not 0) return;

        IValueFormatter[] formatters =
        [
            new CachingReportFormatter(),
            new StepAnalysisFormatter(),
            new ForbiddenTypeViolationFormatter(),
            new DiagnosticFormatter(),
            new GeneratedSourceResultFormatter()
        ];
        foreach (var formatter in formatters) Formatter.AddFormatter(formatter);
    }

    /// <summary>
    ///     Applies recommended formatting options to an assertion scope.
    /// </summary>
    /// <param name="scope">The assertion scope to configure.</param>
    /// <remarks>
    ///     <para>
    ///         This method configures the assertion scope for optimal readability when
    ///         dealing with large generator outputs. Settings applied:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>UseLineBreaks = true - Multi-line output for complex objects</description>
    ///             </item>
    ///             <item>
    ///                 <description>MaxLines = 8000 - Allows large generated files to be displayed</description>
    ///             </item>
    ///             <item>
    ///                 <description>MaxDepth = 12 - Allows deep object graph traversal</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// using var scope = new AssertionScope("Generator Caching Validation");
    /// TestFormatters.ApplyToScope(scope);
    /// 
    /// // Assertions within this scope will have the formatting options applied
    /// report.Should().BeValidAndCached();
    /// </code>
    /// </example>
    public static void ApplyToScope(AssertionScope scope)
    {
        scope.FormattingOptions.UseLineBreaks = true;
        scope.FormattingOptions.MaxLines = 8000;
        scope.FormattingOptions.MaxDepth = 12;
    }

    /// <summary>
    ///     Formatter for <see cref="GeneratorCachingReport" /> instances.
    /// </summary>
    /// <remarks>
    ///     Produces output like: <c>CachingReport[MyGenerator]: [OK] VALID</c>
    ///     or <c>CachingReport[MyGenerator]: [X] FAILED</c>
    /// </remarks>
    private sealed class CachingReportFormatter : IValueFormatter
    {
        /// <inheritdoc />
        public bool CanHandle(object value)
        {
            return value is GeneratorCachingReport;
        }

        /// <inheritdoc />
        public void Format(object value, FormattedObjectGraph graph, FormattingContext context, FormatChild child)
        {
            var report = (GeneratorCachingReport)value;
            graph.AddFragment(
                $"CachingReport[{report.GeneratorName}]: {(report.IsCorrect ? "[OK] VALID" : "[X] FAILED")}");
        }
    }

    /// <summary>
    ///     Formatter for <see cref="GeneratorStepAnalysis" /> instances.
    /// </summary>
    /// <remarks>
    ///     Produces output like: <c>[OK] TransformStep: C:5 U:0 | M:0 N:0 R:0 (Total:5) | Perf: 1.23ms -> 0.45ms</c>
    /// </remarks>
    private sealed class StepAnalysisFormatter : IValueFormatter
    {
        /// <inheritdoc />
        public bool CanHandle(object value)
        {
            return value is GeneratorStepAnalysis;
        }

        /// <inheritdoc />
        public void Format(object value, FormattedObjectGraph graph, FormattingContext context, FormatChild child)
        {
            var step = (GeneratorStepAnalysis)value;
            var status = step.IsCachedSuccessfully ? "[OK]" : "[X]";
            graph.AddFragment($"{status} {step.StepName}: {step.FormatBreakdown()} | Perf: {step.FormatPerformance()}");
        }
    }

    /// <summary>
    ///     Formatter for <see cref="ForbiddenTypeViolation" /> instances.
    /// </summary>
    /// <remarks>
    ///     Produces output like: <c>[!] TransformStep: ISymbol at Output.Value</c>
    /// </remarks>
    private sealed class ForbiddenTypeViolationFormatter : IValueFormatter
    {
        /// <inheritdoc />
        public bool CanHandle(object value)
        {
            return value is ForbiddenTypeViolation;
        }

        /// <inheritdoc />
        public void Format(object value, FormattedObjectGraph graph, FormattingContext context, FormatChild child)
        {
            var violation = (ForbiddenTypeViolation)value;
            graph.AddFragment($"[!] {violation.StepName}: {violation.ForbiddenType.Name} at {violation.Path}");
        }
    }

    /// <summary>
    ///     Formatter for <see cref="Diagnostic" /> instances.
    /// </summary>
    /// <remarks>
    ///     Uses <see cref="DiagnosticSnapshot" /> to produce consistent, readable output
    ///     like: <c>MyFile.cs@10:5 CS0001 (Error): Missing semicolon</c>
    /// </remarks>
    private sealed class DiagnosticFormatter : IValueFormatter
    {
        /// <inheritdoc />
        public bool CanHandle(object value)
        {
            return value is Diagnostic;
        }

        /// <inheritdoc />
        public void Format(object value, FormattedObjectGraph graph, FormattingContext context, FormatChild child)
        {
            graph.AddFragment(DiagnosticSnapshot.FromDiagnostic((Diagnostic)value).Format());
        }
    }

    /// <summary>
    ///     Formatter for <see cref="GeneratedSourceResult" /> instances.
    /// </summary>
    /// <remarks>
    ///     Produces output like: <c>Generated[MyType.g.cs] (1234 chars)</c>
    /// </remarks>
    private sealed class GeneratedSourceResultFormatter : IValueFormatter
    {
        /// <inheritdoc />
        public bool CanHandle(object value)
        {
            return value is GeneratedSourceResult;
        }

        /// <inheritdoc />
        public void Format(object value, FormattedObjectGraph graph, FormattingContext context, FormatChild child)
        {
            var result = (GeneratedSourceResult)value;
            graph.AddFragment($"Generated[{result.HintName}] ({result.SourceText.Length} chars)");
        }
    }
}
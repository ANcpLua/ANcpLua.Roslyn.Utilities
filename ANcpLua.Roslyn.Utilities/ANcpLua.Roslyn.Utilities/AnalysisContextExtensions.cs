using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods that simplify diagnostic reporting by combining
///     <see cref="Diagnostic.Create" /> and <c>ReportDiagnostic</c> into a single call.
/// </summary>
/// <remarks>
///     <para>
///         These extensions eliminate the boilerplate pattern of:
///     </para>
///     <code>
/// context.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
/// </code>
///     <para>
///         And replace it with the more concise:
///     </para>
///     <code>
/// context.ReportDiagnostic(descriptor, location, args);
/// </code>
///     <para>
///         All methods are marked with <see cref="System.Diagnostics.StackTraceHiddenAttribute" />
///         to keep stack traces clean when debugging analyzer exceptions.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="SyntaxNodeAnalysisContext" />: For syntax-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="OperationAnalysisContext" />: For operation-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="SymbolAnalysisContext" />: For symbol-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="SyntaxTreeAnalysisContext" />: For syntax tree-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="SemanticModelAnalysisContext" />: For semantic model-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="CodeBlockAnalysisContext" />: For code block-based analysis
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="OperationBlockAnalysisContext" />: For operation block-based analysis
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Before: Verbose pattern
/// context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), typeName));
///
/// // After: Simplified extension
/// context.ReportDiagnostic(Rule, node.GetLocation(), typeName);
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AnalysisContextExtensions
{
    // ========== SyntaxNodeAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, and message arguments.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, properties, and message
    ///     arguments.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="properties">Custom properties to attach to the diagnostic for code fix use.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, properties, messageArgs));

    // ========== OperationAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, and message arguments.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, properties, and message
    ///     arguments.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="properties">Custom properties to attach to the diagnostic for code fix use.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, properties, messageArgs));

    // ========== SymbolAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, and message arguments.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, additional locations, properties, and message
    ///     arguments.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The primary location where the diagnostic should be reported.</param>
    /// <param name="additionalLocations">Additional locations to associate with the diagnostic.</param>
    /// <param name="properties">Custom properties to attach to the diagnostic for code fix use.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, additionalLocations, properties, messageArgs));

    // ========== SyntaxTreeAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The syntax tree analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SyntaxTreeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The syntax tree analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this SyntaxTreeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    // ========== SemanticModelAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The semantic model analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this SemanticModelAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The semantic model analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this SemanticModelAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    // ========== CodeBlockAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The code block analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this CodeBlockAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The code block analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this CodeBlockAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    // ========== OperationBlockAnalysisContext ==========

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor, location, and message arguments.
    /// </summary>
    /// <param name="context">The operation block analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="messageArgs">Arguments to format into the diagnostic message.</param>
        public static void ReportDiagnostic(
        this OperationBlockAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    /// <summary>
    ///     Reports a diagnostic with the specified descriptor and location.
    /// </summary>
    /// <param name="context">The operation block analysis context.</param>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
        public static void ReportDiagnostic(
        this OperationBlockAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
}

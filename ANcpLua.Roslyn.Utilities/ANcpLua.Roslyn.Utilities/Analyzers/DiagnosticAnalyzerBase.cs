namespace ANcpLua.Roslyn.Utilities.Analyzers;

using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Base class for diagnostic analyzers that standardizes initialization.
/// Automatically configures:
/// - GeneratedCodeAnalysisFlags.None (don't analyze generated code)
/// - EnableConcurrentExecution() for performance
/// </summary>
public abstract class DiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        InitializeCore(context);
    }

    /// <summary>
    /// Register analysis actions. Called after standard configuration.
    /// </summary>
    protected abstract void InitializeCore(AnalysisContext context);
}

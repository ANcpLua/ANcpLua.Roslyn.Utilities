// Copyright (c) Microsoft. All rights reserved.

using Microsoft.CodeAnalysis;

namespace Microsoft.Agents.AI.Workflows.Generators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the executor route source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Usage";

    /// <summary>
    /// AL0101: Handler method must have IWorkflowContext parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingWorkflowContext = new(
        id: "AL0101",
        title: "Handler missing IWorkflowContext parameter",
        messageFormat: "Method '{0}' marked with [MessageHandler] must have IWorkflowContext as the second parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0102: Handler method has invalid return type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidReturnType = new(
        id: "AL0102",
        title: "Handler has invalid return type",
        messageFormat: "Method '{0}' marked with [MessageHandler] must return void, ValueTask, or ValueTask<T>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0103: Executor with [MessageHandler] must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "AL0103",
        title: "Executor with [MessageHandler] must be partial",
        messageFormat: "Class '{0}' contains [MessageHandler] methods but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0104: [MessageHandler] on non-Executor class.
    /// </summary>
    public static readonly DiagnosticDescriptor NotAnExecutor = new(
        id: "AL0104",
        title: "[MessageHandler] on non-Executor class",
        messageFormat: "Method '{0}' is marked with [MessageHandler] but class '{1}' does not derive from Executor",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0105: Handler method has insufficient parameters.
    /// </summary>
    public static readonly DiagnosticDescriptor InsufficientParameters = new(
        id: "AL0105",
        title: "Handler has insufficient parameters",
        messageFormat: "Method '{0}' marked with [MessageHandler] must have at least 2 parameters (message and IWorkflowContext)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0106: ConfigureRoutes already defined.
    /// </summary>
    public static readonly DiagnosticDescriptor ConfigureRoutesAlreadyDefined = new(
        id: "AL0106",
        title: "ConfigureRoutes already defined",
        messageFormat: "Class '{0}' already defines ConfigureRoutes; [MessageHandler] methods will be ignored",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// AL0107: Handler method is static.
    /// </summary>
    public static readonly DiagnosticDescriptor HandlerCannotBeStatic = new(
        id: "AL0107",
        title: "Handler cannot be static",
        messageFormat: "Method '{0}' marked with [MessageHandler] cannot be static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

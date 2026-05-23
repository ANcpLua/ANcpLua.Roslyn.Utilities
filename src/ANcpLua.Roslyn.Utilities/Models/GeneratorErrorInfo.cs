// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     A value-equatable snapshot of an <see cref="Exception" /> safe to flow through the
///     incremental generator cache.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Exception" /> instances do not implement value equality, so storing one in
///         pipeline state forces every cache comparison to reference-equality and breaks
///         incremental caching across compilations. <see cref="GeneratorErrorInfo" /> captures the
///         minimum stable information needed to surface the failure as a diagnostic: type name and
///         message. Stack traces are intentionally excluded because they vary by call site and
///         machine path, which weakens incremental cache stability.
///     </para>
/// </remarks>
/// <param name="TypeName">The exception's runtime type name (e.g. <c>System.InvalidOperationException</c>).</param>
/// <param name="Message">The exception's message, or empty if absent.</param>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct GeneratorErrorInfo(string TypeName, string Message)
{
    /// <summary>
    ///     Captures an <see cref="Exception" /> as a value-equatable <see cref="GeneratorErrorInfo" />.
    /// </summary>
    public static GeneratorErrorInfo From(Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        return new GeneratorErrorInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message ?? string.Empty);
    }

    /// <summary>
    ///     Creates a cache-stable diagnostic representation for this captured generator failure.
    /// </summary>
    /// <param name="id">The diagnostic ID (for example, <c>"GEN001"</c>).</param>
    /// <param name="prefix">Optional prefix prepended to <paramref name="id" />.</param>
    public DiagnosticInfo ToDiagnosticInfo(string id, string? prefix = null)
    {
        id = id ?? throw new ArgumentNullException(nameof(id));
        if (prefix is not null) id = $"{prefix}{id}";

        return DiagnosticInfo.Create(
            new DiagnosticDescriptor(
                id,
                "Exception: ",
                ToString(),
                "Usage",
                DiagnosticSeverity.Error,
                true),
            Location.None);
    }

    /// <summary>
    ///     Renders the captured error in stable <c>Type: Message</c> form.
    /// </summary>
    public override string ToString()
    {
        return $"{TypeName}: {Message}";
    }
}

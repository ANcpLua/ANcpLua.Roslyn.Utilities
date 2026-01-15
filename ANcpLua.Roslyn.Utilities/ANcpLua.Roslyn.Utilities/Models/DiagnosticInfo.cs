// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable representation of a diagnostic for use in incremental source generators.
/// </summary>
/// <remarks>
///     <para>
///         This type provides a value-equal wrapper around diagnostic information that can be safely
///         cached and compared in incremental generator pipelines. Unlike <see cref="Diagnostic" />,
///         which contains non-equatable reference types, <see cref="DiagnosticInfo" /> uses
///         <see cref="LocationInfo" /> and <see cref="EquatableMessageArgs" /> to ensure proper
///         equality semantics.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Use factory methods to create instances from syntax nodes, tokens, symbols, or locations.</description>
///         </item>
///         <item>
///             <description>Call <see cref="ToDiagnostic" /> to convert back to a <see cref="Diagnostic" /> for reporting.</description>
///         </item>
///         <item>
///             <description>Safe for use in generator caching scenarios due to value equality.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="Descriptor">
///     The diagnostic descriptor that defines the diagnostic's ID, title, message format, and
///     severity.
/// </param>
/// <param name="Location">The equatable location information where the diagnostic should be reported.</param>
/// <param name="MessageArgs">The equatable message arguments to format into the diagnostic message.</param>
/// <seealso cref="LocationInfo" />
/// <seealso cref="EquatableMessageArgs" />
/// <seealso cref="Diagnostic" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        LocationInfo Location,
        EquatableMessageArgs MessageArgs)
{
    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument from a syntax token.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="token">The syntax token whose location will be used for the diagnostic.</param>
    /// <param name="arg0">The single argument to format into the diagnostic message.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, SyntaxNode, object?)" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxToken token, object? arg0)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(token), new EquatableMessageArgs([arg0]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument from a syntax node.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="node">The syntax node whose location will be used for the diagnostic.</param>
    /// <param name="arg0">The single argument to format into the diagnostic message.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, SyntaxToken, object?)" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, object? arg0)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(node), new EquatableMessageArgs([arg0]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with multiple arguments from a syntax node.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="node">The syntax node whose location will be used for the diagnostic.</param>
    /// <param name="args">The arguments to format into the diagnostic message.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, Location, object?[])" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, params object?[] args)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(node), new EquatableMessageArgs([..args]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with no arguments from a location.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance with empty message arguments.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, Location, object?[])" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(location), EquatableMessageArgs.Empty);
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with multiple arguments from a location.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="location">The location where the diagnostic should be reported.</param>
    /// <param name="args">The arguments to format into the diagnostic message.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, SyntaxNode, object?[])" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params object?[] args)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(location), new EquatableMessageArgs([..args]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> from a symbol with multiple arguments.
    /// </summary>
    /// <remarks>
    ///     The location is taken from the first declared location of the symbol. If the symbol
    ///     has no locations, <see cref="Microsoft.CodeAnalysis.Location.None" /> is used.
    /// </remarks>
    /// <param name="descriptor">The diagnostic descriptor defining the diagnostic metadata.</param>
    /// <param name="symbol">The symbol whose first location will be used for the diagnostic.</param>
    /// <param name="args">The arguments to format into the diagnostic message.</param>
    /// <returns>A new <see cref="DiagnosticInfo" /> instance.</returns>
    /// <seealso cref="Create(DiagnosticDescriptor, Location, object?[])" />
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ISymbol symbol, params object?[] args)
    {
        var location = symbol.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;
        return new DiagnosticInfo(descriptor, LocationInfo.From(location), new EquatableMessageArgs([..args]));
    }

    /// <summary>
    ///     Converts this <see cref="DiagnosticInfo" /> to a <see cref="Diagnostic" /> for reporting.
    /// </summary>
    /// <remarks>
    ///     This method reconstructs the <see cref="Location" /> from the stored <see cref="LocationInfo" />
    ///     and applies the message arguments to the descriptor's message format.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Diagnostic" /> instance that can be reported via
    ///     <see cref="SourceProductionContext.ReportDiagnostic(Diagnostic)" /> or similar methods.
    /// </returns>
    public Diagnostic ToDiagnostic()
    {
        return MessageArgs.IsEmpty
            ? Diagnostic.Create(Descriptor, Location.ToLocation())
            : Diagnostic.Create(Descriptor, Location.ToLocation(), [.. MessageArgs.Args]);
    }
}
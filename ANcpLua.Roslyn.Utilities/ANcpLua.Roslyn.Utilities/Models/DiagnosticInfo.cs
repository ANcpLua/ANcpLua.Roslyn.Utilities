// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable representation of a diagnostic for use in source generators.
/// </summary>
public readonly record struct DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo Location,
    EquatableMessageArgs MessageArgs)
{
    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxToken token, object? arg0)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(token), new EquatableMessageArgs([arg0]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument from a node.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, object? arg0)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(node), new EquatableMessageArgs([arg0]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with multiple arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, params object?[] args)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(node), new EquatableMessageArgs([.. args]));
    }

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with no arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.From(location), EquatableMessageArgs.Empty);
    }

    /// <summary>
    ///     Creates the <see cref="Diagnostic" /> from this info.
    /// </summary>
    public Diagnostic ToDiagnostic()
    {
        return MessageArgs.Args.IsDefaultOrEmpty
            ? Diagnostic.Create(Descriptor, Location.ToLocation())
            : Diagnostic.Create(Descriptor, Location.ToLocation(), [.. MessageArgs.Args]);
    }
}
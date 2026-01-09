// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable representation of a diagnostic for use in source generators.
/// </summary>
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
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxToken token, object? arg0) =>
        new(descriptor, LocationInfo.From(token), new EquatableMessageArgs(ImmutableArray.Create(arg0)));

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with a single argument from a node.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, object? arg0) =>
        new(descriptor, LocationInfo.From(node), new EquatableMessageArgs(ImmutableArray.Create(arg0)));

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with multiple arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, params object?[] args) =>
        new(descriptor, LocationInfo.From(node), new EquatableMessageArgs(ImmutableArray.Create(args)));

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with no arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location) =>
        new(descriptor, LocationInfo.From(location), EquatableMessageArgs.Empty);

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> with multiple arguments from a location.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params object?[] args) =>
        new(descriptor, LocationInfo.From(location), new EquatableMessageArgs(ImmutableArray.Create(args)));

    /// <summary>
    ///     Creates a <see cref="DiagnosticInfo" /> from a symbol with multiple arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ISymbol symbol, params object?[] args)
    {
        var location = symbol.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;
        return new(descriptor, LocationInfo.From(location), new EquatableMessageArgs(ImmutableArray.Create(args)));
    }

    /// <summary>
    ///     Creates the <see cref="Diagnostic" /> from this info.
    /// </summary>
    public Diagnostic ToDiagnostic() =>
        MessageArgs.IsEmpty
            ? Diagnostic.Create(Descriptor, Location.ToLocation())
            : Diagnostic.Create(Descriptor, Location.ToLocation(), [.. MessageArgs.Args]);
}
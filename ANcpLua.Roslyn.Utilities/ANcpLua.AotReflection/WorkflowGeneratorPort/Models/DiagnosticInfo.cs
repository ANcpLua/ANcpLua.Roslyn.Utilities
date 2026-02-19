// Copyright (c) Microsoft. All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Agents.AI.Workflows.Generators.Models;

/// <summary>
/// Represents diagnostic information in a form that supports value equality.
/// Stores the <see cref="DiagnosticDescriptor"/> directly, eliminating runtime lookup.
/// Location is stored as file path + span, which can be used to recreate a Location.
/// </summary>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    string FilePath,
    TextSpan Span,
    LinePositionSpan LineSpan,
    EquatableArray<string> MessageArgs)
{
    /// <summary>
    /// Creates a DiagnosticInfo from a descriptor, location, and message arguments.
    /// </summary>
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params string[] messageArgs)
    {
        FileLinePositionSpan lineSpan = location.GetLineSpan();
        return new DiagnosticInfo(
            descriptor,
            lineSpan.Path ?? string.Empty,
            location.SourceSpan,
            lineSpan.Span,
            messageArgs.ToEquatableArray());
    }

    /// <summary>
    /// Converts this info back to a Roslyn Diagnostic.
    /// </summary>
    public Diagnostic ToDiagnostic()
    {
        Location location = string.IsNullOrWhiteSpace(this.FilePath)
            ? Location.None
            : Location.Create(this.FilePath, this.Span, this.LineSpan);

        if (this.MessageArgs.IsEmpty)
        {
            return Diagnostic.Create(this.Descriptor, location);
        }

        object[] args = new object[this.MessageArgs.Length];
        for (int i = 0; i < this.MessageArgs.Length; i++)
        {
            args[i] = this.MessageArgs[i];
        }

        return Diagnostic.Create(this.Descriptor, location, args);
    }
}

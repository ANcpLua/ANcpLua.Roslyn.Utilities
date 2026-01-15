using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable representation of a source location for use in source generators.
/// </summary>
/// <remarks>
///     <para>
///         This type provides a value-equal wrapper around location information that can be safely
///         cached in incremental source generator pipelines. Unlike <see cref="Location" />, which
///         is tied to a specific compilation, <see cref="LocationInfo" /> stores only the essential
///         location data (file path, span, and line span) as primitive values.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>File path from the mapped line span or syntax tree</description>
///         </item>
///         <item>
///             <description>Source span for precise character positioning</description>
///         </item>
///         <item>
///             <description>Line span for human-readable line/column information</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="Path">The file path of the source location.</param>
/// <param name="Span">The text span representing the character range in the source file.</param>
/// <param name="LineSpan">The line and column position span for display purposes.</param>
/// <seealso cref="Location" />
/// <seealso cref="DiagnosticInfo" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct LocationInfo(string Path, TextSpan Span, LinePositionSpan LineSpan)
{
    /// <summary>
    ///     Creates a <see cref="LocationInfo" /> from a <see cref="Location" />.
    /// </summary>
    /// <param name="location">The location to convert.</param>
    /// <returns>
    ///     A new <see cref="LocationInfo" /> containing the file path, text span, and line span
    ///     extracted from the specified <paramref name="location" />.
    /// </returns>
    /// <remarks>
    ///     The file path is determined from the mapped line span if available, falling back to
    ///     the syntax tree's file path if the mapped path is empty.
    /// </remarks>
    public static LocationInfo From(Location location)
    {
        var mapped = location.GetMappedLineSpan();
        var path = string.IsNullOrEmpty(mapped.Path)
            ? location.SourceTree?.FilePath ?? string.Empty
            : mapped.Path;
        return new LocationInfo(path, location.SourceSpan, mapped.Span);
    }

    /// <summary>
    ///     Creates a <see cref="LocationInfo" /> from a <see cref="SyntaxNode" />.
    /// </summary>
    /// <param name="node">The syntax node whose location to capture.</param>
    /// <returns>
    ///     A new <see cref="LocationInfo" /> representing the location of the specified
    ///     <paramref name="node" />.
    /// </returns>
    /// <seealso cref="From(Location)" />
    public static LocationInfo From(SyntaxNode node)
    {
        return From(node.GetLocation());
    }

    /// <summary>
    ///     Creates a <see cref="LocationInfo" /> from a <see cref="SyntaxToken" />.
    /// </summary>
    /// <param name="token">The syntax token whose location to capture.</param>
    /// <returns>
    ///     A new <see cref="LocationInfo" /> representing the location of the specified
    ///     <paramref name="token" />.
    /// </returns>
    /// <seealso cref="From(Location)" />
    public static LocationInfo From(SyntaxToken token)
    {
        return From(token.GetLocation());
    }

    /// <summary>
    ///     Converts this <see cref="LocationInfo" /> back to a <see cref="Location" />.
    /// </summary>
    /// <returns>
    ///     A new <see cref="Location" /> instance created from the stored path, span, and line span.
    /// </returns>
    /// <remarks>
    ///     The returned location is suitable for use in diagnostic reporting.
    /// </remarks>
    public Location ToLocation()
    {
        return Location.Create(Path, Span, LineSpan);
    }
}
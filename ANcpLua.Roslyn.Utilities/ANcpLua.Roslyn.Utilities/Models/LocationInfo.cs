using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable representation of a source location for use in source generators.
/// </summary>
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
    public static LocationInfo From(SyntaxNode node) => From(node.GetLocation());

    /// <summary>
    ///     Creates a <see cref="LocationInfo" /> from a <see cref="SyntaxToken" />.
    /// </summary>
    public static LocationInfo From(SyntaxToken token) => From(token.GetLocation());

    /// <summary>
    ///     Converts back to a <see cref="Location" />.
    /// </summary>
    public Location ToLocation() => Location.Create(Path, Span, LineSpan);
}

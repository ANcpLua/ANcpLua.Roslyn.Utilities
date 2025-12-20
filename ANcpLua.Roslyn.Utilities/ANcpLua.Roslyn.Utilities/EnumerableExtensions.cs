using System.Collections.Generic;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="IEnumerable{T}" />.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     Concatenates strings and cleans up line breaks at the beginning and end of the resulting string.
    ///     Returns " " if collection is empty (to use with
    ///     <see cref="StringExtensions.RemoveBlankLinesWhereOnlyWhitespaces(string)" />).
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static string Inject(this IEnumerable<string> values)
    {
        var text = string.Concat(values)
            .TrimStart('\r', '\n')
            .TrimEnd('\r', '\n');
        return string.IsNullOrWhiteSpace(text) ? " " : text;
    }
}
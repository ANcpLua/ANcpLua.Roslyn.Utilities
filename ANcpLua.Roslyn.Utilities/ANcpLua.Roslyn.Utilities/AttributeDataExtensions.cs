using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="AttributeData" />.
/// </summary>
public static class AttributeDataExtensions
{
    /// <summary>
    ///     Gets the generic type argument at the specified position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ITypeSymbol? GetGenericTypeArgument(this AttributeData attributeData, int position)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(position);
    }

    /// <summary>
    ///     Gets a named argument from the attribute.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static TypedConstant GetNamedArgument(this AttributeData attributeData, string name)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.NamedArguments
            .FirstOrDefault(pair => pair.Key == name)
            .Value;
    }
}
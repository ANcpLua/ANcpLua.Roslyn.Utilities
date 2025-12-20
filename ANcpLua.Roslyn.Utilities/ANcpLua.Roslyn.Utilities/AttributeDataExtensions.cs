using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="AttributeData" />.
/// </summary>
public static class AttributeDataExtensions
{
    /// <param name="attributeData"></param>
    extension(AttributeData attributeData)
    {
        /// <summary>
        ///     Gets the generic type argument at the specified position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ITypeSymbol? GetGenericTypeArgument(int position)
        {
            attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

            return attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(position);
        }

        /// <summary>
        ///     Gets a named argument from the attribute.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public TypedConstant GetNamedArgument(string name)
        {
            attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

            return attributeData.NamedArguments
                .FirstOrDefault(pair => pair.Key == name)
                .Value;
        }
    }
}
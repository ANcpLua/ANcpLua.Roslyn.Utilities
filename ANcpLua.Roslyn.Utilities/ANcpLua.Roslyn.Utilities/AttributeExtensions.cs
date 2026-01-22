using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for extracting values from <see cref="AttributeData" />.
/// </summary>
/// <remarks>
///     <para>
///         These extensions simplify the common task of extracting constructor arguments and named arguments
///         from attributes, eliminating verbose loops and null checks.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Constructor arguments:</b> <see cref="GetConstructorArgument{T}(AttributeData, int)" />,
///                 <see cref="TryGetConstructorArgument{T}(AttributeData, int, out T)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Named arguments:</b> <see cref="GetNamedArgument{T}(AttributeData, string)" />,
///                 <see cref="TryGetNamedArgument{T}(AttributeData, string, out T)" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Symbol helpers:</b> <see cref="GetAttributeConstructorArgument{T}(ISymbol, string, int)" />,
///                 <see cref="GetAttributeNamedArgument{T}(ISymbol, string, string)" />
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // Extract DisplayAttribute.Name
/// var displayName = property.GetAttributeNamedArgument&lt;string&gt;(
///     "System.ComponentModel.DataAnnotations.DisplayAttribute", "Name") ?? property.Name;
///
/// // Extract JsonDerivedTypeAttribute constructor argument
/// var derivedType = attribute.GetConstructorArgument&lt;INamedTypeSymbol&gt;(0);
///
/// // Check if property has a specific attribute value
/// if (property.TryGetAttributeNamedArgument&lt;bool&gt;("MyAttribute", "IsRequired", out var isRequired) &amp;&amp; isRequired)
/// {
///     // Handle required property
/// }
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class AttributeExtensions
{
    // ========== AttributeData Constructor Arguments ==========

    /// <summary>
    ///     Gets a constructor argument value at the specified index.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <returns>
    ///     The argument value cast to <typeparamref name="T" />, or <c>default</c> if the index is out of range
    ///     or the value cannot be cast to the expected type.
    /// </returns>
    /// <example>
    ///     <code>
    /// // [JsonDerivedType(typeof(DerivedClass), "discriminator")]
    /// var derivedType = attribute.GetConstructorArgument&lt;INamedTypeSymbol&gt;(0);
    /// var discriminator = attribute.GetConstructorArgument&lt;string&gt;(1);
    /// </code>
    /// </example>
    public static T? GetConstructorArgument<T>(this AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
            return default;

        if (index < 0 || index >= attribute.ConstructorArguments.Length)
            return default;

        var typedConstant = attribute.ConstructorArguments[index];
        return GetTypedConstantValue<T>(typedConstant);
    }

    /// <summary>
    ///     Attempts to get a constructor argument value at the specified index.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the argument value;
    ///     otherwise, contains the default value for <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the argument exists and can be converted to <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetConstructorArgument<T>(this AttributeData attribute, int index, [NotNullWhen(true)] out T? value)
    {
        value = default;

        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
            return false;

        if (index < 0 || index >= attribute.ConstructorArguments.Length)
            return false;

        var typedConstant = attribute.ConstructorArguments[index];
        if (typedConstant.Kind == TypedConstantKind.Error)
            return false;

        var result = GetTypedConstantValue<T>(typedConstant);
        if (result is null)
            return false;

        value = result;
        return true;
    }

    /// <summary>
    ///     Gets the number of constructor arguments in the attribute.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The number of constructor arguments, or 0 if none exist.</returns>
    public static int GetConstructorArgumentCount(this AttributeData attribute)
        => attribute.ConstructorArguments.IsDefaultOrEmpty ? 0 : attribute.ConstructorArguments.Length;

    // ========== AttributeData Named Arguments ==========

    /// <summary>
    ///     Gets a named argument value by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="name">The name of the named argument.</param>
    /// <returns>
    ///     The argument value cast to <typeparamref name="T" />, or <c>default</c> if the named argument
    ///     doesn't exist or the value cannot be cast to the expected type.
    /// </returns>
    /// <example>
    ///     <code>
    /// // [Display(Name = "User Name", Order = 1)]
    /// var displayName = attribute.GetNamedArgument&lt;string&gt;("Name");
    /// var order = attribute.GetNamedArgument&lt;int&gt;("Order");
    /// </code>
    /// </example>
    public static T? GetNamedArgument<T>(this AttributeData attribute, string name)
    {
        if (attribute.NamedArguments.IsDefaultOrEmpty)
            return default;

        foreach (var namedArg in attribute.NamedArguments)
        {
            if (string.Equals(namedArg.Key, name, StringComparison.Ordinal))
                return GetTypedConstantValue<T>(namedArg.Value);
        }

        return default;
    }

    /// <summary>
    ///     Attempts to get a named argument value by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="name">The name of the named argument.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the argument value;
    ///     otherwise, contains the default value for <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the named argument exists and can be converted to <typeparamref name="T" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetNamedArgument<T>(this AttributeData attribute, string name, [NotNullWhen(true)] out T? value)
    {
        value = default;

        if (attribute.NamedArguments.IsDefaultOrEmpty)
            return false;

        foreach (var namedArg in attribute.NamedArguments)
        {
            if (string.Equals(namedArg.Key, name, StringComparison.Ordinal))
            {
                if (namedArg.Value.Kind == TypedConstantKind.Error)
                    return false;

                var result = GetTypedConstantValue<T>(namedArg.Value);
                if (result is null)
                    return false;

                value = result;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks if a named argument exists in the attribute.
    /// </summary>
    /// <param name="attribute">The attribute data to check.</param>
    /// <param name="name">The name of the named argument.</param>
    /// <returns><c>true</c> if the named argument exists; otherwise, <c>false</c>.</returns>
    public static bool HasNamedArgument(this AttributeData attribute, string name)
    {
        if (attribute.NamedArguments.IsDefaultOrEmpty)
            return false;

        foreach (var namedArg in attribute.NamedArguments)
        {
            if (string.Equals(namedArg.Key, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets all named argument names from the attribute.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>An enumerable of all named argument names.</returns>
    public static IEnumerable<string> GetNamedArgumentNames(this AttributeData attribute)
    {
        if (attribute.NamedArguments.IsDefaultOrEmpty)
            yield break;

        foreach (var namedArg in attribute.NamedArguments)
            yield return namedArg.Key;
    }

    // ========== Symbol Convenience Methods ==========

    /// <summary>
    ///     Gets a constructor argument from a specific attribute on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="fullyQualifiedAttributeName">The fully qualified name of the attribute.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <returns>
    ///     The argument value, or <c>default</c> if the attribute doesn't exist,
    ///     the index is out of range, or the value cannot be cast.
    /// </returns>
    /// <example>
    ///     <code>
    /// // Get the Type argument from [JsonDerivedType(typeof(Derived))]
    /// var derivedType = type.GetAttributeConstructorArgument&lt;INamedTypeSymbol&gt;(
    ///     "System.Text.Json.Serialization.JsonDerivedTypeAttribute", 0);
    /// </code>
    /// </example>
    public static T? GetAttributeConstructorArgument<T>(this ISymbol symbol, string fullyQualifiedAttributeName, int index)
    {
        var attribute = symbol.GetAttribute(fullyQualifiedAttributeName);
        if (attribute is null)
            return default;
        return attribute.GetConstructorArgument<T>(index);
    }

    /// <summary>
    ///     Gets a constructor argument from a specific attribute type on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="attributeType">The type symbol of the attribute.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <param name="inherits">Whether to include inherited attribute types.</param>
    /// <returns>
    ///     The argument value, or <c>default</c> if the attribute doesn't exist,
    ///     the index is out of range, or the value cannot be cast.
    /// </returns>
    public static T? GetAttributeConstructorArgument<T>(this ISymbol symbol, ITypeSymbol? attributeType, int index, bool inherits = true)
    {
        var attribute = symbol.GetAttribute(attributeType, inherits);
        if (attribute is null)
            return default;
        return attribute.GetConstructorArgument<T>(index);
    }

    /// <summary>
    ///     Attempts to get a constructor argument from a specific attribute on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="fullyQualifiedAttributeName">The fully qualified name of the attribute.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the argument value;
    ///     otherwise, contains the default value for <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the attribute exists, the argument exists, and can be converted;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetAttributeConstructorArgument<T>(
        this ISymbol symbol,
        string fullyQualifiedAttributeName,
        int index,
        [NotNullWhen(true)] out T? value)
    {
        value = default;
        var attribute = symbol.GetAttribute(fullyQualifiedAttributeName);
        return attribute is not null && attribute.TryGetConstructorArgument(index, out value);
    }

    /// <summary>
    ///     Gets a named argument from a specific attribute on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="fullyQualifiedAttributeName">The fully qualified name of the attribute.</param>
    /// <param name="argumentName">The name of the named argument.</param>
    /// <returns>
    ///     The argument value, or <c>default</c> if the attribute doesn't exist,
    ///     the named argument doesn't exist, or the value cannot be cast.
    /// </returns>
    /// <example>
    ///     <code>
    /// // Get the Name from [Display(Name = "User Name")]
    /// var displayName = property.GetAttributeNamedArgument&lt;string&gt;(
    ///     "System.ComponentModel.DataAnnotations.DisplayAttribute", "Name") ?? property.Name;
    /// </code>
    /// </example>
    public static T? GetAttributeNamedArgument<T>(this ISymbol symbol, string fullyQualifiedAttributeName, string argumentName)
    {
        var attribute = symbol.GetAttribute(fullyQualifiedAttributeName);
        if (attribute is null)
            return default;
        return attribute.GetNamedArgument<T>(argumentName);
    }

    /// <summary>
    ///     Gets a named argument from a specific attribute type on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="attributeType">The type symbol of the attribute.</param>
    /// <param name="argumentName">The name of the named argument.</param>
    /// <param name="inherits">Whether to include inherited attribute types.</param>
    /// <returns>
    ///     The argument value, or <c>default</c> if the attribute doesn't exist,
    ///     the named argument doesn't exist, or the value cannot be cast.
    /// </returns>
    public static T? GetAttributeNamedArgument<T>(
        this ISymbol symbol,
        ITypeSymbol? attributeType,
        string argumentName,
        bool inherits = true)
    {
        var attribute = symbol.GetAttribute(attributeType, inherits);
        if (attribute is null)
            return default;
        return attribute.GetNamedArgument<T>(argumentName);
    }

    /// <summary>
    ///     Attempts to get a named argument from a specific attribute on a symbol.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument value.</typeparam>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="fullyQualifiedAttributeName">The fully qualified name of the attribute.</param>
    /// <param name="argumentName">The name of the named argument.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the argument value;
    ///     otherwise, contains the default value for <typeparamref name="T" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the attribute exists, the named argument exists, and can be converted;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetAttributeNamedArgument<T>(
        this ISymbol symbol,
        string fullyQualifiedAttributeName,
        string argumentName,
        [NotNullWhen(true)] out T? value)
    {
        value = default;
        var attribute = symbol.GetAttribute(fullyQualifiedAttributeName);
        return attribute is not null && attribute.TryGetNamedArgument(argumentName, out value);
    }

    // ========== Array Arguments ==========

    /// <summary>
    ///     Gets a constructor argument as an array of values.
    /// </summary>
    /// <typeparam name="T">The expected type of the array elements.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <returns>
    ///     An immutable array of values, or an empty array if the argument doesn't exist
    ///     or is not an array type.
    /// </returns>
    /// <example>
    ///     <code>
    /// // [MyAttribute(new[] { "a", "b", "c" })]
    /// var values = attribute.GetConstructorArgumentArray&lt;string&gt;(0);
    /// </code>
    /// </example>
    public static ImmutableArray<T> GetConstructorArgumentArray<T>(this AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
            return ImmutableArray<T>.Empty;

        if (index < 0 || index >= attribute.ConstructorArguments.Length)
            return ImmutableArray<T>.Empty;

        var typedConstant = attribute.ConstructorArguments[index];
        return GetTypedConstantArrayValue<T>(typedConstant);
    }

    /// <summary>
    ///     Gets a named argument as an array of values.
    /// </summary>
    /// <typeparam name="T">The expected type of the array elements.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="name">The name of the named argument.</param>
    /// <returns>
    ///     An immutable array of values, or an empty array if the argument doesn't exist
    ///     or is not an array type.
    /// </returns>
    public static ImmutableArray<T> GetNamedArgumentArray<T>(this AttributeData attribute, string name)
    {
        if (attribute.NamedArguments.IsDefaultOrEmpty)
            return ImmutableArray<T>.Empty;

        foreach (var namedArg in attribute.NamedArguments)
        {
            if (string.Equals(namedArg.Key, name, StringComparison.Ordinal))
                return GetTypedConstantArrayValue<T>(namedArg.Value);
        }

        return ImmutableArray<T>.Empty;
    }

    // ========== Common Patterns ==========

    /// <summary>
    ///     Gets the display name from a DisplayAttribute, falling back to the symbol name.
    /// </summary>
    /// <param name="symbol">The symbol to get the display name for.</param>
    /// <param name="displayAttributeType">
    ///     The type symbol for DisplayAttribute. Pass <c>null</c> to use the string-based lookup.
    /// </param>
    /// <returns>
    ///     The value of DisplayAttribute.Name if present; otherwise, the symbol's name.
    /// </returns>
    /// <example>
    ///     <code>
    /// var displayAttr = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.DisplayAttribute");
    /// var displayName = property.GetDisplayName(displayAttr);
    /// </code>
    /// </example>
    public static string GetDisplayName(this ISymbol symbol, INamedTypeSymbol? displayAttributeType)
    {
        if (displayAttributeType is not null)
        {
            var displayName = symbol.GetAttributeNamedArgument<string>(displayAttributeType, "Name");
            if (!string.IsNullOrEmpty(displayName))
                return displayName!;
        }

        return symbol.Name;
    }

    /// <summary>
    ///     Gets the display name from a DisplayAttribute using string-based lookup, falling back to the symbol name.
    /// </summary>
    /// <param name="symbol">The symbol to get the display name for.</param>
    /// <returns>
    ///     The value of DisplayAttribute.Name if present; otherwise, the symbol's name.
    /// </returns>
    public static string GetDisplayName(this ISymbol symbol)
    {
        var displayName = symbol.GetAttributeNamedArgument<string>(
            "System.ComponentModel.DataAnnotations.DisplayAttribute", "Name");
        return !string.IsNullOrEmpty(displayName) ? displayName! : symbol.Name;
    }

    /// <summary>
    ///     Gets all type arguments from JsonDerivedTypeAttribute instances on a type.
    /// </summary>
    /// <param name="type">The type to get derived types from.</param>
    /// <param name="jsonDerivedTypeAttribute">The type symbol for JsonDerivedTypeAttribute.</param>
    /// <returns>
    ///     An immutable array of derived type symbols, or <c>null</c> if no JsonDerivedTypeAttribute is found.
    /// </returns>
    /// <example>
    ///     <code>
    /// var jsonDerivedType = compilation.GetTypeByMetadataName(
    ///     "System.Text.Json.Serialization.JsonDerivedTypeAttribute");
    /// var derivedTypes = baseType.GetJsonDerivedTypes(jsonDerivedType);
    /// </code>
    /// </example>
    public static ImmutableArray<INamedTypeSymbol>? GetJsonDerivedTypes(
        this ITypeSymbol type,
        INamedTypeSymbol? jsonDerivedTypeAttribute)
    {
        if (jsonDerivedTypeAttribute is null)
            return null;

        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        foreach (var attribute in type.GetAttributes(jsonDerivedTypeAttribute))
        {
            var derivedType = attribute.GetConstructorArgument<INamedTypeSymbol>(0);
            if (derivedType is not null && !SymbolEqualityComparer.Default.Equals(derivedType, type))
                builder.Add(derivedType);
        }

        return builder.Count == 0 ? null : builder.ToImmutable();
    }

    // ========== Private Helpers ==========

    private static T? GetTypedConstantValue<T>(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Error)
            return default;

        // Handle null values
        if (typedConstant.IsNull)
            return default;

        // Handle type references (typeof expressions)
        if (typedConstant.Kind == TypedConstantKind.Type && typeof(T).IsAssignableFrom(typeof(ITypeSymbol)))
            return (T?)typedConstant.Value;

        // Handle regular values
        if (typedConstant.Value is T value)
            return value;

        // Try conversion for compatible types
        if (typedConstant.Value is not null)
        {
            try
            {
                return (T)Convert.ChangeType(typedConstant.Value, typeof(T));
            }
            catch
            {
                // Conversion failed
            }
        }

        return default;
    }

    private static ImmutableArray<T> GetTypedConstantArrayValue<T>(TypedConstant typedConstant)
    {
        if (typedConstant.Kind != TypedConstantKind.Array)
            return ImmutableArray<T>.Empty;

        if (typedConstant.Values.IsDefaultOrEmpty)
            return ImmutableArray<T>.Empty;

        var builder = ImmutableArray.CreateBuilder<T>(typedConstant.Values.Length);

        foreach (var element in typedConstant.Values)
        {
            var value = GetTypedConstantValue<T>(element);
            if (value is not null)
                builder.Add(value);
        }

        return builder.ToImmutable();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides extension methods for Roslyn symbol types.
/// </summary>
/// <remarks>
///     <para>
///         This class contains utility methods for working with <see cref="ISymbol" /> and its derived types,
///         including methods for symbol comparison, attribute lookup, accessibility checking, and member traversal.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Symbol comparison using <see cref="SymbolEqualityComparer.Default" /></description>
///         </item>
///         <item>
///             <description>Attribute lookup by name or type symbol with optional inheritance</description>
///         </item>
///         <item>
///             <description>Accessibility and visibility checks</description>
///         </item>
///         <item>
///             <description>Member traversal including inherited members</description>
///         </item>
///         <item>
///             <description>Type parameter and type argument retrieval</description>
///         </item>
///         <item>
///             <description>Interface implementation detection</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="ISymbol" />
/// <seealso cref="ITypeSymbol" />
/// <seealso cref="IMethodSymbol" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class SymbolExtensions
{
    /// <summary>
    ///     Compares two symbols for equality using <see cref="SymbolEqualityComparer.Default" />.
    /// </summary>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="expectedType">The symbol to compare against.</param>
    /// <returns>
    ///     <c>true</c> if both symbols are non-null and equal according to <see cref="SymbolEqualityComparer.Default" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="SymbolEqualityComparer.Default" />
    public static bool IsEqualTo(this ISymbol? symbol, [NotNullWhen(true)] ISymbol? expectedType)
    {
        if (symbol is null || expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(expectedType, symbol);
    }

    /// <summary>
    ///     Gets the fully qualified name of a type symbol in the <c>global::Namespace.Type</c> format.
    /// </summary>
    /// <param name="symbol">The type symbol to get the name for.</param>
    /// <returns>The fully qualified name including the <c>global::</c> prefix.</returns>
    /// <seealso cref="GetMetadataName" />
    /// <seealso cref="SymbolDisplayFormat.FullyQualifiedFormat" />
    public static string GetFullyQualifiedName(this ITypeSymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    ///     Gets the metadata name of a type symbol in the <c>Namespace.Type</c> format.
    /// </summary>
    /// <param name="symbol">The type symbol to get the name for.</param>
    /// <returns>The metadata name without the <c>global::</c> prefix.</returns>
    /// <seealso cref="GetFullyQualifiedName" />
    /// <seealso cref="SymbolDisplayFormat.CSharpErrorMessageFormat" />
    public static string GetMetadataName(this ITypeSymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    /// <summary>
    ///     Gets the reflection-style full name of a symbol, suitable for <c>Type.GetType()</c>.
    /// </summary>
    /// <param name="symbol">The symbol to get the reflection name for.</param>
    /// <returns>
    ///     A string in the format <c>Namespace.Outer+Inner</c> using <see cref="ISymbol.MetadataName" />
    ///     and <c>+</c> as the nested type separator.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This produces the same format as <see cref="Type.FullName" /> at runtime,
    ///         using <c>MetadataName</c> (which includes generic arity suffixes like <c>`1</c>)
    ///         and <c>+</c> for nested types. Useful when generated code needs to reference types
    ///         via reflection or <c>Type.GetType()</c>.
    ///     </para>
    /// </remarks>
    public static string GetReflectionFullName(this ISymbol symbol)
    {
        var typeNames = new Stack<string>();
        for (ISymbol? current = symbol; current is ITypeSymbol; current = current.ContainingType)
            typeNames.Push(current.MetadataName);

        var typeName = string.Join("+", typeNames);
        var ns = symbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : symbol.ContainingNamespace?.ToDisplayString();

        return string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
    }

    /// <summary>
    ///     Checks if the symbol has public accessibility.
    /// </summary>
    public static bool IsPublic(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public;

    /// <summary>
    ///     Checks if the symbol has internal accessibility.
    /// </summary>
    public static bool IsInternal(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Internal;

    /// <summary>
    ///     Checks if the symbol has private accessibility.
    /// </summary>
    public static bool IsPrivate(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Private;

    /// <summary>
    ///     Checks if the symbol has protected accessibility.
    /// </summary>
    public static bool IsProtected(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Protected;

    /// <summary>
    ///     Checks if a symbol has a specific attribute identified by its fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type (e.g., <c>"System.ObsoleteAttribute"</c>).
    /// </param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         The fully qualified name must match the format returned by
    ///         <c>ISymbol.ToDisplayString()</c> using the default format, which is
    ///         <c>Namespace.TypeName</c> without the <c>global::</c> prefix.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Include the full namespace path (e.g., <c>"System.Serializable"</c>)</description>
    ///         </item>
    ///         <item>
    ///             <description>Include the <c>Attribute</c> suffix if present in the type name</description>
    ///         </item>
    ///         <item>
    ///             <description>For nested types, use <c>+</c> separator (e.g., <c>"Outer+InnerAttribute"</c>)</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 For generic attributes, include type parameters (e.g., <c>"MyNamespace.GenericAttribute`1"</c>
    ///                 )
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for common attributes
    /// if (methodSymbol.HasAttribute("System.ObsoleteAttribute"))
    /// {
    ///     // Method is marked obsolete
    /// }
    ///
    /// // Check for custom attributes
    /// if (classSymbol.HasAttribute("MyNamespace.MyCustomAttribute"))
    /// {
    ///     // Apply custom logic
    /// }
    ///
    /// // Check for serialization attributes
    /// if (fieldSymbol.HasAttribute("System.NonSerializedAttribute"))
    /// {
    ///     // Field should be excluded from serialization
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return true;

        return false;
    }

    /// <summary>
    ///     Gets the first attribute matching the specified fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to search for the attribute.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type (e.g., <c>"System.ObsoleteAttribute"</c>).
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute, or <c>null</c> if not found.
    /// </returns>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return attribute;

        return null;
    }

    /// <summary>
    ///     Gets all attributes of a specific type, optionally including inherited attribute types.
    /// </summary>
    /// <param name="symbol">The symbol to search for attributes.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />;
    ///     <c>false</c> to match only exact types. Defaults to <c>true</c>.
    ///     This parameter is ignored if <paramref name="attributeType" /> is sealed.
    /// </param>
    /// <returns>
    ///     An enumerable of <see cref="AttributeData" /> for all matching attributes.
    ///     Returns an empty enumerable if <paramref name="attributeType" /> is <c>null</c>.
    /// </returns>
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol? attributeType,
        bool inherits = true)
    {
        if (attributeType is null)
            yield break;

        if (attributeType.IsSealed)
            inherits = false;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is null)
                continue;

            if (inherits)
            {
                if (attribute.AttributeClass.IsOrInheritsFrom(attributeType))
                    yield return attribute;
            }
            else
            {
                if (SymbolEqualityComparer.Default.Equals(attributeType, attribute.AttributeClass))
                    yield return attribute;
            }
        }
    }

    /// <summary>
    ///     Gets the first attribute of a specific type, optionally including inherited attribute types.
    /// </summary>
    /// <param name="symbol">The symbol to search for the attribute.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />;
    ///     <c>false</c> to match only exact types. Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute, or <c>null</c> if not found.
    /// </returns>
    /// <seealso cref="GetAttributes(ISymbol, ITypeSymbol?, bool)" />
    /// <seealso cref="HasAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true) => symbol.GetAttributes(attributeType, inherits).FirstOrDefault();

    /// <summary>
    ///     Checks if a symbol has an attribute of a specific type.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeType">The type symbol representing the attribute type to find.</param>
    /// <param name="inherits">
    ///     <c>true</c> to include attributes that inherit from <paramref name="attributeType" />;
    ///     <c>false</c> to match only exact types. Defaults to <c>true</c>.
    /// </param>
    /// <returns><c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.</returns>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttribute(ISymbol, ITypeSymbol?, bool)" />
    public static bool HasAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true) => symbol.GetAttribute(attributeType, inherits) is not null;

    /// <summary>
    ///     Checks if a symbol is visible outside its containing assembly.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol and all its containing types have public, protected,
    ///     or protected internal accessibility; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method recursively checks the containing type hierarchy to ensure
    ///         the symbol is accessible from outside the assembly.
    ///     </para>
    /// </remarks>
    public static bool IsVisibleOutsideOfAssembly([NotNullWhen(true)] this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Protected
            and not Accessibility.ProtectedOrInternal)
            return false;

        return symbol.ContainingType is null || symbol.ContainingType.IsVisibleOutsideOfAssembly();
    }

    /// <summary>
    ///     Checks if a symbol is a user-defined operator or conversion method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is an <see cref="IMethodSymbol" /> with
    ///     <see cref="MethodKind.UserDefinedOperator" /> or <see cref="MethodKind.Conversion" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool IsOperator(this ISymbol? symbol) =>
        symbol is IMethodSymbol
        {
            MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion
        };

    /// <summary>
    ///     Checks if a symbol is a const field.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is an <see cref="IFieldSymbol" /> with <see cref="IFieldSymbol.IsConst" />
    ///     set to <c>true</c>; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsConst(this ISymbol? symbol) => symbol is IFieldSymbol { IsConst: true };

    /// <summary>
    ///     Gets all members of a type including members inherited from base types.
    /// </summary>
    /// <param name="symbol">The type symbol to get members from.</param>
    /// <returns>
    ///     An enumerable of all members from the type and its entire base type hierarchy.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?, string)" />
    /// <seealso cref="GetAllMembers(INamespaceOrTypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers())
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    /// <summary>
    ///     Gets all members with the specified name from a type including inherited members.
    /// </summary>
    /// <param name="symbol">The type symbol to get members from.</param>
    /// <param name="name">The name of the members to find.</param>
    /// <returns>
    ///     An enumerable of all members with the specified name from the type and its base type hierarchy.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?)" />
    /// <seealso cref="GetAllMembers(INamespaceOrTypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    /// <summary>
    ///     Gets all members with the specified name from a namespace or type symbol,
    ///     including inherited members and interface members for interface types.
    /// </summary>
    /// <param name="symbol">The namespace or type symbol to get members from.</param>
    /// <param name="name">The name of the members to find.</param>
    /// <returns>
    ///     An enumerable of all members with the specified name. For interface types,
    ///     this includes members from all inherited interfaces.
    /// </returns>
    /// <seealso cref="GetAllMembers(ITypeSymbol?)" />
    /// <seealso cref="GetAllMembers(ITypeSymbol?, string)" />
    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
                foreach (var iface in interfaceSymbol.AllInterfaces)
                foreach (var member in iface.GetMembers(name))
                    yield return member;

            if (symbol is ITypeSymbol typeSymbol)
                symbol = typeSymbol.BaseType;
            else
                yield break;
        }
    }

    /// <summary>
    ///     Checks if a symbol is declared in a top-level statement.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     <c>true</c> if the symbol is declared in a compilation unit (top-level statements);
    ///     <c>false</c> if it has no declaring syntax references, is declared elsewhere,
    ///     or is in a generated file (ending with <c>.g.cs</c>).
    /// </returns>
    public static bool IsTopLevelStatement(this ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences.Length is 0)
            return false;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            if (!syntax.IsKind(SyntaxKind.CompilationUnit))
            {
                if (syntax.SyntaxTree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal))
                    continue;

                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Gets the type associated with a symbol, depending on the symbol kind.
    /// </summary>
    /// <param name="symbol">The symbol to get the type from.</param>
    /// <returns>
    ///     The associated <see cref="ITypeSymbol" />:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For <see cref="IParameterSymbol" />: the parameter type</description>
    ///         </item>
    ///         <item>
    ///             <description>For <see cref="IFieldSymbol" />: the field type</description>
    ///         </item>
    ///         <item>
    ///             <description>For <see cref="IPropertySymbol" /> with a getter: the property type</description>
    ///         </item>
    ///         <item>
    ///             <description>For <see cref="ILocalSymbol" />: the local variable type</description>
    ///         </item>
    ///         <item>
    ///             <description>For <see cref="IMethodSymbol" />: the return type</description>
    ///         </item>
    ///         <item>
    ///             <description>For other symbols: <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </returns>
    public static ITypeSymbol? GetSymbolType(this ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol { GetMethod: not null } property => property.Type,
            ILocalSymbol local => local.Type,
            IMethodSymbol method => method.ReturnType,
            _ => null
        };
    }

    /// <summary>
    ///     Gets the containing namespace of a symbol as a string.
    /// </summary>
    /// <param name="symbol">The symbol to get the namespace from.</param>
    /// <returns>
    ///     The fully qualified namespace name, or an empty string if the symbol
    ///     is in the global namespace.
    /// </returns>
    public static string GetNamespaceName(this ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }

    /// <summary>
    ///     Gets a single method by name from a named type symbol.
    /// </summary>
    /// <param name="type">The type to search for the method.</param>
    /// <param name="name">The name of the method to find.</param>
    /// <returns>
    ///     The <see cref="IMethodSymbol" /> if exactly one method with the specified name exists;
    ///     <c>null</c> if no method is found or if multiple methods with the same name exist (overloads).
    /// </returns>
    /// <seealso cref="GetProperty" />
    public static IMethodSymbol? GetMethod(this INamedTypeSymbol type, string name)
    {
        IMethodSymbol? result = null;
        foreach (var member in type.GetMembers(name))
            if (member is IMethodSymbol method)
            {
                if (result is not null)
                    return null; // Multiple methods with same name
                result = method;
            }

        return result;
    }

    /// <summary>
    ///     Gets a single property by name from a named type symbol.
    /// </summary>
    /// <param name="type">The type to search for the property.</param>
    /// <param name="name">The name of the property to find.</param>
    /// <returns>
    ///     The <see cref="IPropertySymbol" /> if a property with the specified name exists;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <seealso cref="GetMethod" />
    public static IPropertySymbol? GetProperty(this INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
            if (member is IPropertySymbol property)
                return property;

        return null;
    }

    /// <summary>
    ///     Checks if a method explicitly implements a specific interface method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="interfaceMethod">The interface method to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="method" /> explicitly implements
    ///     <paramref name="interfaceMethod" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="ExplicitOrImplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitInterfaceImplementations" />
    public static bool ExplicitlyImplements(this IMethodSymbol method, IMethodSymbol interfaceMethod)
    {
        foreach (var impl in method.ExplicitInterfaceImplementations)
            if (SymbolEqualityComparer.Default.Equals(impl, interfaceMethod))
                return true;

        return false;
    }

    /// <summary>
    ///     Gets all explicit and implicit interface implementations for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get implementations for.</param>
    /// <returns>
    ///     An immutable array of interface members that <paramref name="symbol" /> implements,
    ///     both explicitly and implicitly. Returns an empty array if the symbol is not a
    ///     method, property, or event.
    /// </returns>
    /// <seealso cref="ExplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitlyImplements" />
    public static ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations(this ISymbol symbol)
    {
        if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
            return ImmutableArray<ISymbol>.Empty;

        var containingType = symbol.ContainingType;
        var query = containingType.AllInterfaces
            .SelectMany(iface => iface.GetMembers(), (iface, interfaceMember) => new { iface, interfaceMember })
            .Select(t => new { t, impl = containingType.FindImplementationForInterfaceMember(t.interfaceMember) })
            .Where(t => SymbolEqualityComparer.Default.Equals(symbol, t.impl))
            .Select(t => t.t.interfaceMember);
        return [..query];
    }

    /// <summary>
    ///     Gets all explicit interface implementations for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get explicit implementations for.</param>
    /// <returns>
    ///     An immutable array of interface members that <paramref name="symbol" /> explicitly implements.
    ///     Returns an empty array if the symbol is not an event, method, or property.
    /// </returns>
    /// <seealso cref="ExplicitOrImplicitInterfaceImplementations" />
    /// <seealso cref="ExplicitlyImplements" />
    public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
    {
        return symbol switch
        {
            IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
            IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
            IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
            _ => ImmutableArray<ISymbol>.Empty
        };
    }

    /// <summary>
    ///     Gets all type parameters from a symbol and its containing types.
    /// </summary>
    /// <param name="symbol">The symbol to get type parameters from.</param>
    /// <returns>
    ///     An immutable array containing all type parameters from the symbol
    ///     and its entire containing type hierarchy.
    /// </returns>
    /// <seealso cref="GetTypeParameters" />
    /// <seealso cref="GetAllTypeArguments" />
    public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this ISymbol? symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeParameterSymbol>();
        while (symbol is not null)
        {
            results.AddRange(symbol.GetTypeParameters());
            symbol = symbol.ContainingType;
        }

        return results.ToImmutable();
    }

    /// <summary>
    ///     Gets the type parameters of a method or named type symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get type parameters from.</param>
    /// <returns>
    ///     The type parameters of the symbol if it is an <see cref="IMethodSymbol" /> or
    ///     <see cref="INamedTypeSymbol" />; otherwise, an empty immutable array.
    /// </returns>
    /// <seealso cref="GetAllTypeParameters" />
    /// <seealso cref="GetTypeArguments" />
    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol nt => nt.TypeParameters,
            _ => ImmutableArray<ITypeParameterSymbol>.Empty
        };
    }

    /// <summary>
    ///     Gets the type arguments of a method or named type symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get type arguments from.</param>
    /// <returns>
    ///     The type arguments of the symbol if it is an <see cref="IMethodSymbol" /> or
    ///     <see cref="INamedTypeSymbol" />; otherwise, an empty immutable array.
    /// </returns>
    /// <seealso cref="GetAllTypeArguments" />
    /// <seealso cref="GetTypeParameters" />
    public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeArguments,
            INamedTypeSymbol nt => nt.TypeArguments,
            _ => ImmutableArray<ITypeSymbol>.Empty
        };
    }

    /// <summary>
    ///     Gets the overridden member for a method, property, or event.
    /// </summary>
    /// <param name="symbol">The symbol to get the overridden member from.</param>
    /// <returns>
    ///     The overridden symbol:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>For <see cref="IMethodSymbol" />: <see cref="IMethodSymbol.OverriddenMethod" /></description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 For <see cref="IPropertySymbol" />: <see cref="IPropertySymbol.OverriddenProperty" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>For <see cref="IEventSymbol" />: <see cref="IEventSymbol.OverriddenEvent" /></description>
    ///         </item>
    ///         <item>
    ///             <description>For other symbols: <c>null</c></description>
    ///         </item>
    ///     </list>
    /// </returns>
    public static ISymbol? GetOverriddenMember(this ISymbol? symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.OverriddenMethod,
            IPropertySymbol property => property.OverriddenProperty,
            IEventSymbol @event => @event.OverriddenEvent,
            _ => null
        };
    }

    /// <summary>
    ///     Gets all type arguments from a symbol and its containing types.
    /// </summary>
    /// <param name="symbol">The symbol to get type arguments from.</param>
    /// <returns>
    ///     An immutable array containing all type arguments from the symbol
    ///     and its entire containing type hierarchy.
    /// </returns>
    /// <seealso cref="GetTypeArguments" />
    /// <seealso cref="GetAllTypeParameters" />
    public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
    {
        var results = ImmutableArray.CreateBuilder<ITypeSymbol>();
        results.AddRange(symbol.GetTypeArguments());

        var containingType = symbol.ContainingType;
        while (containingType is not null)
        {
            results.AddRange(containingType.GetTypeArguments());
            containingType = containingType.ContainingType;
        }

        return results.ToImmutable();
    }

    // ========== Short Name Attribute Matching ==========

    /// <summary>
    ///     Checks if a symbol has an attribute by its short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     <c>true</c> if the symbol has an attribute matching the short name;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method matches attributes by their short class name, ignoring namespaces.
    ///         It automatically handles the "Attribute" suffix convention:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 If <paramref name="attributeShortName" /> is "Obsolete", matches both
    ///                 "Obsolete" and "ObsoleteAttribute"
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If <paramref name="attributeShortName" /> is "ObsoleteAttribute", matches both
    ///                 "Obsolete" and "ObsoleteAttribute"
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         This is useful when you need to check for an attribute but don't have the
    ///         <see cref="INamedTypeSymbol" /> for it (e.g., when the attribute type might
    ///         not be available in the current compilation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check for RequiresUnreferencedCode attribute
    /// if (method.HasAttributeByShortName("RequiresUnreferencedCode"))
    /// {
    ///     // Method requires unreferenced code
    /// }
    ///
    /// // Both of these work the same:
    /// symbol.HasAttributeByShortName("Obsolete")
    /// symbol.HasAttributeByShortName("ObsoleteAttribute")
    /// </code>
    /// </example>
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    /// <seealso cref="GetAttributeByShortName" />
    public static bool HasAttributeByShortName(this ISymbol symbol, string attributeShortName)
    {
        var nameWithoutSuffix = attributeShortName.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeShortName[..^9]
            : attributeShortName;
        var nameWithSuffix = nameWithoutSuffix + "Attribute";

        foreach (var attribute in symbol.GetAttributes())
        {
            var className = attribute.AttributeClass?.Name;
            if (className is null)
                continue;

            if (string.Equals(className, nameWithoutSuffix, StringComparison.Ordinal) ||
                string.Equals(className, nameWithSuffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets the first attribute matching the short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to get the attribute from.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     The <see cref="AttributeData" /> for the first matching attribute,
    ///     or <c>null</c> if no attribute matches.
    /// </returns>
    /// <seealso cref="HasAttributeByShortName" />
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    public static AttributeData? GetAttributeByShortName(this ISymbol symbol, string attributeShortName)
    {
        var nameWithoutSuffix = attributeShortName.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeShortName[..^9]
            : attributeShortName;
        var nameWithSuffix = nameWithoutSuffix + "Attribute";

        foreach (var attribute in symbol.GetAttributes())
        {
            var className = attribute.AttributeClass?.Name;
            if (className is null)
                continue;

            if (string.Equals(className, nameWithoutSuffix, StringComparison.Ordinal) ||
                string.Equals(className, nameWithSuffix, StringComparison.Ordinal))
                return attribute;
        }

        return null;
    }

    /// <summary>
    ///     Gets all attributes matching the short name (with or without "Attribute" suffix).
    /// </summary>
    /// <param name="symbol">The symbol to get the attributes from.</param>
    /// <param name="attributeShortName">
    ///     The short name of the attribute (e.g., "Obsolete" or "ObsoleteAttribute").
    /// </param>
    /// <returns>
    ///     An enumerable of <see cref="AttributeData" /> for all matching attributes.
    /// </returns>
    /// <seealso cref="HasAttributeByShortName" />
    /// <seealso cref="GetAttributeByShortName" />
    public static IEnumerable<AttributeData> GetAttributesByShortName(this ISymbol symbol, string attributeShortName)
    {
        var nameWithoutSuffix = attributeShortName.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeShortName[..^9]
            : attributeShortName;
        var nameWithSuffix = nameWithoutSuffix + "Attribute";

        foreach (var attribute in symbol.GetAttributes())
        {
            var className = attribute.AttributeClass?.Name;
            if (className is null)
                continue;

            if (string.Equals(className, nameWithoutSuffix, StringComparison.Ordinal) ||
                string.Equals(className, nameWithSuffix, StringComparison.Ordinal))
                yield return attribute;
        }
    }

    // ========== Attribute Type Argument Extraction ==========

    /// <summary>
    ///     Extracts fully-qualified type names from <c>typeof()</c> constructor arguments
    ///     of all attributes matching the specified name.
    /// </summary>
    /// <param name="symbol">The symbol to extract attribute type arguments from.</param>
    /// <param name="fullyQualifiedAttributeName">
    ///     The fully qualified name of the attribute type (e.g., <c>"MyNamespace.MyAttribute"</c>).
    /// </param>
    /// <returns>
    ///     An <see cref="EquatableArray{T}" /> of fully-qualified type name strings,
    ///     sorted by ordinal comparison for deterministic incremental generator caching.
    ///     Returns an empty array if no matching attributes are found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method extracts the first constructor argument from each matching attribute
    ///         when that argument is a <c>typeof()</c> expression. It is designed for the common
    ///         generator pattern where attributes declare types:
    ///     </para>
    ///     <code>
    ///     [SendsMessage(typeof(Request))]
    ///     [YieldsOutput(typeof(Response))]
    ///     public partial class MyExecutor { }
    ///     </code>
    ///     <para>
    ///         Results are sorted by <see cref="StringComparer.Ordinal" /> to ensure consistent
    ///         ordering for incremental generator caching, since attribute order is not guaranteed
    ///         across partial class declarations.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetAttribute(ISymbol, string)" />
    /// <seealso cref="HasAttribute(ISymbol, string)" />
    public static EquatableArray<string> GetAttributeTypeArguments(
        this ISymbol symbol,
        string fullyQualifiedAttributeName)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol)
                builder.Add(typeSymbol.GetFullyQualifiedName());

        if (builder.Count == 0)
            return default;

        builder.Sort(StringComparer.Ordinal);
        return new EquatableArray<string>(builder.ToImmutable());
    }
}
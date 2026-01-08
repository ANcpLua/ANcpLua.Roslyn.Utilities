using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for Roslyn symbol types.
/// </summary>
public static class SymbolExtensions
{
    /// <summary>
    ///     Compares two symbols for equality using SymbolEqualityComparer.Default.
    /// </summary>
    public static bool IsEqualTo(this ISymbol? symbol, [NotNullWhen(true)] ISymbol? expectedType)
    {
        if (symbol is null || expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(expectedType, symbol);
    }

    /// <summary>
    ///     Gets the fully qualified name of a type symbol (global::Namespace.Type format).
    /// </summary>
    public static string GetFullyQualifiedName(this ITypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    ///     Gets the metadata name of a type symbol (Namespace.Type format).
    /// </summary>
    public static string GetMetadataName(this ITypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    /// <summary>
    ///     Checks if a type symbol has a specific attribute (by string name).
    /// </summary>
    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets the first attribute matching the specified name, or null.
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
                return attribute;
        }

        return null;
    }

    /// <summary>
    ///     Checks if a symbol is visible outside its assembly.
    /// </summary>
    public static bool IsVisibleOutsideOfAssembly([NotNullWhen(true)] this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Protected
            and not Accessibility.ProtectedOrInternal)
        {
            return false;
        }

        return symbol.ContainingType is null || symbol.ContainingType.IsVisibleOutsideOfAssembly();
    }

    /// <summary>
    ///     Checks if a symbol is an operator.
    /// </summary>
    public static bool IsOperator(this ISymbol? symbol) => symbol is IMethodSymbol
    {
        MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion
    };

    /// <summary>
    ///     Checks if a symbol is a const field.
    /// </summary>
    public static bool IsConst(this ISymbol? symbol) => symbol is IFieldSymbol { IsConst: true };

    /// <summary>
    ///     Gets all members including inherited members.
    /// </summary>
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
    ///     Gets all members by name including inherited members.
    /// </summary>
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
    ///     Gets all members including inherited and interface members.
    /// </summary>
    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
            {
                foreach (var iface in interfaceSymbol.AllInterfaces)
                foreach (var member in iface.GetMembers(name))
                    yield return member;
            }

            if (symbol is ITypeSymbol typeSymbol)
                symbol = typeSymbol.BaseType;
            else
                yield break;
        }
    }

    /// <summary>
    ///     Checks if a symbol is declared in a top-level statement.
    /// </summary>
    public static bool IsTopLevelStatement(this ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences.Length is 0)
            return false;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            if (!syntax.IsKind(SyntaxKind.CompilationUnit))
            {
                if (syntax.SyntaxTree.FilePath?.EndsWith(".g.cs", StringComparison.Ordinal) is true)
                    continue;

                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Gets the type of a symbol (parameter, field, property, local, or method return type).
    /// </summary>
    public static ITypeSymbol? GetSymbolType(this ISymbol symbol) =>
        symbol switch
        {
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol { GetMethod: not null } property => property.Type,
            ILocalSymbol local => local.Type,
            IMethodSymbol method => method.ReturnType,
            _ => null
        };

    /// <summary>
    ///     Gets the containing namespace as a string, or empty if global.
    /// </summary>
    public static string GetNamespaceName(this ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }

    /// <summary>
    ///     Gets a single method by name from a type, or null if not found or multiple exist.
    /// </summary>
    public static IMethodSymbol? GetMethod(this INamedTypeSymbol type, string name)
    {
        IMethodSymbol? result = null;
        foreach (var member in type.GetMembers(name))
        {
            if (member is IMethodSymbol method)
            {
                if (result is not null)
                    return null; // Multiple methods with same name
                result = method;
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets a single property by name from a type, or null if not found.
    /// </summary>
    public static IPropertySymbol? GetProperty(this INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
        {
            if (member is IPropertySymbol property)
                return property;
        }

        return null;
    }

    /// <summary>
    ///     Checks if a method explicitly implements a specific interface method.
    /// </summary>
    public static bool ExplicitlyImplements(this IMethodSymbol method, IMethodSymbol interfaceMethod)
    {
        foreach (var impl in method.ExplicitInterfaceImplementations)
        {
            if (SymbolEqualityComparer.Default.Equals(impl, interfaceMethod))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets all explicit and implicit interface implementations for a symbol.
    /// </summary>
    public static ImmutableArray<ISymbol> ExplicitOrImplicitInterfaceImplementations(this ISymbol symbol)
    {
        if (symbol.Kind is not SymbolKind.Method and not SymbolKind.Property and not SymbolKind.Event)
            return ImmutableArray<ISymbol>.Empty;

        var containingType = symbol.ContainingType;
        var query = from iface in containingType.AllInterfaces
            from interfaceMember in iface.GetMembers()
            let impl = containingType.FindImplementationForInterfaceMember(interfaceMember)
            where SymbolEqualityComparer.Default.Equals(symbol, impl)
            select interfaceMember;
        return query.ToImmutableArray();
    }

    /// <summary>
    ///     Gets all explicit interface implementations for a symbol.
    /// </summary>
    public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol) =>
        symbol switch
        {
            IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
            IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
            IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
            _ => ImmutableArray<ISymbol>.Empty
        };

    /// <summary>
    ///     Gets all type parameters from a symbol and its containing types.
    /// </summary>
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
    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol) =>
        symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol nt => nt.TypeParameters,
            _ => ImmutableArray<ITypeParameterSymbol>.Empty
        };

    /// <summary>
    ///     Gets the type arguments of a method or named type symbol.
    /// </summary>
    public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol) =>
        symbol switch
        {
            IMethodSymbol m => m.TypeArguments,
            INamedTypeSymbol nt => nt.TypeArguments,
            _ => ImmutableArray<ITypeSymbol>.Empty
        };

    /// <summary>
    ///     Gets the overridden member for a method, property, or event.
    /// </summary>
    public static ISymbol? GetOverriddenMember(this ISymbol? symbol) =>
        symbol switch
        {
            IMethodSymbol method => method.OverriddenMethod,
            IPropertySymbol property => property.OverriddenProperty,
            IEventSymbol @event => @event.OverriddenEvent,
            _ => null
        };

    /// <summary>
    ///     Gets all type arguments from a symbol and its containing types.
    /// </summary>
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
}

using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="ITypeSymbol" />.
/// </summary>
public static class TypeSymbolExtensions
{
    /// <summary>
    ///     Gets all interfaces including the type itself if it's an interface.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
    {
        var allInterfaces = type.AllInterfaces;
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType && !allInterfaces.Contains(namedType))
        {
            var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
            result.AddRange(allInterfaces);
            result.Add(namedType);
            return result;
        }

        return allInterfaces;
    }

    /// <summary>
    ///     Checks if a type inherits from a base type (using SymbolEqualityComparer).
    /// </summary>
    public static bool InheritsFrom(this ITypeSymbol classSymbol, ITypeSymbol? baseClassType)
    {
        if (baseClassType is null)
            return false;

        var baseType = classSymbol.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseClassType, baseType))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type implements an interface (using SymbolEqualityComparer).
    /// </summary>
    public static bool Implements(this ITypeSymbol classSymbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface)) return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is or implements an interface.
    /// </summary>
    public static bool IsOrImplements(this ITypeSymbol symbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in symbol.GetAllInterfacesIncludingThis())
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets attributes of a specific type with optional inheritance checking.
    /// </summary>
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
    ///     Gets the first attribute of a specific type.
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true) =>
        symbol.GetAttributes(attributeType, inherits).FirstOrDefault();

    /// <summary>
    ///     Checks if a symbol has an attribute of a specific type.
    /// </summary>
    public static bool HasAttribute(this ISymbol symbol, ITypeSymbol? attributeType, bool inherits = true) =>
        symbol.GetAttribute(attributeType, inherits) is not null;

    /// <summary>
    ///     Checks if a type is or inherits from another type.
    /// </summary>
    public static bool IsOrInheritsFrom(this ITypeSymbol symbol, ITypeSymbol? expectedType)
    {
        if (expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(symbol, expectedType) ||
               (!expectedType.IsSealed && symbol.InheritsFrom(expectedType));
    }

    public static bool IsObject(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Object;
    public static bool IsString(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_String;
    public static bool IsChar(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Char;
    public static bool IsInt32(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int32;
    public static bool IsInt64(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int64;
    public static bool IsBoolean(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Boolean;
    public static bool IsDateTime(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_DateTime;
    public static bool IsByte(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Byte;
    public static bool IsSByte(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_SByte;
    public static bool IsInt16(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int16;
    public static bool IsUInt16(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt16;
    public static bool IsUInt32(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt32;
    public static bool IsUInt64(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt64;
    public static bool IsSingle(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Single;
    public static bool IsDouble(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Double;
    public static bool IsDecimal(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Decimal;

    /// <summary>
    ///     Checks if a type is an enumeration.
    /// </summary>
    public static bool IsEnumeration([NotNullWhen(true)] this ITypeSymbol? symbol) =>
        (symbol?.GetEnumerationType()) is not null;

    /// <summary>
    ///     Gets the underlying type of an enum.
    /// </summary>
    public static INamedTypeSymbol? GetEnumerationType(this ITypeSymbol? symbol) =>
        (symbol as INamedTypeSymbol)?.EnumUnderlyingType;

    /// <summary>
    ///     Checks if a type is a numeric type.
    /// </summary>
    public static bool IsNumberType(this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        return symbol.SpecialType switch
        {
            SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal
                or SpecialType.System_Byte or SpecialType.System_SByte => true,
            _ => false
        };
    }

    /// <summary>
    ///     Gets the underlying type of Nullable&lt;T&gt; or returns the type itself.
    /// </summary>
    [return: NotNullIfNotNull(nameof(typeSymbol))]
    public static ITypeSymbol? GetUnderlyingNullableTypeOrSelf(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ConstructedFrom.SpecialType is SpecialType.System_Nullable_T &&
                namedTypeSymbol.TypeArguments.Length is 1)
            {
                return namedTypeSymbol.TypeArguments[0];
            }
        }

        return typeSymbol;
    }

    /// <summary>
    ///     Checks if a type is a unit test class (xUnit, NUnit, MSTest).
    /// </summary>
    public static bool IsUnitTestClass(this INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind is not TypeKind.Class)
            return false;

        // Check for test class attributes
        foreach (var attr in symbol.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            switch (name)
            {
                case null:
                    continue;
                // MSTest
                case "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute":
                // NUnit
                case "NUnit.Framework.TestFixtureAttribute":
                    return true;
            }
        }

        // xUnit doesn't require class-level attributes, check for test method attributes
        foreach (var member in symbol.GetMembers())
        {
            if (member is not IMethodSymbol method)
                continue;

            foreach (var attr in method.GetAttributes())
            {
                var name = attr.AttributeClass?.ToDisplayString();
                if (name is null)
                    continue;

                // xUnit
                if (name.StartsWith("Xunit.", StringComparison.Ordinal))
                    return true;

                // NUnit
                if (name is "NUnit.Framework.TestAttribute" or "NUnit.Framework.TestCaseAttribute")
                    return true;

                // MSTest
                if (name is "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type could potentially be made static (no instance members, no base class, no interfaces).
    /// </summary>
    public static bool IsPotentialStatic(this INamedTypeSymbol symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is not { IsAbstract: false, IsStatic: false, IsImplicitlyDeclared: false })
            return false;

        if (symbol.Interfaces.Length > 0)
            return false;

        if (symbol.BaseType is not null && symbol.BaseType.SpecialType is not SpecialType.System_Object)
            return false;

        if (symbol.IsUnitTestClass())
            return false;

        if (symbol.IsTopLevelStatement(cancellationToken))
            return false;

        foreach (var member in symbol.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
                continue;

            if (!member.IsStatic && !member.IsOperator())
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks if a type is a Span or ReadOnlySpan.
    /// </summary>
    public static bool IsSpanType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol namedType)
            return false;

        var name = namedType.OriginalDefinition?.ToDisplayString();
        return name is "System.Span<T>" or "System.ReadOnlySpan<T>";
    }

    /// <summary>
    ///     Checks if a type is a Memory or ReadOnlyMemory.
    /// </summary>
    public static bool IsMemoryType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol namedType)
            return false;

        var name = namedType.OriginalDefinition?.ToDisplayString();
        return name is "System.Memory<T>" or "System.ReadOnlyMemory<T>";
    }

    /// <summary>
    ///     Checks if a type is a Task or ValueTask (generic or non-generic).
    /// </summary>
    public static bool IsTaskType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        var name = symbol is INamedTypeSymbol namedType
            ? namedType.OriginalDefinition?.ToDisplayString()
            : symbol.ToDisplayString();

        return name is "System.Threading.Tasks.Task"
            or "System.Threading.Tasks.Task<TResult>"
            or "System.Threading.Tasks.ValueTask"
            or "System.Threading.Tasks.ValueTask<TResult>";
    }

    /// <summary>
    ///     Checks if a type is IEnumerable or IEnumerable&lt;T&gt;.
    /// </summary>
    public static bool IsEnumerableType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.SpecialType is SpecialType.System_Collections_IEnumerable)
            return true;

        if (symbol is not INamedTypeSymbol namedType)
            return false;

        return namedType.OriginalDefinition?.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T;
    }

    /// <summary>
    ///     Gets the element type if this is an array, Span, Memory, or IEnumerable&lt;T&gt;.
    /// </summary>
    public static ITypeSymbol? GetElementType(this ITypeSymbol? symbol)
    {
        switch (symbol)
        {
            case null:
                return null;
            // Array
            case IArrayTypeSymbol arrayType:
                return arrayType.ElementType;
        }

        if (symbol is not INamedTypeSymbol namedType)
            return null;

        // Span<T>, ReadOnlySpan<T>, Memory<T>, ReadOnlyMemory<T>, IEnumerable<T>
        if (namedType.TypeArguments.Length is 1)
        {
            var name = namedType.OriginalDefinition?.ToDisplayString();
            if (name is "System.Span<T>" or "System.ReadOnlySpan<T>"
                or "System.Memory<T>" or "System.ReadOnlyMemory<T>"
                or "System.Collections.Generic.IEnumerable<T>")
            {
                return namedType.TypeArguments[0];
            }
        }

        return null;
    }
}
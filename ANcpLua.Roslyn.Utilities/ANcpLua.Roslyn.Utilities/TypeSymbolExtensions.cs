using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="ITypeSymbol" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides extension methods for working with type symbols in Roslyn analyzers
///         and source generators. It includes functionality for:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Type inheritance and interface implementation checks</description>
///         </item>
///         <item>
///             <description>Special type detection (numeric, span, memory, task, enumerable)</description>
///         </item>
///         <item>
///             <description>Nullable type unwrapping</description>
///         </item>
///         <item>
///             <description>Unit test class detection</description>
///         </item>
///         <item>
///             <description>Static class candidacy analysis</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="SymbolExtensions" />
/// <seealso cref="MethodSymbolExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TypeSymbolExtensions
{
    /// <summary>
    ///     Gets all interfaces implemented by the type, including the type itself if it is an interface.
    /// </summary>
    /// <param name="type">The type symbol to get interfaces from.</param>
    /// <returns>
    ///     An enumerable of all interfaces. If <paramref name="type" /> is an interface,
    ///     it is included in the result along with all its inherited interfaces.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="ITypeSymbol.AllInterfaces" />, which only returns inherited interfaces,
    ///     this method includes the type itself when querying an interface type.
    /// </remarks>
    /// <seealso cref="Implements" />
    /// <seealso cref="IsOrImplements" />
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
    ///     Determines whether a type inherits from a specified base type.
    /// </summary>
    /// <param name="classSymbol">The type symbol to check.</param>
    /// <param name="baseClassType">The potential base type to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="classSymbol" /> inherits from <paramref name="baseClassType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method walks the inheritance chain using <see cref="SymbolEqualityComparer.Default" />
    ///         for comparison. It does not consider the type itself as inheriting from itself.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Returns <c>false</c> if <paramref name="classSymbol" /> equals
    ///                 <paramref name="baseClassType" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Walks the entire base type chain until <see cref="object" /> or <c>null</c></description>
    ///         </item>
    ///         <item>
    ///             <description>Use <see cref="IsOrInheritsFrom" /> if you need to include the type itself in the check</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Check if a type inherits from a specific base class
    /// INamedTypeSymbol? exceptionType = compilation.GetTypeByMetadataName("System.Exception");
    /// if (typeSymbol.InheritsFrom(exceptionType))
    /// {
    ///     // Type is a custom exception (but not System.Exception itself)
    /// }
    ///
    /// // Walk inheritance chain manually for analysis
    /// ITypeSymbol? current = typeSymbol.BaseType;
    /// while (current is not null)
    /// {
    ///     // Analyze each base type
    ///     current = current.BaseType;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsOrInheritsFrom" />
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
    ///     Determines whether a type implements a specified interface.
    /// </summary>
    /// <param name="classSymbol">The type symbol to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="classSymbol" /> implements <paramref name="interfaceType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method uses <see cref="SymbolEqualityComparer.Default" /> for comparison
    ///     and checks against <see cref="ITypeSymbol.AllInterfaces" />.
    /// </remarks>
    /// <seealso cref="IsOrImplements" />
    /// <seealso cref="GetAllInterfacesIncludingThis" />
    public static bool Implements(this ITypeSymbol classSymbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in classSymbol.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether a type is or implements a specified interface.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is or implements <paramref name="interfaceType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Unlike <see cref="Implements" />, this method returns <c>true</c> if the symbol itself
    ///     is the interface being checked.
    /// </remarks>
    /// <seealso cref="Implements" />
    /// <seealso cref="GetAllInterfacesIncludingThis" />
    public static bool IsOrImplements(this ITypeSymbol symbol, ITypeSymbol? interfaceType)
    {
        if (interfaceType is null)
            return false;

        foreach (var iface in symbol.GetAllInterfacesIncludingThis())
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether a type is or inherits from a specified type.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="expectedType">The expected type or base type to check against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> equals or inherits from <paramref name="expectedType" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method first checks for equality, then checks inheritance if the expected type is not sealed.
    /// </remarks>
    /// <seealso cref="InheritsFrom" />
    public static bool IsOrInheritsFrom(this ITypeSymbol symbol, ITypeSymbol? expectedType)
    {
        if (expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(symbol, expectedType) ||
               !expectedType.IsSealed && symbol.InheritsFrom(expectedType);
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="object" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="object" />; otherwise, <c>false</c>.</returns>
    public static bool IsObject(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Object;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="string" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="string" />; otherwise, <c>false</c>.</returns>
    public static bool IsString(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_String;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="char" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="char" />; otherwise, <c>false</c>.</returns>
    public static bool IsChar(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Char;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="int" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="int" />; otherwise, <c>false</c>.</returns>
    public static bool IsInt32(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int32;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="long" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="long" />; otherwise, <c>false</c>.</returns>
    public static bool IsInt64(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int64;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="bool" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="bool" />; otherwise, <c>false</c>.</returns>
    public static bool IsBoolean(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Boolean;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="DateTime" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="DateTime" />; otherwise, <c>false</c>.</returns>
    public static bool IsDateTime(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_DateTime;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="byte" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="byte" />; otherwise, <c>false</c>.</returns>
    public static bool IsByte(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Byte;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="sbyte" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="sbyte" />; otherwise, <c>false</c>.</returns>
    public static bool IsSByte(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_SByte;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="short" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="short" />; otherwise, <c>false</c>.</returns>
    public static bool IsInt16(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Int16;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="ushort" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="ushort" />; otherwise, <c>false</c>.</returns>
    public static bool IsUInt16(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt16;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="uint" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="uint" />; otherwise, <c>false</c>.</returns>
    public static bool IsUInt32(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt32;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="ulong" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="ulong" />; otherwise, <c>false</c>.</returns>
    public static bool IsUInt64(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_UInt64;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="float" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="float" />; otherwise, <c>false</c>.</returns>
    public static bool IsSingle(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Single;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="double" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="double" />; otherwise, <c>false</c>.</returns>
    public static bool IsDouble(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Double;

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="decimal" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is <see cref="decimal" />; otherwise, <c>false</c>.</returns>
    public static bool IsDecimal(this ITypeSymbol? symbol) => symbol?.SpecialType is SpecialType.System_Decimal;

    /// <summary>
    ///     Determines whether the type symbol represents an enumeration type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns><c>true</c> if <paramref name="symbol" /> is an enumeration type; otherwise, <c>false</c>.</returns>
    /// <seealso cref="GetEnumerationType" />
    public static bool IsEnumeration([NotNullWhen(true)] this ITypeSymbol? symbol) => symbol?.GetEnumerationType() is not null;

    /// <summary>
    ///     Gets the underlying type of an enumeration.
    /// </summary>
    /// <param name="symbol">The type symbol to get the underlying type from, or <c>null</c>.</param>
    /// <returns>
    ///     The underlying type of the enumeration (e.g., <see cref="int" />), or <c>null</c>
    ///     if <paramref name="symbol" /> is not an enumeration.
    /// </returns>
    /// <seealso cref="IsEnumeration" />
    public static INamedTypeSymbol? GetEnumerationType(this ITypeSymbol? symbol) => (symbol as INamedTypeSymbol)?.EnumUnderlyingType;

    /// <summary>
    ///     Determines whether the type symbol represents a numeric type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a numeric type; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>Numeric types include:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Signed integers: <see cref="sbyte" />, <see cref="short" />, <see cref="int" />,
    ///                 <see cref="long" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Unsigned integers: <see cref="byte" />, <see cref="ushort" />, <see cref="uint" />,
    ///                 <see cref="ulong" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Floating-point: <see cref="float" />, <see cref="double" />, <see cref="decimal" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
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
    ///     Gets the underlying type of a <see cref="Nullable{T}" /> or returns the type itself.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to unwrap, or <c>null</c>.</param>
    /// <returns>
    ///     The underlying type if <paramref name="typeSymbol" /> is <see cref="Nullable{T}" />;
    ///     otherwise, <paramref name="typeSymbol" /> itself.
    /// </returns>
    /// <remarks>
    ///     This method is useful for analyzing nullable value types without special-casing nullability.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(typeSymbol))]
    public static ITypeSymbol? GetUnderlyingNullableTypeOrSelf(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            if (namedTypeSymbol.ConstructedFrom.SpecialType is SpecialType.System_Nullable_T &&
                namedTypeSymbol.TypeArguments.Length is 1)
                return namedTypeSymbol.TypeArguments[0];

        return typeSymbol;
    }

    /// <summary>
    ///     Gets the underlying type if the symbol represents a nullable type (either <see cref="Nullable{T}" /> or a nullable
    ///     reference type).
    /// </summary>
    /// <param name="typeSymbol">The type symbol to unwrap.</param>
    /// <returns>The underlying type symbol.</returns>
    public static ITypeSymbol UnwrapNullable(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];

        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        return typeSymbol;
    }

    /// <summary>
    ///     Determines whether the type symbol represents a unit test class.
    /// </summary>
    /// <param name="symbol">The named type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a unit test class; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method detects test classes from the following frameworks:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <b>MSTest:</b> Classes with <c>[TestClass]</c> attribute or methods with <c>[TestMethod]</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>NUnit:</b> Classes with <c>[TestFixture]</c> attribute or methods with <c>[Test]</c>/
    ///                 <c>[TestCase]</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <b>xUnit:</b> Classes with methods having <c>[Fact]</c>, <c>[Theory]</c>, or other Xunit.*
    ///                 attributes
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
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

                switch (name)
                {
                    // NUnit
                    case "NUnit.Framework.TestAttribute" or "NUnit.Framework.TestCaseAttribute":
                    // MSTest
                    case "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute":
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a type could potentially be made static.
    /// </summary>
    /// <param name="symbol">The named type symbol to check.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> could be made static; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>A type is considered a candidate for being static if:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>It is not abstract or already static</description>
    ///         </item>
    ///         <item>
    ///             <description>It is not implicitly declared</description>
    ///         </item>
    ///         <item>
    ///             <description>It has no interfaces</description>
    ///         </item>
    ///         <item>
    ///             <description>It has no base class other than <see cref="object" /></description>
    ///         </item>
    ///         <item>
    ///             <description>It is not a unit test class</description>
    ///         </item>
    ///         <item>
    ///             <description>It is not a top-level statement container</description>
    ///         </item>
    ///         <item>
    ///             <description>All non-implicitly-declared members are static or operators</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsUnitTestClass" />
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
    ///     Determines whether the type symbol represents <see cref="Span{T}" /> or <see cref="ReadOnlySpan{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a span type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsMemoryType" />
    /// <seealso cref="GetElementType" />
    public static bool IsSpanType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol namedType)
            return false;

        var name = namedType.OriginalDefinition.ToDisplayString();
        return name is "System.Span<T>" or "System.ReadOnlySpan<T>";
    }

    /// <summary>
    ///     Determines whether the type symbol represents <see cref="Memory{T}" /> or <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a memory type; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="IsSpanType" />
    /// <seealso cref="GetElementType" />
    public static bool IsMemoryType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol namedType)
            return false;

        var name = namedType.OriginalDefinition.ToDisplayString();
        return name is "System.Memory<T>" or "System.ReadOnlyMemory<T>";
    }

    /// <summary>
    ///     Determines whether the type symbol represents a task type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is a task type; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>Task types include:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Threading.Tasks.Task" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Threading.Tasks.Task{TResult}" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Threading.Tasks.ValueTask" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Threading.Tasks.ValueTask{TResult}" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static bool IsTaskType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        var name = symbol is INamedTypeSymbol namedType
            ? namedType.OriginalDefinition.ToDisplayString()
            : symbol.ToDisplayString();

        return name is "System.Threading.Tasks.Task"
            or "System.Threading.Tasks.Task<TResult>"
            or "System.Threading.Tasks.ValueTask"
            or "System.Threading.Tasks.ValueTask<TResult>";
    }

    /// <summary>
    ///     Determines whether the type symbol represents an enumerable type.
    /// </summary>
    /// <param name="symbol">The type symbol to check, or <c>null</c>.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="symbol" /> is <see cref="System.Collections.IEnumerable" />
    ///     or <see cref="System.Collections.Generic.IEnumerable{T}" />; otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="GetElementType" />
    public static bool IsEnumerableType([NotNullWhen(true)] this ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.SpecialType is SpecialType.System_Collections_IEnumerable)
            return true;

        if (symbol is not INamedTypeSymbol namedType)
            return false;

        return namedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T;
    }

    /// <summary>
    ///     Gets the element type of a collection or span-like type.
    /// </summary>
    /// <param name="symbol">The type symbol to get the element type from, or <c>null</c>.</param>
    /// <returns>
    ///     The element type if <paramref name="symbol" /> is an array, span, memory, or generic enumerable;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method extracts element types from:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Arrays (e.g., <c>int[]</c> returns <c>int</c>)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Span{T}" /> and <see cref="ReadOnlySpan{T}" /></description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Memory{T}" /> and <see cref="ReadOnlyMemory{T}" /></description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="System.Collections.Generic.IEnumerable{T}" />
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="IsSpanType" />
    /// <seealso cref="IsMemoryType" />
    /// <seealso cref="IsEnumerableType" />
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
            var name = namedType.OriginalDefinition.ToDisplayString();
            if (name is "System.Span<T>" or "System.ReadOnlySpan<T>"
                or "System.Memory<T>" or "System.ReadOnlyMemory<T>"
                or "System.Collections.Generic.IEnumerable<T>")
                return namedType.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    ///     Determines whether the specified type has a parameterless <c>Dispose()</c> method that returns <c>void</c>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a <c>void Dispose()</c> method with no parameters; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="HasDisposeAsyncMethod" />
    public static bool HasDisposeMethod(this ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("Dispose"))
            if (member is IMethodSymbol { Parameters.IsEmpty: true, ReturnsVoid: true })
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type has a parameterless <c>DisposeAsync()</c> method.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a <c>DisposeAsync()</c> method with no parameters; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <seealso cref="HasDisposeMethod" />
    public static bool HasDisposeAsyncMethod(this ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("DisposeAsync"))
            if (member is IMethodSymbol { Parameters.IsEmpty: true })
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether the specified type has a <c>Count</c> or <c>Length</c> property with a getter.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="type" /> has a readable <c>Count</c> or <c>Length</c> property;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public static bool HasCountProperty(this ITypeSymbol type)
    {
        foreach (var member in type.GetAllMembers("Count"))
            if (member is IPropertySymbol { GetMethod: not null })
                return true;

        foreach (var member in type.GetAllMembers("Length"))
            if (member is IPropertySymbol { GetMethod: not null })
                return true;

        return false;
    }
}
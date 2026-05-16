using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Fluent matcher for <see cref="INamedTypeSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides type-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by type kind (class, struct, interface, enum, record).</description>
///         </item>
///         <item>
///             <description>Match by inheritance hierarchy.</description>
///         </item>
///         <item>
///             <description>Match by interface implementations.</description>
///         </item>
///         <item>
///             <description>Match by nesting level and member presence.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Type()" />
/// <seealso cref="Match.Type(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class TypeMatcher : SymbolMatcherBase<TypeMatcher, INamedTypeSymbol>
{
    /// <summary>
    ///     Matches class types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Struct" />
    /// <seealso cref="Interface" />
    /// <seealso cref="Enum" />
    /// <seealso cref="Record" />
    public TypeMatcher Class()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Class);
    }

    /// <summary>
    ///     Matches struct types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Record" />
    public TypeMatcher Struct()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Struct);
    }

    /// <summary>
    ///     Matches interface types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    public TypeMatcher Interface()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Interface);
    }

    /// <summary>
    ///     Matches enum types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Struct" />
    public TypeMatcher Enum()
    {
        return AddPredicate(static t => t.TypeKind == TypeKind.Enum);
    }

    /// <summary>
    ///     Matches record types (both class and struct records).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    /// <seealso cref="Struct" />
    public TypeMatcher Record()
    {
        return AddPredicate(static t => t.IsRecord);
    }

    /// <summary>
    ///     Matches generic types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotGeneric" />
    public TypeMatcher Generic()
    {
        return AddPredicate(static t => t.IsGenericType);
    }

    /// <summary>
    ///     Matches non-generic types.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Generic" />
    public TypeMatcher NotGeneric()
    {
        return AddPredicate(static t => !t.IsGenericType);
    }

    /// <summary>
    ///     Matches types that inherit from a base type with the specified name.
    /// </summary>
    /// <param name="baseTypeName">
    ///     The name or fully qualified name of the base type to check for in the inheritance hierarchy.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     Traverses the entire inheritance hierarchy, not just the immediate base type.
    /// </remarks>
    /// <seealso cref="Implements" />
    public TypeMatcher InheritsFrom(string baseTypeName)
    {
        return AddPredicate(t => t.InheritsFromName(baseTypeName));
    }

    /// <summary>
    ///     Matches types that implement an interface with the specified name.
    /// </summary>
    /// <param name="interfaceName">
    ///     The name or fully qualified name of the interface to check for.
    /// </param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <remarks>
    ///     Checks all interfaces including inherited ones via <see cref="INamedTypeSymbol" />.<c>AllInterfaces</c>.
    /// </remarks>
    /// <seealso cref="InheritsFrom" />
    /// <seealso cref="Disposable" />
    public TypeMatcher Implements(string interfaceName)
    {
        return AddPredicate(t => t.ImplementsInterfaceName(interfaceName));
    }

    /// <summary>
    ///     Matches types that implement <see cref="System.IDisposable" />.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Implements" />
    public TypeMatcher Disposable()
    {
        return Implements("System.IDisposable");
    }

    /// <summary>
    ///     Matches nested types (types declared within another type).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="TopLevel" />
    public TypeMatcher Nested()
    {
        return AddPredicate(static t => t.ContainingType is not null);
    }

    /// <summary>
    ///     Matches top-level types (types not declared within another type).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Nested" />
    public TypeMatcher TopLevel()
    {
        return AddPredicate(static t => t.ContainingType is null);
    }

    /// <summary>
    ///     Matches static classes.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Class" />
    public TypeMatcher StaticClass()
    {
        return AddPredicate(static t => t.IsStatic && t.TypeKind == TypeKind.Class);
    }

    /// <summary>
    ///     Matches types that have a member with the specified name.
    /// </summary>
    /// <param name="name">The name of the member to look for.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="HasParameterlessConstructor" />
    public TypeMatcher HasMember(string name)
    {
        return AddPredicate(t => t.GetMembers(name).Length > 0);
    }

    /// <summary>
    ///     Matches types that have a parameterless constructor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="HasMember" />
    public TypeMatcher HasParameterlessConstructor()
    {
        return AddPredicate(HasParameterlessCtor);
    }

    private static bool HasParameterlessCtor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
            if (ctor.Parameters.Length is 0)
                return true;

        return false;
    }
}

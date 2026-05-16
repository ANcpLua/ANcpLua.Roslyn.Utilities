using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Fluent matcher for <see cref="IPropertySymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides property-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by accessor presence (getter, setter, init).</description>
///         </item>
///         <item>
///             <description>Match by property characteristics (indexer, required, read-only).</description>
///         </item>
///         <item>
///             <description>Match by property type.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Property()" />
/// <seealso cref="Match.Property(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class PropertyMatcher : SymbolMatcherBase<PropertyMatcher, IPropertySymbol>
{
    /// <summary>
    ///     Matches properties that have a getter accessor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithSetter" />
    /// <seealso cref="ReadOnly" />
    public PropertyMatcher WithGetter()
    {
        return AddPredicate(static p => p.GetMethod is not null);
    }

    /// <summary>
    ///     Matches properties that have a setter accessor.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithGetter" />
    /// <seealso cref="WithInitSetter" />
    public PropertyMatcher WithSetter()
    {
        return AddPredicate(static p => p.SetMethod is not null);
    }

    /// <summary>
    ///     Matches properties that have an init-only setter.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithSetter" />
    public PropertyMatcher WithInitSetter()
    {
        return AddPredicate(static p => p.SetMethod?.IsInitOnly == true);
    }

    /// <summary>
    ///     Matches read-only properties (properties with a getter but no setter).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="WithGetter" />
    /// <seealso cref="WithSetter" />
    public PropertyMatcher ReadOnly()
    {
        return AddPredicate(static p => p.GetMethod is not null && p.SetMethod is null);
    }

    /// <summary>
    ///     Matches indexer properties.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher Indexer()
    {
        return AddPredicate(static p => p.IsIndexer);
    }

    /// <summary>
    ///     Matches required properties (C# 11+).
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher Required()
    {
        return AddPredicate(static p => p.IsRequired);
    }

    /// <summary>
    ///     Matches properties with the specified type name.
    /// </summary>
    /// <param name="typeName">The name of the property type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public PropertyMatcher OfType(string typeName)
    {
        return AddPredicate(p => p.Type.Name == typeName);
    }
}

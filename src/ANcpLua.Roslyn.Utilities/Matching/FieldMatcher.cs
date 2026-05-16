using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Matching;

/// <summary>
///     Fluent matcher for <see cref="IFieldSymbol" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         Provides field-specific matching predicates in addition to the common
///         symbol matching functionality from <see cref="SymbolMatcherBase{TSelf, TSymbol}" />.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Match by field modifiers (const, readonly, volatile).</description>
///         </item>
///         <item>
///             <description>Match by field type.</description>
///         </item>
///         <item>
///             <description>Match by backing field status.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="Match.Field()" />
/// <seealso cref="Match.Field(string)" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class FieldMatcher : SymbolMatcherBase<FieldMatcher, IFieldSymbol>
{
    /// <summary>
    ///     Matches const fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="ReadOnly" />
    public FieldMatcher Const()
    {
        return AddPredicate(static f => f.IsConst);
    }

    /// <summary>
    ///     Matches readonly fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="Const" />
    public FieldMatcher ReadOnly()
    {
        return AddPredicate(static f => f.IsReadOnly);
    }

    /// <summary>
    ///     Matches volatile fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public FieldMatcher Volatile()
    {
        return AddPredicate(static f => f.IsVolatile);
    }

    /// <summary>
    ///     Matches fields with the specified type name.
    /// </summary>
    /// <param name="typeName">The name of the field type to match.</param>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    public FieldMatcher OfType(string typeName)
    {
        return AddPredicate(f => f.Type.Name == typeName);
    }

    /// <summary>
    ///     Matches compiler-generated backing fields for auto-properties.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="NotBackingField" />
    public FieldMatcher BackingField()
    {
        return AddPredicate(static f => f.AssociatedSymbol is not null);
    }

    /// <summary>
    ///     Matches fields that are not compiler-generated backing fields.
    /// </summary>
    /// <returns>The current matcher instance for fluent chaining.</returns>
    /// <seealso cref="BackingField" />
    public FieldMatcher NotBackingField()
    {
        return AddPredicate(static f => f.AssociatedSymbol is null);
    }
}

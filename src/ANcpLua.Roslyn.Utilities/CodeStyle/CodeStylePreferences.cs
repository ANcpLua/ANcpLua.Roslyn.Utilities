namespace ANcpLua.Roslyn.Utilities.CodeStyle;

/// <summary>
///     Specifies the preference for using expression-bodied members.
/// </summary>
/// <remarks>
///     Maps to the <c>csharp_style_expression_bodied_*</c> family of <c>.editorconfig</c> options
///     (e.g., <c>csharp_style_expression_bodied_methods</c>, <c>csharp_style_expression_bodied_properties</c>).
/// </remarks>
/// <seealso cref="NamespaceDeclarationPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum ExpressionBodyPreference
{
    /// <summary>
    ///     Never use expression-bodied members; always use block bodies.
    /// </summary>
    Never,

    /// <summary>
    ///     Use expression-bodied members whenever possible.
    /// </summary>
    WhenPossible,

    /// <summary>
    ///     Use expression-bodied members only when the body fits on a single line.
    /// </summary>
    WhenOnSingleLine
}

/// <summary>
///     Specifies the preference for explicit accessibility modifiers on type and member declarations.
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_style_require_accessibility_modifiers</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum AccessibilityModifiersRequired
{
    /// <summary>
    ///     Never require accessibility modifiers.
    /// </summary>
    Never,

    /// <summary>
    ///     Always require explicit accessibility modifiers on all declarations.
    /// </summary>
    Always,

    /// <summary>
    ///     Require accessibility modifiers on all members except interface members.
    /// </summary>
    ForNonInterfaceMembers,

    /// <summary>
    ///     Omit the modifier only when it matches the default accessibility for that declaration kind.
    /// </summary>
    OmitIfDefault
}

/// <summary>
///     Specifies the preference for namespace declaration style.
/// </summary>
/// <remarks>
///     Maps to the <c>csharp_style_namespace_declarations</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum NamespaceDeclarationPreference
{
    /// <summary>
    ///     Use block-scoped namespace declarations: <c>namespace Foo { }</c>.
    /// </summary>
    BlockScoped,

    /// <summary>
    ///     Use file-scoped namespace declarations: <c>namespace Foo;</c>.
    /// </summary>
    FileScoped
}

/// <summary>
///     Specifies the preference for using collection expressions (<c>[..]</c> syntax).
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_style_prefer_collection_expression</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum CollectionExpressionPreference
{
    /// <summary>
    ///     Never suggest collection expressions.
    /// </summary>
    Never,

    /// <summary>
    ///     Suggest collection expressions when the target type is loosely compatible
    ///     (e.g., <c>IEnumerable&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>).
    /// </summary>
    WhenTypesLooselyMatch,

    /// <summary>
    ///     Suggest collection expressions only when the target type exactly matches
    ///     (e.g., <c>List&lt;T&gt;</c>, <c>int[]</c>).
    /// </summary>
    WhenTypesExactlyMatch
}

/// <summary>
///     Specifies the preference for adding parentheses to clarify operator precedence.
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_style_parentheses_in_*_operators</c> family of <c>.editorconfig</c> options.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum ParenthesesPreference
{
    /// <summary>
    ///     Always add parentheses to clarify precedence, even when not strictly necessary.
    /// </summary>
    AlwaysForClarity,

    /// <summary>
    ///     Never add unnecessary parentheses; rely on operator precedence rules.
    /// </summary>
    NeverIfUnnecessary
}

/// <summary>
///     Specifies which unused parameters should be flagged by analyzers.
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_code_quality_unused_parameters</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="UnusedValuePreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum UnusedParametersPreference
{
    /// <summary>
    ///     Flag unused parameters in all methods, including public API.
    /// </summary>
    AllMethods,

    /// <summary>
    ///     Only flag unused parameters in non-public methods.
    /// </summary>
    NonPublicMethods
}

/// <summary>
///     Specifies how to handle expression results that are not used.
/// </summary>
/// <remarks>
///     Maps to the <c>csharp_style_unused_value_expression_statement_preference</c> and
///     <c>csharp_style_unused_value_assignment_preference</c> <c>.editorconfig</c> options.
/// </remarks>
/// <seealso cref="UnusedParametersPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum UnusedValuePreference
{
    /// <summary>
    ///     Assign unused values to a discard variable (<c>_ = expression;</c>).
    /// </summary>
    DiscardVariable,

    /// <summary>
    ///     Assign unused values to an unused local variable.
    /// </summary>
    UnusedLocalVariable
}

/// <summary>
///     Specifies where binary operators should be placed when an expression wraps to a new line.
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_style_operator_placement_when_wrapping</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum OperatorPlacementWhenWrappingPreference
{
    /// <summary>
    ///     Place the operator at the beginning of the new line.
    /// </summary>
    BeginningOfLine,

    /// <summary>
    ///     Place the operator at the end of the previous line.
    /// </summary>
    EndOfLine
}

/// <summary>
///     Specifies the preference for explicit casts in <c>foreach</c> iteration variables.
/// </summary>
/// <remarks>
///     Maps to the <c>dotnet_style_prefer_foreach_explicit_cast_in_source</c> <c>.editorconfig</c> option.
/// </remarks>
/// <seealso cref="ExpressionBodyPreference" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum ForEachExplicitCastInSourcePreference
{
    /// <summary>
    ///     Always add an explicit cast in the <c>foreach</c> variable declaration.
    /// </summary>
    Always,

    /// <summary>
    ///     Add an explicit cast only when the collection is strongly typed and the cast is safe.
    /// </summary>
    WhenStronglyTyped
}
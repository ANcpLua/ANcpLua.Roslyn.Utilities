using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
/// Composable pattern for matching symbols. Like regex, but for Roslyn symbols.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SymbolPattern{T}"/> provides a declarative, composable approach to matching
/// Roslyn symbols based on their characteristics. Patterns can be combined using logical
/// operators to create complex matching criteria.
/// </para>
/// <list type="bullet">
///   <item><description>Use <see cref="And"/> or the <c>&amp;</c> operator to require both patterns to match.</description></item>
///   <item><description>Use <see cref="Or"/> or the <c>|</c> operator to match if either pattern matches.</description></item>
///   <item><description>Use <see cref="Not"/> or the <c>!</c> operator to invert a pattern.</description></item>
/// </list>
/// </remarks>
/// <typeparam name="T">The type of symbol this pattern matches, constrained to <see cref="ISymbol"/>.</typeparam>
/// <seealso cref="SymbolPattern"/>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
abstract class SymbolPattern<T> where T : ISymbol
{
    /// <summary>
    /// Determines whether the specified symbol matches this pattern.
    /// </summary>
    /// <param name="symbol">The symbol to test against this pattern.</param>
    /// <returns><c>true</c> if <paramref name="symbol"/> matches this pattern; otherwise, <c>false</c>.</returns>
    public abstract bool Matches(T symbol);

    /// <summary>
    /// Creates a new pattern that matches only when both this pattern and the specified pattern match.
    /// </summary>
    /// <param name="other">The pattern to combine with this pattern using logical AND.</param>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when both patterns match.</returns>
    /// <seealso cref="op_BitwiseAnd"/>
    public SymbolPattern<T> And(SymbolPattern<T> other) => new AndPattern<T>(this, other);

    /// <summary>
    /// Creates a new pattern that matches when either this pattern or the specified pattern matches.
    /// </summary>
    /// <param name="other">The pattern to combine with this pattern using logical OR.</param>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when either pattern matches.</returns>
    /// <seealso cref="op_BitwiseOr"/>
    public SymbolPattern<T> Or(SymbolPattern<T> other) => new OrPattern<T>(this, other);

    /// <summary>
    /// Creates a new pattern that matches when this pattern does not match.
    /// </summary>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when this pattern does not match.</returns>
    /// <seealso cref="op_LogicalNot"/>
    public SymbolPattern<T> Not() => new NotPattern<T>(this);

    /// <summary>
    /// Combines two patterns using logical AND. Equivalent to calling <see cref="And"/>.
    /// </summary>
    /// <param name="left">The first pattern to combine.</param>
    /// <param name="right">The second pattern to combine.</param>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when both patterns match.</returns>
    public static SymbolPattern<T> operator &(SymbolPattern<T> left, SymbolPattern<T> right) => left.And(right);

    /// <summary>
    /// Combines two patterns using logical OR. Equivalent to calling <see cref="Or"/>.
    /// </summary>
    /// <param name="left">The first pattern to combine.</param>
    /// <param name="right">The second pattern to combine.</param>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when either pattern matches.</returns>
    public static SymbolPattern<T> operator |(SymbolPattern<T> left, SymbolPattern<T> right) => left.Or(right);

    /// <summary>
    /// Negates a pattern. Equivalent to calling <see cref="Not"/>.
    /// </summary>
    /// <param name="pattern">The pattern to negate.</param>
    /// <returns>A new <see cref="SymbolPattern{T}"/> that matches when the original pattern does not match.</returns>
    public static SymbolPattern<T> operator !(SymbolPattern<T> pattern) => pattern.Not();
}

internal sealed class AndPattern<T>(SymbolPattern<T> left, SymbolPattern<T> right) : SymbolPattern<T> where T : ISymbol
{
    public override bool Matches(T symbol) => left.Matches(symbol) && right.Matches(symbol);
}

internal sealed class OrPattern<T>(SymbolPattern<T> left, SymbolPattern<T> right) : SymbolPattern<T> where T : ISymbol
{
    public override bool Matches(T symbol) => left.Matches(symbol) || right.Matches(symbol);
}

internal sealed class NotPattern<T>(SymbolPattern<T> inner) : SymbolPattern<T> where T : ISymbol
{
    public override bool Matches(T symbol) => !inner.Matches(symbol);
}

internal sealed class PredicatePattern<T>(Func<T, bool> predicate) : SymbolPattern<T> where T : ISymbol
{
    public override bool Matches(T symbol) => predicate(symbol);
}

internal sealed class AlwaysTruePattern<T> : SymbolPattern<T> where T : ISymbol
{
    public static AlwaysTruePattern<T> Instance { get; } = new();
    public override bool Matches(T symbol) => true;
}

/// <summary>
/// Entry point for creating symbol patterns.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides factory methods for creating pattern builders for different
/// symbol types. Each builder provides a fluent API for specifying matching criteria.
/// </para>
/// <list type="bullet">
///   <item><description>Use <see cref="Method"/> to create patterns for <see cref="IMethodSymbol"/>.</description></item>
///   <item><description>Use <see cref="Type"/> to create patterns for <see cref="INamedTypeSymbol"/>.</description></item>
///   <item><description>Use <see cref="Parameter"/> to create patterns for <see cref="IParameterSymbol"/>.</description></item>
///   <item><description>Use <see cref="Property"/> to create patterns for <see cref="IPropertySymbol"/>.</description></item>
///   <item><description>Use <see cref="Field"/> to create patterns for <see cref="IFieldSymbol"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SymbolPattern{T}"/>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class SymbolPattern
{
    /// <summary>
    /// Creates a new pattern builder for matching method symbols.
    /// </summary>
    /// <returns>A new <see cref="MethodPatternBuilder"/> for configuring method matching criteria.</returns>
    public static MethodPatternBuilder Method() => new();

    /// <summary>
    /// Creates a new pattern builder for matching type symbols.
    /// </summary>
    /// <returns>A new <see cref="TypePatternBuilder"/> for configuring type matching criteria.</returns>
    public static TypePatternBuilder Type() => new();

    /// <summary>
    /// Creates a new pattern builder for matching parameter symbols.
    /// </summary>
    /// <returns>A new <see cref="ParameterPatternBuilder"/> for configuring parameter matching criteria.</returns>
    public static ParameterPatternBuilder Parameter() => new();

    /// <summary>
    /// Creates a new pattern builder for matching property symbols.
    /// </summary>
    /// <returns>A new <see cref="PropertyPatternBuilder"/> for configuring property matching criteria.</returns>
    public static PropertyPatternBuilder Property() => new();

    /// <summary>
    /// Creates a new pattern builder for matching field symbols.
    /// </summary>
    /// <returns>A new <see cref="FieldPatternBuilder"/> for configuring field matching criteria.</returns>
    public static FieldPatternBuilder Field() => new();

    /// <summary>
    /// Creates a pattern that matches if any of the specified patterns match.
    /// </summary>
    /// <typeparam name="T">The type of symbol the patterns match, constrained to <see cref="ISymbol"/>.</typeparam>
    /// <param name="patterns">The patterns to combine using logical OR.</param>
    /// <returns>
    /// A <see cref="SymbolPattern{T}"/> that matches if any of the specified patterns match.
    /// If <paramref name="patterns"/> is empty, returns a pattern that always matches.
    /// </returns>
    /// <seealso cref="All{T}"/>
    public static SymbolPattern<T> Any<T>(params SymbolPattern<T>[] patterns) where T : ISymbol =>
        patterns.Length == 0
            ? AlwaysTruePattern<T>.Instance
            : patterns.Aggregate((a, b) => a.Or(b));

    /// <summary>
    /// Creates a pattern that matches only if all of the specified patterns match.
    /// </summary>
    /// <typeparam name="T">The type of symbol the patterns match, constrained to <see cref="ISymbol"/>.</typeparam>
    /// <param name="patterns">The patterns to combine using logical AND.</param>
    /// <returns>
    /// A <see cref="SymbolPattern{T}"/> that matches only if all specified patterns match.
    /// If <paramref name="patterns"/> is empty, returns a pattern that always matches.
    /// </returns>
    /// <seealso cref="Any{T}"/>
    public static SymbolPattern<T> All<T>(params SymbolPattern<T>[] patterns) where T : ISymbol =>
        patterns.Length == 0
            ? AlwaysTruePattern<T>.Instance
            : patterns.Aggregate((a, b) => a.And(b));

    /// <summary>
    /// Creates a pattern from a custom predicate function.
    /// </summary>
    /// <typeparam name="T">The type of symbol the pattern matches, constrained to <see cref="ISymbol"/>.</typeparam>
    /// <param name="predicate">A function that returns <c>true</c> if a symbol matches the desired criteria.</param>
    /// <returns>A <see cref="SymbolPattern{T}"/> that uses the specified predicate for matching.</returns>
    public static SymbolPattern<T> Where<T>(Func<T, bool> predicate) where T : ISymbol =>
        new PredicatePattern<T>(predicate);
}

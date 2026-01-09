using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Patterns;

/// <summary>
/// Composable pattern for matching symbols. Like regex, but for Roslyn symbols.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
abstract class SymbolPattern<T> where T : ISymbol
{
    public abstract bool Matches(T symbol);

    public SymbolPattern<T> And(SymbolPattern<T> other) => new AndPattern<T>(this, other);
    public SymbolPattern<T> Or(SymbolPattern<T> other) => new OrPattern<T>(this, other);
    public SymbolPattern<T> Not() => new NotPattern<T>(this);

    public static SymbolPattern<T> operator &(SymbolPattern<T> left, SymbolPattern<T> right) => left.And(right);
    public static SymbolPattern<T> operator |(SymbolPattern<T> left, SymbolPattern<T> right) => left.Or(right);
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
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class SymbolPattern
{
    public static MethodPatternBuilder Method() => new();
    public static TypePatternBuilder Type() => new();
    public static ParameterPatternBuilder Parameter() => new();
    public static PropertyPatternBuilder Property() => new();
    public static FieldPatternBuilder Field() => new();

    public static SymbolPattern<T> Any<T>(params SymbolPattern<T>[] patterns) where T : ISymbol =>
        patterns.Length == 0
            ? AlwaysTruePattern<T>.Instance
            : patterns.Aggregate((a, b) => a.Or(b));

    public static SymbolPattern<T> All<T>(params SymbolPattern<T>[] patterns) where T : ISymbol =>
        patterns.Length == 0
            ? AlwaysTruePattern<T>.Instance
            : patterns.Aggregate((a, b) => a.And(b));

    public static SymbolPattern<T> Where<T>(Func<T, bool> predicate) where T : ISymbol =>
        new PredicatePattern<T>(predicate);
}

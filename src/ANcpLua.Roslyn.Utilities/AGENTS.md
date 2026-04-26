The killer combination is **extension properties + property patterns**. Today you can't pattern-match on extension methods, which forces source-gen pipelines into awkward shapes.

**Today** — Roslyn helpers must be methods, so pipeline filters can't use property patterns:

```csharp
internal static class SymbolHelpers
{
    public static bool IsValueEquatable(this ITypeSymbol s) =>
        s.SpecialType == SpecialType.System_String
        || s is INamedTypeSymbol { IsRecord: true } or { IsValueType: true };

    public static bool HasQylSkill(this ISymbol s, INamedTypeSymbol attr) =>
        s.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attr));
}

// pipeline — can't pattern-match, falls back to imperative checks
.Select(static (ctx, _) =>
{
    var sym = (INamedTypeSymbol)ctx.TargetSymbol;
    if (!sym.IsValueEquatable()) return null;
    if (!sym.HasQylSkill(ctx.QylSkillAttr)) return null;
    return new SkillModel(sym.ToDisplayString(...), ...);
})
```

**Hypothetical "extension everything"** — properties, so the pipeline collapses into one switch:

```csharp
internal static class SymbolHelpers
{
    extension(ITypeSymbol s)
    {
        public bool IsValueEquatable =>
            s.SpecialType == SpecialType.System_String
            || s is INamedTypeSymbol { IsRecord: true } or { IsValueType: true };

        public string FullName => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    extension(ISymbol s)
    {
        public bool IsQylSkill =>
            s.GetAttributes().Any(a => a.AttributeClass?.Name == "QylSkillAttribute");
    }
}

// pipeline — single switch expression with exhaustive property patterns
.Select(static (ctx, _) => ctx.TargetSymbol switch
{
    INamedTypeSymbol { IsValueEquatable: true, IsQylSkill: true, FullName: var fqn } s
        => new SkillModel(fqn, s.GetMembers().OfType<IMethodSymbol>().ToImmutableArray()),
    _ => null
});
```

The pipeline reads like a spec. No imperative guards, no `()` noise inside `Where`/`Select`, no `if (x is null) return null` ladder.

**The other transformative win — extension operators** would retire `EquatableArray<T>` entirely:

```csharp
// today: EquatableArray<T> exists ONLY because ImmutableArray<T> lacks value ==
public readonly record struct EquatableArray<T>(ImmutableArray<T> Values) : IEquatable<EquatableArray<T>>
{
    public bool Equals(EquatableArray<T> other) => Values.AsSpan().SequenceEqual(other.Values.AsSpan());
    public override int GetHashCode() { /* hand-rolled */ }
    // + ImmutableArray<T> -> EquatableArray<T> wrapping/unwrapping at every pipeline boundary
}

// hypothetical: extension operators on the type you don't own
extension<T>(ImmutableArray<T>) where T : IEquatable<T>
{
    public static bool operator ==(ImmutableArray<T> a, ImmutableArray<T> b) => a.AsSpan().SequenceEqual(b.AsSpan());
    public static bool operator !=(ImmutableArray<T> a, ImmutableArray<T> b) => !(a == b);
}
```

Now `ImmutableArray<T>` works directly in an `IIncrementalGenerator` pipeline — the entire `EquatableArray<T>` wrapper, every `.ToEquatableArray()` call site, and the value-equality footgun chapter of every Roslyn-utilities readme just… vanish.

The third quiet win — **extension static factories** — lets you write `INamedTypeSymbol.FromMetadataName(comp, "Microsoft.Agents.AI.AIAgent")` instead of `comp.GetTypeByMetadataName(...)`, which matters because the type name is the search key, not the compilation. Reads in the direction of intent.
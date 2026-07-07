# ANcpLua.DiscriminatedUnion

Roslyn incremental generator (netstandard2.0): a `[DiscriminatedUnion]` partial
record + nested case records becomes a closed union — sealed cases, exhaustive
`Match`/`Switch`.

- Project: [ANcpLua.Analyzers.DiscriminatedUnion.csproj](ANcpLua.Analyzers.DiscriminatedUnion.csproj)
- Source of truth: `DiscriminatedUnionGenerator.cs`, `Extraction/`, `Generation/`, `Models/`.
- Shared helpers: [../ANcpLua.Roslyn.Utilities](../ANcpLua.Roslyn.Utilities/CLAUDE.md).

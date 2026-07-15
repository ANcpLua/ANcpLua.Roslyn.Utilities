# ANcpLua.Roslyn.Utilities

Minimal navigation for Claude/Codex agents. Keep policy in `AGENTS.md`; keep findings in issues, PRs, or tests.

This repo lives in `~/RiderProjects/qyl-workspace/`; workspace-level rules are in
the router at `../AGENTS.md`. Downstream chain: this repo → `ANcpLua.NET.Sdk` →
`ANcpLua.Agents` (`../ANcpLua.Agents/`) → qyl (`../qyl/`).

## Project Index

1. [AOT reflection generator](src/ANcpLua.AotReflection/CLAUDE.md)
2. [AOT reflection attributes](src/ANcpLua.AotReflection.Attributes/CLAUDE.md)
3. [Discriminated union generator](src/ANcpLua.DiscriminatedUnion/CLAUDE.md)
4. [Extensible enum mirror generator](src/ANcpLua.ExtensibleEnumMirror/CLAUDE.md)
5. [Core Roslyn utilities](src/ANcpLua.Roslyn.Utilities/CLAUDE.md)
6. [Polyfills package](src/ANcpLua.Roslyn.Utilities.Polyfills/CLAUDE.md)
7. [Source-only package](src/ANcpLua.Roslyn.Utilities.Sources/CLAUDE.md)
8. [Testing utilities](src/ANcpLua.Roslyn.Utilities.Testing/CLAUDE.md)
9. [AOT testing utilities](src/ANcpLua.Roslyn.Utilities.Testing.Aot/CLAUDE.md)

## Nearby Repos

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk): shared SDK/version truth for the ANcpLua repos.
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers): analyzer consumer of the source-only utilities.
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents): successor location for agent workflow/test helpers; do not describe this repo as the MAF runtime home.

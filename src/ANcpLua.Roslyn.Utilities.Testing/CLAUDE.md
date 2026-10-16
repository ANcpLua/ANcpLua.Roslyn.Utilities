# ANcpLua.Roslyn.Utilities.Testing

Framework-independent Roslyn testing core. Keep test-framework attributes, assertions,
lifecycle contracts and output interfaces in their explicit integration packages.
`Compile`, analyzer/generator runners, and `RepositoryRoot` must remain usable from a plain console app.

- Core project: [ANcpLua.Roslyn.Utilities.Testing.csproj](ANcpLua.Roslyn.Utilities.Testing.csproj).
- Neutral build/process helpers: sibling `ANcpLua.Roslyn.Utilities.Testing.MSBuild`.
- Opt-in integrations: siblings `.Testing.Xunit`, `.Testing.NUnit`, `.Testing.TUnit`, `.Testing.Bunit`.
- `PackageConsumptionTests` runs a packed core consumer and checks its complete resolved dependency graph.

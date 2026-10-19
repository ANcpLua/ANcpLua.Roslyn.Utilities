# Changelog

## 3.0.1

- XML documentation of `AotSafe`, `TrimSafe` and `AotUnsafe` names the current ANcpLua.Analyzers rule IDs (AL1402-AL1405); the README names AL1313.
- Repository: .NET SDK 10.0.402, no unused version properties or package versions, no csproj-level LangVersion/Nullable duplicating Directory.Build.props.

## 3.0.0

- Separate framework-independent Roslyn helpers from opt-in xUnit, NUnit, TUnit, and bUnit packages. The core package no longer installs unrelated test frameworks or AI clients.
- Move neutral project/process helpers to `ANcpLua.Roslyn.Utilities.Testing.MSBuild`; output callbacks use `Action<string>` instead of xUnit interfaces. Move xUnit assertions and fixtures to `.Testing.Xunit`. Existing namespaces remain; consumers must reference the package that owns each API.
- Expose MSBuild builder collections as `Collection<T>` and build-result items as `IReadOnlyList<string>`; parse recorded properties with ordinal comparison.
- Make `Compile.BuildOrThrow` throw its documented `InvalidOperationException` with compiler diagnostics.
- Correct process execution so binary-log arguments do not reach application arguments, each invocation has its own log, and commands that produce no binary log preserve their output and exit code.
- Update the transitive cryptography dependency used by NuGet tooling.

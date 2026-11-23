# Changelog

## 5.0.1

- CI: actions/setup-dotnet digest 0f4a354.
- Tests: the parallel-enumeration tests allow 30 seconds instead of 5; a loaded Windows runner missed the 5-second window.

## 5.0.0

.NET 11 and C# 15. The `5.0.0-preview.1` line ends here; 4.x is skipped so this release sorts above that preview.

- Toolchain: .NET SDK 11.0.100, Microsoft.CodeAnalysis 5.12.0 and Microsoft.CodeAnalysis.Analyzers 5.12.0. The AotReflection and ExtensibleEnumMirror generators, and any analyzer or generator that references the `ANcpLua.Roslyn.Utilities` assembly, need a Roslyn 5.12 host (SDK 11.0.100 or later); `.Sources` still compiles against Microsoft.CodeAnalysis 4.14 and later.
- Target frameworks: `.Testing`, `.Testing.MSBuild`, `.Testing.Xunit`, `.Testing.NUnit`, `.Testing.TUnit` and `.Testing.Bunit` move from net10.0 to net11.0; `ANcpLua.Analyzers.AotReflection.Attributes` ships netstandard2.0 and net11.0. The netstandard2.0 packages are unchanged.
- `.Polyfills` and `.Sources` supply the C# 15 `UnionAttribute` and `IUnion` on targets before .NET 11; `InjectUnionOnLegacy=false` (`Prop.InjectUnionOnLegacy`) opts out. A C# 15 compiler is still required.
- `LanguageVersionExtensions.IsCSharp15OrAbove`.
- `.Testing`: the analyzer, code-fix and refactoring runners compile test code against the .NET 11 reference assemblies (Basic.Reference.Assemblies.Net110, Microsoft.NETCore.App.Ref 11.0.0). Breaking: the `useNet10References` parameters are now `useNet11References`.
- `.Testing.MSBuild`: `NetSdkVersion.Net110` and `Tfm.Net110`. `ProjectBuilder` defaults to the .NET 11 SDK and writes `global.json` for the selected SDK, so `WithDotnetSdkVersion(NetSdkVersion.Net100)` now pins 10.0.100; with `NetSdkVersion.Ambient` there is no SDK pin, in MTP mode too. `PackageTestBase` defaults to net11.0. Breaking: `NetSdkVersion.Ambient` changed its numeric value.
- Dependencies: Microsoft.Extensions.Diagnostics.Testing and Telemetry.Abstractions 11.0.1, Microsoft.AspNetCore.Mvc.Testing 11.0.0, System.Threading.Channels and System.Security.Cryptography.Pkcs 11.0.0, Basic.Reference.Assemblies 1.8.15.
- Repository: `AnalysisLevel` 11.0-all; CI publishes from `main` only (the `release/**` prerelease lines are gone).

## 3.0.2

- Fix: the ANcpLua.Roslyn.Utilities and .Testing assemblies in 3.0.0 and 3.0.1 carried AssemblyVersion 0.0.0.0 (2.2.48 carried 2.2.48.0), so `.Testing.Xunit` 3.0.1, which references `.Testing` 3.0.1.0, failed at runtime with FileNotFoundException. A test packed the src projects with VersionPrefix 0.0.0 into the build output that the release then packed with `--no-build`. That test now builds into its own artifacts path, and CI packs before it runs tests.

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

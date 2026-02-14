[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Sources?label=Sources&color=7C3AED)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Sources/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Polyfills?label=Polyfills&color=D97706)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Polyfills/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Testing?label=Testing&color=059669)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Testing/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Roslyn.Utilities

Utilities for Roslyn analyzers and source generators.

## Packages

| Package | Purpose |
|---------|---------|
| `ANcpLua.Roslyn.Utilities` | Core utilities (DLL reference) |
| `ANcpLua.Roslyn.Utilities.Sources` | Source-only package (embeds as `internal` in analyzers/generators) |
| `ANcpLua.Roslyn.Utilities.Polyfills` | Source-only polyfills for `netstandard2.0` (no Roslyn dependency) |
| `ANcpLua.Roslyn.Utilities.Testing` | Testing framework for Roslyn tooling |

## Installation

```bash
# For analyzers/generators (source-only, no runtime dependency)
dotnet add package ANcpLua.Roslyn.Utilities.Sources

# For polyfills only (no Roslyn dependency)
dotnet add package ANcpLua.Roslyn.Utilities.Polyfills

# For runtime reference
dotnet add package ANcpLua.Roslyn.Utilities

# For testing
dotnet add package ANcpLua.Roslyn.Utilities.Testing
```

## Polyfills

The `.Polyfills` and `.Sources` packages include polyfills for modern C# features on `netstandard2.0`:

| Polyfill | What it enables | Opt-out property |
|----------|-----------------|------------------|
| `Index` / `Range` | `array[^1]`, `array[1..3]` syntax | `InjectIndexRangeOnLegacy` |
| `IsExternalInit` | `record` types and `init` properties | `InjectIsExternalInitOnLegacy` |
| Nullable attributes | `[NotNull]`, `[MaybeNull]`, etc. | `InjectNullabilityAttributesOnLegacy` |
| Trim/AOT attributes | `[RequiresUnreferencedCode]`, etc. | `InjectTrimAttributesOnLegacy` |
| `TimeProvider` | Testable time abstraction | `InjectTimeProviderPolyfill` |
| `Lock` | `System.Threading.Lock` polyfill | `InjectLockPolyfill` |
| String extensions | `string.Contains(StringComparison)` | `InjectStringExtensionsPolyfill` |

All polyfills are enabled by default. Set any property to `false` to opt out, or disable all with:

```xml
<InjectAllPolyfillsOnLegacy>false</InjectAllPolyfillsOnLegacy>
```

## Documentation

**[ancplua.mintlify.app/utilities](https://ancplua.mintlify.app/utilities/overview)**

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk)
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)

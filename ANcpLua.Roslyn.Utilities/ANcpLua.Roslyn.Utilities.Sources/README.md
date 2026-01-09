# ANcpLua.Roslyn.Utilities.Sources

Source-only version of [ANcpLua.Roslyn.Utilities](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities) for embedding in source generators and analyzers.

## When to Use This Package

| Scenario | Package to Use |
|----------|----------------|
| Normal library/application | `ANcpLua.Roslyn.Utilities` (binary) |
| Source Generator | `ANcpLua.Roslyn.Utilities.Sources` (this package) |
| Roslyn Analyzer | `ANcpLua.Roslyn.Utilities.Sources` (this package) |
| Test project for generators | `ANcpLua.Roslyn.Utilities.Testing` (binary) |

## Why Source-Only?

Source generators and analyzers run inside the Roslyn compiler process, which **cannot load NuGet runtime dependencies**. The utilities must be embedded as source code that compiles directly into your analyzer/generator assembly.

## Installation

```xml
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" PrivateAssets="all" />
```

The `PrivateAssets="all"` ensures the source files are embedded but not exposed as a transitive dependency.

## What's Included

All utilities from `ANcpLua.Roslyn.Utilities`:

- **EquatableArray<T>** - Value-equal array wrapper for incremental generator caching
- **Symbol Extensions** - Attribute lookup, type hierarchy, accessibility checks
- **Syntax Extensions** - Syntax node helpers and transformations
- **Pipeline Extensions** - Filtered syntax providers for incremental generators
- **Diagnostic Helpers** - Cache-safe diagnostic info models

## Visibility

All types are compiled as `internal` in your assembly. This is intentional:
- Prevents conflicts with other packages using the same utilities
- Keeps implementation details hidden from consumers

## Conditional Compilation

The package defines `ANCPLUA_ROSLYN_UTILITIES_SOURCES` automatically. You can use this in your source files:

```csharp
#if ANCPLUA_ROSLYN_UTILITIES_SOURCES
// Code that only runs when using source-only version
#endif
```

## License

MIT - Same as the binary package.

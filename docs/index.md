# ANcpLua Developer Documentation

Comprehensive documentation for the ANcpLua .NET development ecosystem.

## Packages

| Package | Description |
|---------|-------------|
| [ANcpLua.NET.Sdk](sdk/) | Zero-config .NET SDK with analyzers, polyfills, and best practices |
| [ANcpLua.Roslyn.Utilities](utilities/) | Utilities for Roslyn analyzers and source generators |
| [ANcpLua.Analyzers](https://ancplua.github.io/ANcpLua.Analyzers/) | 17 diagnostic rules for C# code quality |

## Quick Start

```xml
<!-- Use the SDK for zero-config best practices -->
<Project Sdk="ANcpLua.NET.Sdk" />

<!-- Or add utilities to your analyzer/generator project -->
<PackageReference Include="ANcpLua.Roslyn.Utilities" Version="*" />
```

## What You Get

### With ANcpLua.NET.Sdk

- Latest C# language features enabled
- Nullable reference types enabled
- Maximum analyzer coverage
- Deterministic builds with SourceLink
- 50+ banned APIs with suggested alternatives
- Auto-injected test frameworks (xUnit v3, AwesomeAssertions)
- Web defaults (OpenTelemetry, health checks, resilience)
- 15+ polyfills for legacy target frameworks

### With ANcpLua.Roslyn.Utilities

- `DiagnosticFlow<T>` - Railway-oriented programming for generators
- `SemanticGuard<T>` - Declarative symbol validation
- `SymbolPattern` - Composable pattern matching
- Domain contexts (Awaitable, ASP.NET, Disposable, Collection)
- 170+ extension methods for Roslyn APIs
- `EquatableArray<T>` - Value-equal arrays for generator caching

## Navigation

- **[SDK Documentation](sdk/)** - Complete guide to ANcpLua.NET.Sdk features
- **[Roslyn Utilities](utilities/)** - Extension methods and helper classes

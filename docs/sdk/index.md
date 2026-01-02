# ANcpLua.NET.Sdk

Zero-config .NET SDK that provides best practices, analyzers, polyfills, and sensible defaults for all your .NET projects.

## Installation

Replace your SDK reference with ANcpLua.NET.Sdk:

```xml
<Project Sdk="ANcpLua.NET.Sdk/1.3.15">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## What You Get "For Free"

| Category | Features |
|----------|----------|
| **Compiler** | Latest C#, nullable enabled, implicit usings |
| **Analyzers** | 3 packages pre-configured, 100s of rules |
| **Banned APIs** | 50+ dangerous APIs blocked with alternatives |
| **Testing** | xUnit v3, AwesomeAssertions, MTP auto-injected |
| **Web** | OpenTelemetry, health checks, resilience |
| **Polyfills** | 15+ features for legacy TFMs |
| **Build** | Deterministic, SourceLink, SBOM |

## SDK Variants

Choose the variant that matches your project type:

| Variant | Use Case |
|---------|----------|
| `ANcpLua.NET.Sdk` | Libraries, console apps, workers |
| `ANcpLua.NET.Sdk.Web` | ASP.NET Core with ServiceDefaults |
| `ANcpLua.NET.Sdk.Test` | Test projects with xUnit v3 + MTP |

## Quick Links

- [SDK Variants](variants.md) - Detailed variant comparison
- [Compiler Settings](compiler.md) - What's configured automatically
- [Analyzers](analyzers.md) - Included analyzer packages
- [Banned APIs](banned-apis.md) - Blocked APIs and alternatives
- [Test Projects](testing.md) - Auto-injected test frameworks
- [Web Projects](web.md) - ServiceDefaults and telemetry
- [Polyfills](polyfills.md) - Legacy TFM support
- [Build & Package](build.md) - Deterministic builds and packaging

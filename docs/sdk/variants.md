# SDK Variants

ANcpLua.NET.Sdk comes in three variants, each tailored for specific project types.

## Base SDK

For libraries, console applications, and worker services.

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Includes:**
- All compiler settings
- Analyzers and banned APIs
- Guard clauses (`Throw.IfNull`)
- Build and package defaults

## Web SDK

For ASP.NET Core applications. Adds ServiceDefaults with OpenTelemetry, health checks, and resilience.

```xml
<Project Sdk="ANcpLua.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Adds:**
- OpenTelemetry tracing and metrics
- Health check endpoints (`/health`, `/health/ready`)
- HttpClient resilience (retry, circuit breaker)
- JSON configuration (camelCase, null handling)
- DevLogs (`console.log` to server logs)
- DI container validation on startup

## Test SDK

For test projects. Adds xUnit v3, AwesomeAssertions, and Microsoft Testing Platform.

```xml
<Project Sdk="ANcpLua.NET.Sdk.Test">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Adds:**
- xunit.v3.mtp-v2 + parallel framework
- AwesomeAssertions + analyzers
- GitHubActionsTestLogger (on CI)
- TRX report generation

## Choosing a Variant

| Project Type | Recommended Variant |
|-------------|---------------------|
| Class library | `ANcpLua.NET.Sdk` |
| Console app | `ANcpLua.NET.Sdk` |
| Worker service | `ANcpLua.NET.Sdk` |
| ASP.NET Core API | `ANcpLua.NET.Sdk.Web` |
| Blazor | `ANcpLua.NET.Sdk.Web` |
| Unit tests | `ANcpLua.NET.Sdk.Test` |
| Integration tests | `ANcpLua.NET.Sdk.Test` |

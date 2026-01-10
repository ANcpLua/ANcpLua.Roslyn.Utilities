# Web Projects (ServiceDefaults)

The Web SDK variant auto-registers production-ready defaults via source generator.

## Usage

```xml
<Project Sdk="ANcpLua.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Auto-Registered Features

| Feature | What It Does |
|---------|--------------|
| **OpenTelemetry** | Tracing + metrics for observability |
| **Health Checks** | `/health` and `/health/ready` endpoints |
| **Resilience** | Retry, circuit breaker on HttpClient |
| **JSON Config** | CamelCase, null handling |
| **DevLogs** | `console.log` from browser to server logs |
| **Validation** | DI container validation on startup |

## OpenTelemetry

Automatic instrumentation for:
- HTTP requests (incoming and outgoing)
- Database queries (EF Core, Dapper)
- gRPC calls
- Custom spans via `ActivitySource`

Export to:
- Console (development)
- OTLP (production)
- Zipkin, Jaeger (configurable)

## Health Checks

Two endpoints are registered:

| Endpoint | Purpose |
|----------|---------|
| `/health` | Liveness probe - is the app running? |
| `/health/ready` | Readiness probe - is the app ready for traffic? |

Configure in Kubernetes:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
```

## HttpClient Resilience

All `HttpClient` instances get:

| Policy | Configuration |
|--------|---------------|
| **Retry** | 3 attempts with exponential backoff |
| **Circuit Breaker** | Opens after 5 failures, 30s duration |
| **Timeout** | 30s per request |

## JSON Configuration

Default JSON serializer settings:

```csharp
new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
};
```

## DI Validation

On startup, the SDK validates:
- All registered services can be resolved
- No circular dependencies
- No missing registrations

Catches configuration errors before they reach production.

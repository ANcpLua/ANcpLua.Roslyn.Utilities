[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Testing?label=Testing&color=059669)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Testing/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Roslyn.Utilities

Utilities for Roslyn analyzers and source generators. Less boilerplate, proper caching, no lost diagnostics.

## Installation

```bash
dotnet add package ANcpLua.Roslyn.Utilities
dotnet add package ANcpLua.Roslyn.Utilities.Testing  # for tests
```

## Quick Start

```csharp
// Railway-oriented pipeline - diagnostics flow through, never lost
provider
    .SelectFlow(ExtractModel)
    .ThenFlow(Validate)
    .ReportAndContinue(context)
    .AddSource(context);

// Fluent symbol matching
Match.Method().Async().ReturningTask().Public().Matches(method);
Invoke.Method("Dispose").OnTypeImplementing("IDisposable").Matches(invocation);

// Declarative validation
SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustHaveCancellationToken(ctRequired)
    .ToFlow();
```

## Features

| Category | Key APIs |
|----------|----------|
| **Flow Control** | `DiagnosticFlow<T>`, `ReportAndContinue()`, `CollectFlows()` |
| **Pattern Matching** | `Match.*` (symbols), `Invoke.*` (operations) |
| **Validation** | `SemanticGuard<T>` |
| **Domain Contexts** | `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext` |
| **Extensions** | `SymbolExtensions`, `TypeSymbolExtensions`, `OperationExtensions`, `InvocationExtensions` |
| **Code Generation** | `IndentedStringBuilder`, `ValueStringBuilder`, `GeneratedCodeHelpers` |
| **Pipeline** | `GroupBy()`, `Batch()`, `Distinct()`, `CollectAsEquatableArray()` |

## Packages

| Package | Target | Description |
|---------|--------|-------------|
| `ANcpLua.Roslyn.Utilities` | netstandard2.0 | Core library |
| `ANcpLua.Roslyn.Utilities.Testing` | net10.0 | Generator test framework with caching validation |

## Documentation

**[ancplua.mintlify.app/utilities](https://ancplua.mintlify.app/utilities/overview)**

## Related

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK
- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Custom analyzers

## License

[MIT](LICENSE)

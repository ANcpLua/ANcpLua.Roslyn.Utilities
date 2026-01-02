# ANcpLua.Roslyn.Utilities

Comprehensive utilities for Roslyn analyzers and incremental source generators.

## Installation

```bash
dotnet add package ANcpLua.Roslyn.Utilities
```

## Overview

| Category | Key Types |
|----------|-----------|
| **Flow Control** | `DiagnosticFlow<T>` |
| **Validation** | `SemanticGuard<T>` |
| **Pattern Matching** | `SymbolPattern`, `Match.*`, `Invoke.*` |
| **Domain Contexts** | `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext` |
| **Code Generation** | `IndentedStringBuilder`, `GeneratedCodeHelpers` |
| **Caching** | `EquatableArray<T>`, `HashCombiner` |

## Quick Examples

### Railway-Oriented Pipelines

```csharp
symbol.ToFlow(nullDiag)
    .Then(ValidateMethod)
    .Where(m => m.IsAsync, asyncRequired)
    .WarnIf(m => m.IsObsolete, obsoleteWarn)
    .Then(GenerateCode);
```

### Pattern Matching

```csharp
var asyncTask = SymbolPattern.Method()
    .Async()
    .ReturnsTask()
    .WithCancellationToken()
    .Public()
    .Build();

if (asyncTask.Matches(method)) { ... }
```

### Declarative Validation

```csharp
SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustReturnTask(taskRequired)
    .MustHaveCancellationToken(ctRequired)
    .ToFlow();
```

## Extension Methods

The library provides 170+ extension methods across 20 classes:

| Class | Purpose |
|-------|---------|
| `SymbolExtensions` | Equality, attributes, visibility, names |
| `TypeSymbolExtensions` | Inheritance, interfaces, special types |
| `MethodSymbolExtensions` | Overrides, interface implementations |
| `OperationExtensions` | Tree traversal, context detection |
| `InvocationExtensions` | Method call analysis |
| `IncrementalValuesProviderExtensions` | Pipeline helpers |
| `SourceProductionContextExtensions` | Source output |
| `StringExtensions` | Zero-allocation parsing |
| `EnumerableExtensions` | Null-safe LINQ |

## Documentation

- [DiagnosticFlow](diagnostic-flow.md) - Railway-oriented programming
- [SemanticGuard](semantic-guard.md) - Declarative validation
- [Pattern Matching](patterns.md) - Composable symbol patterns
- [Domain Contexts](contexts.md) - Awaitable, ASP.NET, Disposable, Collection
- [Pipeline Extensions](pipeline.md) - Generator pipeline helpers
- [Symbol Extensions](symbols.md) - Symbol analysis utilities
- [Operation Extensions](operations.md) - IOperation tree traversal
- [Code Generation](codegen.md) - IndentedStringBuilder, helpers

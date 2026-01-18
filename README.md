[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Testing?label=Testing&color=059669)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Testing/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Roslyn.Utilities

Comprehensive utilities for Roslyn analyzers and source generators.

> **CRITICAL: Layer 0 Package - No SDK Dependency Allowed**
>
> This is an **upstream** package in the ANcpLua ecosystem. It **CANNOT** depend on ANcpLua.NET.Sdk.
> Any such dependency would create a circular reference and break the entire build chain.

## Dependency Hierarchy

```
Layer 0: ANcpLua.Roslyn.Utilities     <- YOU ARE HERE (publishes first)
    |
    v
Layer 1: ANcpLua.NET.Sdk              <- Consumes this package
    |
    v
Layer 2: Downstream repos             <- Consume the SDK
```

**Why this matters:**

- This package must be published to NuGet **before** ANcpLua.NET.Sdk can reference it
- Uses `Microsoft.NET.Sdk` directly (not any custom SDK)
- Uses [PolySharp](https://github.com/Sergio0694/PolySharp) for netstandard2.0 polyfills instead of SDK-provided
  polyfills
- CI enforcement: GitHub Actions checks fail if any ANcpLua.NET.Sdk dependency is detected

## Installation

```shell
dotnet add package ANcpLua.Roslyn.Utilities
dotnet add package ANcpLua.Roslyn.Utilities.Testing
```

## Packages

| Package                              | Target         | Description                                                                              |
|--------------------------------------|----------------|------------------------------------------------------------------------------------------|
| **ANcpLua.Roslyn.Utilities**         | netstandard2.0 | Core library with EquatableArray, symbol extensions, pipeline extensions, DiagnosticFlow |
| **ANcpLua.Roslyn.Utilities.Testing** | net10.0        | Testing framework for incremental generators with caching validation                     |

## Building

```bash
# Build
dotnet build -c Release

# Pack
dotnet pack -c Release
```

## Highlights

### DiagnosticFlow - Railway-Oriented Programming

Never lose diagnostics in your pipeline:

```csharp
symbol.ToFlow(nullDiag)
    .Then(ValidateMethod)
    .Where(m => m.IsAsync, asyncRequired)
    .WarnIf(m => m.IsObsolete, obsoleteWarn)
    .Then(GenerateCode);

// Pipeline integration
provider
    .SelectFlow(ExtractModel)
    .ThenFlow(ValidateModel)
    .ReportAndContinue(context)
    .AddSource(context);
```

### Symbol Pattern Matching

Replace 50-line if-statements with composable patterns:

```csharp
var asyncTask = SymbolPattern.Method()
    .Async()
    .ReturnsTask()
    .WithCancellationToken()
    .Public()
    .Build();

if (asyncTask.Matches(method)) { ... }
```

### SemanticGuard - Declarative Validation

```csharp
SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustReturnTask(taskRequired)
    .MustHaveCancellationToken(ctRequired)
    .ToFlow();  // -> DiagnosticFlow<IMethodSymbol>
```

## API Overview

| Category             | Key APIs                                                                      |
|----------------------|-------------------------------------------------------------------------------|
| **Flow Control**     | `DiagnosticFlow<T>`, `ReportAndContinue()`                                    |
| **Pattern Matching** | `SymbolPattern.*`, `Match.*`, `Invoke.*`                                      |
| **Validation**       | `SemanticGuard<T>`, `MustBeAsync()`, `MustBePartial()`                        |
| **Domain Contexts**  | `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext` |
| **Operations**       | `OperationExtensions`, `InvocationExtensions`, `OverloadFinder`               |
| **Code Generation**  | `IndentedStringBuilder`, `GeneratedCodeHelpers`                               |
| **Pipeline**         | `GroupBy()`, `Batch()`, `Distinct()`, `CollectFlows()`                        |

## Symbol Extensions

```csharp
// Core
symbol.IsEqualTo(other)
symbol.HasAttribute("Full.Name")
symbol.IsVisibleOutsideOfAssembly()

// Type checking
type.InheritsFrom(baseType)
type.Implements(interfaceType)
type.IsTaskType() / IsSpanType() / IsEnumerableType()

// Methods
method.IsInterfaceImplementation()
method.IsOrOverrideMethod(baseMethod)
```

## Operation Extensions

```csharp
// Navigation
operation.Ancestors()
operation.FindAncestor<TOperation>()
operation.Descendants()

// Context
operation.IsInExpressionTree()
operation.IsInsideLoop()
operation.IsInsideTryBlock()

// Invocations
invocation.GetArgument("name")
invocation.IsLinqMethod()
invocation.AllArgumentsAreConstant()
```

## Domain Contexts

Pre-cached symbol lookups for common patterns:

```csharp
var ctx = new AwaitableContext(compilation);
ctx.IsTaskLike(type)
ctx.IsAwaitable(type)
ctx.CanUseAsyncKeyword(method)

var asp = new AspNetContext(compilation);
asp.IsController(type)
asp.IsAction(method)
asp.IsFromBody(parameter)

var disp = new DisposableContext(compilation);
disp.IsDisposable(type)
disp.HasDisposeMethod(type)

var coll = new CollectionContext(compilation);
coll.IsImmutable(type)
coll.GetElementType(type)
```

## Code Generation

```csharp
var sb = new IndentedStringBuilder();
using (sb.BeginNamespace("MyNamespace"))
using (sb.BeginClass("public partial", "MyClass"))
using (sb.BeginMethod("public", "void", "Execute"))
{
    sb.AppendLine("// generated code");
}
```

## Pipeline Extensions

```csharp
provider
    .SelectFlow(ExtractModel)
    .ThenFlow(ValidateModel)
    .WarnIf(m => m.IsOld, obsoleteWarn)
    .ReportAndContinue(context)
    .AddSource(context);

// Collection operations
provider.GroupBy(keySelector)
provider.Batch(100)
provider.Distinct()
provider.CollectAsEquatableArray()
```

## Testing Library

```csharp
using var result = await Test<MyGenerator>.Run(source);
result
    .Produces("Output.g.cs", expectedContent)
    .IsCached()
    .IsClean()
    .HasNoForbiddenTypes();
```

## Documentation

Full reference: https://ancplua.mintlify.app/utilities/overview

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)
- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities/)
[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Roslyn.Utilities.Testing?label=Testing&color=059669)](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Testing/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Roslyn.Utilities

Utilities for building Roslyn incremental source generators with proper caching.

## Installation

```shell
dotnet add package ANcpLua.Roslyn.Utilities
dotnet add package ANcpLua.Roslyn.Utilities.Testing  # For tests
```

## Packages

| Package | Target | Description |
|---------|--------|-------------|
| `ANcpLua.Roslyn.Utilities` | netstandard2.0 | Core utilities for incremental generators |
| `ANcpLua.Roslyn.Utilities.Testing` | net10.0 | Fluent testing with caching validation |

## Key APIs

### EquatableArray&lt;T&gt;

Value-equal array wrapper — essential for generator caching (ImmutableArray uses reference equality):

```csharp
// Pipeline outputs need value equality for caching to work
context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => new MyModel { Items = GetItems(ctx).AsEquatableArray() });
```

### Pipeline Extensions

```csharp
// Simplified attribute filtering for classes/records
var provider = context.SyntaxProvider
    .ForAttributeWithMetadataNameOfClassesAndRecords("MyNamespace.MyAttribute");

// Add sources with automatic file naming
provider.Select(GenerateFile).AddSource(context);

// Collect with proper caching
var collected = provider.CollectAsEquatableArray();
```

### Testing

```csharp
// Verify generated output
await source.ShouldGenerate<MyGenerator>("Output.g.cs", expectedContent);

// Verify diagnostics
await source.ShouldHaveDiagnostics<MyGenerator>(
    Diagnostic("GEN001", DiagnosticSeverity.Error));

// Validate caching (catches forbidden types: ISymbol, Compilation, SyntaxNode, etc.)
await source.ShouldBeCached<MyGenerator>();
```

## Related

- [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers) — Analyzers using these utilities
- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) — MSBuild SDK with embedded source generator helpers

## License

[MIT](LICENSE)

# ANcpLua.Roslyn.Utilities

Reusable utilities and fluent testing framework for Roslyn incremental source generators.

## Packages

| Package                                                                                               | Description                               | Target         |
|-------------------------------------------------------------------------------------------------------|-------------------------------------------|----------------|
| [`ANcpLua.Roslyn.Utilities`](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities)                 | Core utilities for incremental generators | netstandard2.0 |
| [`ANcpLua.Roslyn.Utilities.Testing`](https://www.nuget.org/packages/ANcpLua.Roslyn.Utilities.Testing) | Fluent testing framework                  | net10.0        |

## Installation

```bash
# Core utilities for your generator project
dotnet add package ANcpLua.Roslyn.Utilities

# Testing framework for your test project
dotnet add package ANcpLua.Roslyn.Utilities.Testing
```

## Core Utilities

### EquatableArray<T>

A value-equal immutable array wrapper essential for incremental generator caching:

```csharp
using ANcpLua.Roslyn.Utilities;

// Convert ImmutableArray to EquatableArray for proper caching
var items = myImmutableArray.AsEquatableArray();

// Use in pipeline outputs - enables proper caching
context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => new MyModel { Items = GetItems(ctx).AsEquatableArray() });
```

### Pipeline Extensions

```csharp
using ANcpLua.Roslyn.Utilities;

// Simplified attribute filtering for classes and records
var provider = context.SyntaxProvider
    .ForAttributeWithMetadataNameOfClassesAndRecords("MyNamespace.MyAttribute");

// Add generated sources with automatic file naming
provider.Select(GenerateFile).AddSource(context);
provider.Select(GenerateFiles).AddSources(context);

// Collect as EquatableArray for proper caching
var collected = provider.CollectAsEquatableArray();

// Exception handling with diagnostic reporting
provider.SelectAndReportExceptions(Transform, context);
```

### String Extensions

```csharp
using ANcpLua.Roslyn.Utilities;

// Convert to property/parameter names
"userName".ToPropertyName();   // "UserName"
"UserName".ToParameterName();  // "userName" (handles C# keywords)

// Text processing for generated code
source.TrimBlankLines();       // Remove whitespace-only lines
source.NormalizeLineEndings();
"MyNamespace.MyClass".ExtractNamespace();   // "MyNamespace"
"MyNamespace.MyClass".ExtractSimpleName();  // "MyClass"
"MyNamespace.MyClass".WithGlobalPrefix();   // "global::MyNamespace.MyClass"
```

### Models

```csharp
// Structured generator output
record struct FileWithName(string Name, string Text);

// Result with associated diagnostics
record struct ResultWithDiagnostics<T>(T Result, EquatableArray<Diagnostic> Diagnostics);
```

## Testing Framework

### Quick Start

```csharp
using ANcpLua.Roslyn.Utilities.Testing;

[Fact]
public async Task Generator_ProducesExpectedOutput()
{
    await """
        [GenerateBuilder]
        public class Person
        {
            public string Name { get; set; }
        }
        """.ShouldGenerate<MyGenerator>("Person.Builder.g.cs", """
            public class PersonBuilder
            {
                public PersonBuilder WithName(string name) { ... }
            }
            """);
}
```

### Testing Generated Output

```csharp
// Verify exact content
await source.ShouldGenerate<MyGenerator>("Output.g.cs", expectedContent);

// Verify content contains substring
await source.ShouldGenerate<MyGenerator>("Output.g.cs", "public class Foo", exactMatch: false);
```

### Testing Diagnostics

```csharp
// Expect specific diagnostics
await "public class Invalid { }".ShouldHaveDiagnostics<MyGenerator>(
    GeneratorTestExtensions.Diagnostic("GEN001", DiagnosticSeverity.Error)
        .WithMessage("Missing required attribute")
);

// Expect no diagnostics
await "[Valid] public class Valid { }".ShouldHaveNoDiagnostics<MyGenerator>();
```

### Testing Caching

Validate that your generator correctly caches intermediate results:

```csharp
// Validate all observable pipeline steps are cached
await source.ShouldBeCached<MyGenerator>();

// Validate specific steps
await source.ShouldBeCached<MyGenerator>("TransformStep", "CollectStep");
```

The caching test validates:

- **No forbidden types cached**: ISymbol, Compilation, SyntaxNode, SemanticModel, SyntaxTree
- **Proper caching**: Pipeline steps produce Cached/Unchanged on second run

### Configuration

```csharp
// Configure language version and references
TestConfiguration.LanguageVersion = LanguageVersion.CSharp12;
TestConfiguration.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

// Add additional references for your generator's attributes
TestConfiguration.AdditionalReferences = [
    MetadataReference.CreateFromFile(typeof(MyAttribute).Assembly.Location)
];
```

### Direct Assertions

For more control, use the assertion API directly on run results:

```csharp
// Assert on generator run results
result.Should().HaveGeneratedSource("Output.g.cs")
    .Which.Should().HaveContent(expectedContent);

result.Should().HaveNoDiagnostics();
result.Should().NotHaveForbiddenTypes();

// Assert on caching report
report.Should().BeValidAndCached(["TransformStep"]);
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities).
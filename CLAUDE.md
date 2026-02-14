# CLAUDE.md - ANcpLua.Roslyn.Utilities

**The SOURCE OF TRUTH for Roslyn utilities.** Other repositories (ErrorOrX, ANcpLua.Analyzers, qyl) consume these utilities. Check here FIRST before writing any helper code.

## Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  <-- YOU ARE HERE (UPSTREAM SOURCE OF TRUTH)
         | publishes packages
LAYER 1: ANcpLua.NET.Sdk           <-- Syncs Version.props from here
         | auto-syncs Version.props
LAYER 2: ANcpLua.Analyzers         <-- DOWNSTREAM (uses SDK)
         | consumed by
LAYER 3: ErrorOrX, qyl, other      <-- END USERS
```

### Critical Rules

| Rule | Description |
|------|-------------|
| **No SDK dependency** | This repo CANNOT depend on ANcpLua.NET.Sdk (circular) |
| **Publish first** | Must publish to NuGet before SDK can update |
| **Check before writing** | Always search this repo before writing helper code |

## Packages

| Package | Target | Purpose |
|---------|--------|---------|
| `ANcpLua.Roslyn.Utilities` | `netstandard2.0` | Core utilities for generators/analyzers |
| `ANcpLua.Roslyn.Utilities.Sources` | `netstandard2.0` | Source-only package (embeds as `internal` in analyzers/generators) |
| `ANcpLua.Roslyn.Utilities.Polyfills` | `netstandard2.0` | Source-only polyfills (no Roslyn dependency) |
| `ANcpLua.Roslyn.Utilities.Testing` | `net10.0` | Testing framework for Roslyn tooling |

## Build Commands

```bash
dotnet build -c Release
dotnet pack -c Release
dotnet test
```

---

## Utility Catalog - CHECK HERE FIRST

Before writing ANY helper code, search this catalog. Duplication is the enemy.

### Core Types

| Type | Purpose | Location |
|------|---------|----------|
| `EquatableArray<T>` | Value-equality array for generator caching | `EquatableArray.cs` |
| `DiagnosticFlow<T>` | Railway-oriented error accumulation | `DiagnosticFlow.cs` |
| `Result<T>` | General-purpose success/failure result | `Result.cs` |
| `Error` | Error type with Code + Message | `Result.cs` |
| `DiagnosticInfo` | Equatable diagnostic for caching | `Models/DiagnosticInfo.cs` |
| `LocationInfo` | Serializable location | `Models/LocationInfo.cs` |
| `FileWithName` | Hint name + generated content | `Models/FileWithName.cs` |
| `HashCombiner` | Hash code combining | `HashCombiner.cs` |
| `ValueStringBuilder` | Stack-allocated string building | `ValueStringBuilder.cs` |
| `EmptyServiceProvider` | Singleton null-returning IServiceProvider | `EmptyServiceProvider.cs` |

### Pattern Matching DSL

| Entry Point | Purpose | Location |
|-------------|---------|----------|
| `Match.Method()` | Method symbol matching | `Matching/SymbolMatch.cs` |
| `Match.Type()` | Type symbol matching | `Matching/SymbolMatch.cs` |
| `Match.Property()` | Property symbol matching | `Matching/SymbolMatch.cs` |
| `Match.Field()` | Field symbol matching | `Matching/SymbolMatch.cs` |
| `Match.Parameter()` | Parameter symbol matching | `Matching/SymbolMatch.cs` |
| `Invoke.Method()` | Invocation operation matching | `Matching/InvocationMatch.cs` |

### Validation

| Type | Purpose | Location |
|------|---------|----------|
| `Guard.NotNull()` | Argument validation with CallerArgumentExpression | `Guard.cs` |
| `Guard.NotNullOrElse()` | Null fallback (eager or lazy) | `Guard.cs` |
| `Guard.HasMinLength()` | String length validation | `Guard.cs` |
| `Guard.NotDefault<T>()` | Value type not default | `Guard.cs` |
| `Guard.OneOf<T>()` | Set membership validation | `Guard.cs` |
| `Guard.NoDuplicates<T>()` | Collection duplicate detection | `Guard.cs` |
| `Guard.FileExists()` | File/directory existence validation | `Guard.cs` |
| `Guard.ValidFileName()` | Path character validation | `Guard.cs` |
| `Guard.DefinedEnum<T>()` | Enum value validation | `Guard.cs` |
| `Guard.NotNegative()` | Numeric guards (int, long, double, decimal) | `Guard.cs` |
| `Guard.Unreachable()` | Unreachable code marker with caller info | `Guard.cs` |
| `SemanticGuard<T>` | Declarative semantic validation | `SemanticGuard.cs` |

### Domain Contexts (cache well-known types)

| Context | Purpose | Location |
|---------|---------|----------|
| `AwaitableContext` | Task/ValueTask/async patterns | `Contexts/AwaitableContext.cs` |
| `AspNetContext` | Controllers, actions, binding | `Contexts/AspNetContext.cs` |
| `DisposableContext` | IDisposable/IAsyncDisposable | `Contexts/DisposableContext.cs` |
| `CollectionContext` | IEnumerable, lists, dictionaries | `Contexts/CollectionContext.cs` |

### Extension Categories

| File | Purpose |
|------|---------|
| `ResultExtensions.cs` | Select/Then/Tap/Where + async variants for Result&lt;T&gt; |
| `SymbolExtensions.cs` | Symbol attributes, visibility, equality, attribute type extraction |
| `TypeSymbolExtensions.cs` | Type hierarchy, primitives, patterns, codegen helpers |
| `MethodSymbolExtensions.cs` | Interface implementation, overrides |
| `OperationExtensions.cs` | Operation tree navigation, context detection |
| `InvocationExtensions.cs` | Invocation arguments, receivers |
| `SyntaxExtensions.cs` | Syntax modifiers, locations |
| `CompilationExtensions.cs` | Target framework detection |
| `EnumerableExtensions.cs` | `.OrEmpty()`, `.WhereNotNull()`, etc. |
| `NullableExtensions.cs` | Functional nullable transforms |
| `StringExtensions.cs` | Name conversion, line splitting, hashing, graph escaping |
| `StringComparisonExtensions.cs` | Ordinal comparisons |
| `ReflectionExtensions.cs` | Multi-TFM MethodInfo invoke, generic method lookup |
| `RuntimeTypeExtensions.cs` | Runtime Type checks for tasks, open generics |
| `TryExtensions.cs` | TryParse, TryGet patterns |
| `DictionaryExtensions.cs` | Dictionary utilities |
| `ListExtensions.cs` | List utilities |

### Pipeline Extensions

| File | Purpose |
|------|---------|
| `IncrementalValuesProviderExtensions.cs` | Generator pipeline operations |
| `SyntaxValueProviderExtensions.cs` | Syntax-filtered providers |
| `SourceProductionContextExtensions.cs` | Output source files |

### Code Generation

| Type | Purpose | Location |
|------|---------|----------|
| `IndentedStringBuilder` | Structured code building | `CodeGeneration.cs` |
| `GeneratedCodeHelpers` | Standard headers/attributes | `CodeGeneration.cs` |

### Configuration

| File | Purpose |
|------|---------|
| `AnalyzerConfigOptionsProviderExtensions.cs` | MSBuild property access |
| `AnalyzerOptionsExtensions.cs` | EditorConfig access |

### Comparers

| Type | Purpose | Location |
|------|---------|----------|
| `StringOrdinalComparer` | Ordinal string comparison (case-sensitive) | `Comparers/StringOrdinalComparer.cs` |
| `StringOrdinalIgnoreCaseComparer` | Ordinal string comparison (case-insensitive) | `Comparers/StringOrdinalComparer.cs` |

### Polyfills (netstandard2.0)

All polyfills are `#if`-gated and compile only on older TFMs. Included in `.Sources` and `.Polyfills` packages.

| Category | Files | TFM Guard |
|----------|-------|-----------|
| Language Features | `IsExternalInit`, `RequiredMember`, `CompilerFeatureRequired`, `SetsRequiredMembers`, `CallerArgumentExpression`, `ParamCollection` | `!NET5_0` to `!NET9_0` |
| Nullable Attributes | `AllowNull`, `MaybeNull`, `NotNull`, `MemberNotNull`, etc. | `!NETCOREAPP3_1` / `!NET5_0` |
| Trim/AOT Attributes | `DynamicallyAccessedMembers`, `RequiresUnreferencedCode`, `RequiresDynamicCode`, `UnconditionalSuppressMessage` | `!NET5_0` / `!NET7_0` |
| Index/Range | `Index`, `Range` | `!NETCOREAPP3_0 && !NETSTANDARD2_1` |
| Diagnostics | `StackTraceHiddenAttribute` | `!NET6_0` |
| Exceptions | `UnreachableException` | `!NET7_0` |
| Experimental | `ExperimentalAttribute` | `!NET8_0` |
| TimeProvider | `TimeProvider`, `ITimer` | `!NET8_0` |
| Lock | `System.Threading.Lock` | `!NET9_0` |
| String Extensions | `Contains`, `Replace`, `IndexOf` with `StringComparison` | `!NETCOREAPP2_1 && !NETSTANDARD2_1` |

**Opt-out**: Set `InjectXxxOnLegacy=false` per category, or `InjectAllPolyfillsOnLegacy=false` for all.

### Analyzer Infrastructure

| Type | Purpose | Location |
|------|---------|----------|
| `DiagnosticAnalyzerBase` | Base analyzer with standard config | `Analyzers/DiagnosticAnalyzerBase.cs` |
| `CodeFixProviderBase<T>` | Base code fix with transform pattern | `CodeFixes/CodeFixProviderBase.cs` |

---

## Key Utility Reference

### EquatableArray<T> - Generator Caching

```csharp
// Create from ImmutableArray
var equatable = items.ToImmutableArray().AsEquatableArray();

// Create from array (ownership transfer)
var equatable = new[] { "a", "b" }.ToEquatableArray();

// Use in records for generator caching
public record EndpointModel(
    string Name,
    EquatableArray<string> Parameters  // Value equality!
);

// Access
equatable.Length
equatable[index]
equatable.AsImmutableArray()
equatable.AsSpan()
equatable.IsDefaultOrEmpty
```

### DiagnosticFlow<T> - Railway-Oriented Programming

```csharp
// Create flows
DiagnosticFlow.Ok(model)
DiagnosticFlow.Fail<T>(diagnostic)
symbol.ToFlow(nullDiagnostic)  // fails if null

// Chain operations
flow.Then(Validate)           // chain DiagnosticFlow-returning functions
    .Select(m => m.Transform) // map values (no new diagnostics)
    .Where(m => m.IsValid, errorDiag)  // filter with diagnostic on false
    .WarnIf(m => m.IsOld, warnDiag)    // add warning if predicate true

// Combine
DiagnosticFlow.Zip(flow1, flow2)        // (T1, T2) tuple
DiagnosticFlow.Collect(flows)           // all must succeed
DiagnosticFlow.CollectSuccesses(flows)  // keep only successes

// Pipeline integration
provider
    .SelectFlow(ExtractModel)
    .ThenFlow(ValidateModel)
    .ReportAndContinue(context)  // report diagnostics, return successes
    .AddSource(context);
```

### Match.* DSL - Symbol Matching

```csharp
// Method matching
Match.Method()
    .Named("Execute")
    .Async()
    .Public()
    .WithParameters(2)
    .WithCancellationToken()
    .Matches(method);

// Multiple attributes (any match)
Match.Method()
    .WithAttribute("Xunit.FactAttribute", "Xunit.TheoryAttribute")
    .Matches(method);

// Type matching
Match.Type()
    .Class()
    .Public()
    .Implements("IDisposable")
    .HasParameterlessConstructor()
    .Matches(type);

// IMPORTANT: Matchers mutate! Create new for each pattern
static MethodMatcher PublicInstance() => Match.Method().Public().NotStatic();
var asyncApi = PublicInstance().Async();
var syncApi = PublicInstance().NotAsync();  // fresh matcher
```

### Invoke.* - Operation Matching

```csharp
Invoke.Method("Dispose")
    .OnTypeImplementing("IDisposable")
    .WithNoArguments()
    .Matches(invocation);

// Multiple names
Invoke.Method("WriteLine", "Write").OnConsole().Matches(invocation);

// Multiple receiver types
Invoke.Method("Wait").OnType("Task", "ValueTask").Matches(invocation);

// LINQ methods
Invoke.Method().Linq().Named("Where").Matches(invocation);
```

### Guard - Argument Validation

```csharp
// Null validation
var validated = Guard.NotNull(possiblyNull);
var config = Guard.NotNullOrElse(optionalConfig, DefaultConfig);
var conn = Guard.NotNullWithMember(config, config?.ConnectionString);

// String/collection validation
Guard.NotNullOrEmpty(name);
Guard.NotNullOrWhiteSpace(title);
Guard.NoDuplicates(items);

// File system validation
var path = Guard.FileExists(filePath);
var dir = Guard.DirectoryExists(dirPath);
var name = Guard.ValidFileName(fileName);

// Type validation
Guard.DefinedEnum(status);
Guard.AssignableTo<IService>(serviceType);

// Numeric validation (int, long, double, decimal)
Guard.NotNegative(count);
Guard.Positive(amount);
Guard.NotGreaterThan(value, 100);
Guard.InRange(page, 1, maxPages);

// Unreachable code
return status switch
{
    Status.Active => "Active",
    _ => Guard.Unreachable<string>()
};
```

### Collection Extensions

```csharp
// Null-safe operations
items.OrEmpty()              // returns empty if null
items.WhereNotNull()         // filter nulls
items.ToImmutableArrayOrEmpty()

// Utilities
items.HasDuplicates()
items.SingleOrDefaultIfMultiple()
seq1.SequenceEquals(seq2)
items.GetSequenceHashCode()
```

### Context Classes

```csharp
// Create once per compilation, reuse for all checks
var awaitable = new AwaitableContext(compilation);
awaitable.IsTaskLike(type)
awaitable.IsAwaitable(type)
awaitable.CanUseAsyncKeyword(method)

var disposable = new DisposableContext(compilation);
disposable.IsDisposable(type)
disposable.ShouldBeDisposed(type)  // excludes DI-managed

var aspnet = new AspNetContext(compilation);
aspnet.IsController(type)
aspnet.IsAction(method)
aspnet.IsFromBody(parameter)
```

---

## Helper Selection Guide

| Question | Use |
|----------|-----|
| "Validate argument, throw if null" | `Guard.NotNull()` |
| "Provide fallback for null" | `Guard.NotNullOrElse()` |
| "Transform nullable in pipeline" | `NullableExtensions.Select()` |
| "Safe cast" | `ObjectExtensions.As<T>()` |
| "Parse with null on failure" | `TryExtensions.TryParse*()` |
| "Compare strings explicitly" | `StringComparisonExtensions.EqualsOrdinal()` |
| "Check if type implements X" | `TypeSymbolExtensions.Implements()` |
| "Match method pattern" | `Match.Method()...Matches()` |
| "Match invocation" | `Invoke.Method()...Matches()` |
| "Accumulate errors in pipeline" | `DiagnosticFlow<T>` |
| "General-purpose success/failure" | `Result<T>` |
| "Chain result operations" | `ResultExtensions.Select/Then/Tap/Where` |
| "Async result pipelines" | `ResultExtensions.ThenAsync/TapAsync/ToResult` |
| "Cache array in generator" | `EquatableArray<T>` |
| "Extract typeof() args from attributes" | `symbol.GetAttributeTypeArguments()` |
| "Get nested type chain for codegen" | `type.GetContainingTypeChain()` |
| "Get generic clause for codegen" | `type.GetGenericParameterClause()` |
| "Short deterministic ID from string" | `input.ToShortHash()` |
| "Escape label for graph visualization" | `label.EscapeDotLabel()` / `.EscapeMermaidLabel()` |
| "Invoke method without TIE wrapping" | `method.InvokeUnwrapped()` |
| "Find method on closed generic from open" | `type.GetMethodFromGenericDefinition()` |
| "Is this Type a Task<T> or ValueTask<T>?" | `type.IsGenericTask()` / `.IsGenericValueTask()` |
| "Find IHandler<> implementations" | `type.ImplementsOpenGeneric()` / `.GetClosedImplementations()` |
| "Need empty IServiceProvider" | `EmptyServiceProvider.Instance` |
| "Ordinal string comparer for dictionaries" | `StringOrdinalComparer.Instance` / `.IgnoreCase` |
| "Polyfills for netstandard2.0 generator" | `ANcpLua.Roslyn.Utilities.Sources` (includes polyfills) |
| "Polyfills without Roslyn dependency" | `ANcpLua.Roslyn.Utilities.Polyfills` |

---

## Structure

```
ANcpLua.Roslyn.Utilities/
├── ANcpLua.Roslyn.Utilities/         # netstandard2.0 - Core library
│   ├── Comparers/                    # StringOrdinalComparer
│   ├── Contexts/                     # Domain-specific type caches
│   ├── Matching/                     # Symbol/invocation matching DSL
│   ├── Models/                       # Equatable data models
│   └── Polyfills/                    # All netstandard2.0 polyfills (SOURCE OF TRUTH)
│       ├── DiagnosticAttributes/     # NullableAttributes
│       ├── Diagnostics/              # StackTraceHidden
│       ├── Exceptions/               # UnreachableException
│       ├── Experimental/             # ExperimentalAttribute
│       ├── IndexRange/               # Index, Range
│       ├── LanguageFeatures/         # IsExternalInit, Required, etc.
│       ├── NullabilityAttributes/    # MemberNotNull
│       ├── StringExtensions/         # String.Contains(StringComparison)
│       ├── TimeProvider/             # TimeProvider polyfill
│       ├── TrimAttributes/           # DynamicallyAccessedMembers, etc.
│       └── Lock.cs                   # System.Threading.Lock polyfill
├── ANcpLua.Roslyn.Utilities.Sources/  # Source-only NuGet (public→internal + polyfills)
├── ANcpLua.Roslyn.Utilities.Polyfills/ # Source-only polyfills (no Roslyn dependency)
└── ANcpLua.Roslyn.Utilities.Testing/  # net10.0 - Testing framework
    ├── MSBuild/                      # Integration test infrastructure
    ├── Analysis/                     # Caching analysis
    ├── Formatting/                   # Report formatting
    └── Instrumentation/              # OpenTelemetry integration
```

## Release Order

```
1. Roslyn.Utilities -> publish to NuGet  <-- YOU ARE HERE
2. SDK -> update Version.props -> publish
3. Sync Version.props to downstream repos
4. Downstream repos can now build
```

## Common CI Errors

### SDK Version Not Found (in downstream repos)
```
error: Unable to find package ANcpLua.NET.Sdk with version (= X.X.X)
```

**Fix:** Either downgrade global.json to published version, OR publish SDK first.

### Circular Dependency Detected
```
error: ANcpLua.Roslyn.Utilities cannot reference ANcpLua.NET.Sdk
```

**Fix:** Remove the SDK reference. This repo is UPSTREAM - it cannot depend on SDK.

# DiagnosticFlow

Railway-oriented programming for source generator pipelines. Never lose a diagnostic.

## The Problem

Traditional generator code loses diagnostics:

```csharp
// BAD: Diagnostics get lost
var model = ExtractModel(syntax);
if (model == null) return; // Where's the diagnostic?

var validated = Validate(model);
if (!validated.Success) return; // Lost again!
```

## The Solution

`DiagnosticFlow<T>` carries both value AND diagnostics through the pipeline:

```csharp
// GOOD: Diagnostics flow through
symbol.ToFlow(nullDiag)
    .Then(ExtractModel)
    .Then(Validate)
    .Then(Generate)
    .ReportAndContinue(context);
```

## Creating Flows

```csharp
// From value
var flow = DiagnosticFlow.Ok(value);

// From nullable (fails if null)
var flow = symbol.ToFlow(nullDiag);

// With initial diagnostics
var flow = DiagnosticFlow.Ok(value, warnings);

// Failed flow
var flow = DiagnosticFlow.Fail<T>(errorDiag);
```

## Chaining Operations

### Then - Transform Value

```csharp
flow.Then(value => TransformValue(value))
    .Then(transformed => AnotherTransform(transformed));
```

### Select - Map Without Flow

```csharp
flow.Select(value => value.Name)
    .Select(name => name.ToUpperInvariant());
```

### Where - Filter with Diagnostic

```csharp
flow.Where(
    predicate: m => m.IsAsync,
    onFail: asyncRequiredDiag
);
```

### WarnIf - Conditional Warning

```csharp
flow.WarnIf(
    predicate: m => m.IsObsolete,
    warning: obsoleteWarning
);
```

## Combining Flows

```csharp
// Tuple of two flows (both must succeed)
var combined = DiagnosticFlow.Zip(flow1, flow2);
// Result: DiagnosticFlow<(T1, T2)>

// Collect all (all must succeed, diagnostics accumulated)
var all = DiagnosticFlow.Collect(flows);
// Result: DiagnosticFlow<ImmutableArray<T>>
```

## Result Handling

```csharp
// Get value or default
var value = flow.ValueOrDefault(fallback);

// Pattern match
flow.Match(
    onSuccess: value => HandleSuccess(value),
    onFailure: diagnostics => HandleFailure(diagnostics)
);

// Execute side effect
flow.Do(value => LogValue(value));
```

## Pipeline Integration

Use with `IncrementalValuesProvider`:

```csharp
var pipeline = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .SelectFlow(ctx => ExtractModel(ctx))
    .ThenFlow(model => Validate(model))
    .WarnIf(model => model.IsDeprecated, deprecatedWarn)
    .ReportAndContinue(context)
    .Select(model => GenerateCode(model));

context.RegisterSourceOutput(pipeline, (ctx, code) =>
    ctx.AddSource(code.Name, code.Content));
```

## Properties

| Property | Description |
|----------|-------------|
| `IsSuccess` | True if no errors |
| `IsFailed` | True if has errors |
| `HasErrors` | True if any error-severity diagnostic |
| `Value` | The wrapped value (throws if failed) |
| `Diagnostics` | All accumulated diagnostics |

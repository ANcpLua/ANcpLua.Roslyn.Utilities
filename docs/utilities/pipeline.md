# Pipeline Extensions

Extensions for `IncrementalValuesProvider<T>` to simplify generator pipelines.

## Source Output

```csharp
// Single source file
provider.AddSource(context);

// Multiple source files
provider.AddSources(context);

// With auto-generated header
context.AddSourceWithHeader("file.g.cs", code, "MyGenerator");
```

## DiagnosticFlow Integration

```csharp
// Transform to flow
provider.SelectFlow(ctx => ExtractModel(ctx))

// Chain flow operations
.ThenFlow(model => Validate(model))

// Conditional warning
.WarnIf(model => model.IsDeprecated, deprecatedWarn)

// Filter with diagnostic
.WhereFlow(model => model.IsValid, invalidDiag)

// Report diagnostics, continue with successes
.ReportAndContinue(context)

// Or report and stop if errors
.ReportAndStop(context)

// Collect flows
.CollectFlows()
.CollectFlowSuccesses()
```

## Collection Operations

```csharp
// Collect to EquatableArray (for caching)
provider.CollectAsEquatableArray()

// Group by key
provider.GroupBy(
    keySelector: m => m.Namespace,
    elementSelector: m => m)

// Add indices
provider.WithIndex()  // (T Value, int Index)

// Unique values
provider.Distinct()
provider.Distinct(comparer)

// Batching
provider.Batch(100)  // ImmutableArray<T> of up to 100 items

// Take/Skip
provider.Take(10)
provider.Skip(5)

// Counting
provider.Count()
provider.Any()
provider.Any(predicate)
provider.FirstOrDefault()
```

## Combining Providers

```csharp
// Combine with collected values
provider1.CombineWithCollected(provider2)
// Result: (T1 Value, EquatableArray<T2> Collected)
```

## Error Handling

```csharp
// Catch exceptions as diagnostics
provider.SelectAndReportExceptions(
    selector: ctx => ExtractModel(ctx),
    context: initContext)

// Report diagnostics from results
provider.SelectAndReportDiagnostics(context)

// Filter nulls
provider.WhereNotNull()
```

## Complete Example

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName("MyAttribute", ...)
            .SelectFlow(ctx => ExtractModel(ctx))
            .ThenFlow(Validate)
            .WarnIf(m => m.IsObsolete, obsoleteWarn)
            .ReportAndContinue(context);

        var grouped = models
            .GroupBy(m => m.Namespace, m => m)
            .CollectAsEquatableArray();

        context.RegisterSourceOutput(grouped, (ctx, groups) =>
        {
            foreach (var group in groups)
            {
                var code = GenerateForNamespace(group.Key, group.Elements);
                ctx.AddSource($"{group.Key}.g.cs", code);
            }
        });
    }
}
```

# SemanticGuard

Declarative validation for Roslyn symbols. Accumulates violations as diagnostics.

## The Problem

Validation code gets verbose:

```csharp
// BAD: Verbose and error-prone
var diagnostics = new List<Diagnostic>();
if (!method.IsAsync)
    diagnostics.Add(Diagnostic.Create(asyncRequired, location));
if (!method.ReturnsTask())
    diagnostics.Add(Diagnostic.Create(taskRequired, location));
if (!method.HasCancellationToken())
    diagnostics.Add(Diagnostic.Create(ctRequired, location));
if (diagnostics.Count > 0)
    return null;
```

## The Solution

`SemanticGuard<T>` provides fluent validation:

```csharp
// GOOD: Clear and declarative
var flow = SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustReturnTask(taskRequired)
    .MustHaveCancellationToken(ctRequired)
    .ToFlow();
```

## Creating Guards

```csharp
// For methods
var guard = SemanticGuard.ForMethod(methodSymbol);

// For types
var guard = SemanticGuard.ForType(typeSymbol);

// For any symbol
var guard = SemanticGuard.For(symbol);
```

## Method Validations

```csharp
SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustNotBeAsync(syncRequired)
    .MustReturnTask(taskRequired)
    .MustReturnVoid(voidRequired)
    .MustHaveCancellationToken(ctRequired)
    .MustBePublic(publicRequired)
    .MustBeStatic(staticRequired)
    .MustNotBeStatic(instanceRequired)
    .MustBeVirtual(virtualRequired)
    .MustNotBeAbstract(concreteRequired);
```

## Type Validations

```csharp
SemanticGuard.ForType(type)
    .MustBeClass(classRequired)
    .MustHaveMultipleDeclarations(partialRequired)  // Detects multi-declaration partial types
    .MustBePublic(publicRequired)
    .MustNotBeStatic(instanceRequired)
    .MustNotBeAbstract(concreteRequired)
    .MustImplement(interfaceType, implRequired)
    .MustInheritFrom(baseType, inheritRequired);

// For parameterless constructor validation, use Match.Type():
Match.Type()
    .HasParameterlessConstructor()
    .Matches(type);
```

## Generic Validations

```csharp
guard
    .Must(s => s.DeclaredAccessibility == Accessibility.Public, publicRequired)
    .MustNot(s => s.IsAbstract, concreteRequired)
    .MustMatch(pattern, patternNotMatched)
    .MustHaveAttribute("MyAttribute", attrRequired);
```

## Pattern Matching Integration

Combine with `SymbolPattern`:

```csharp
var asyncHandler = SymbolPattern.Method()
    .Async()
    .ReturnsTask()
    .WithCancellationToken()
    .Build();

SemanticGuard.ForMethod(method)
    .MustMatch(asyncHandler, handlerPatternViolated)
    .ToFlow();
```

## Converting to Flow

```csharp
// Get as DiagnosticFlow
var flow = guard.ToFlow();

// Check validity
if (guard.IsValid)
{
    // All validations passed
}

// Get violations directly
var violations = guard.Violations;
```

## Complete Example

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider
            .ForAttributeWithMetadataName("HandlerAttribute", ...)
            .Select((ctx, _) =>
            {
                var method = ctx.TargetSymbol as IMethodSymbol;
                return SemanticGuard.ForMethod(method)
                    .MustBeAsync(Diagnostics.AsyncRequired)
                    .MustBePublic(Diagnostics.PublicRequired)
                    .MustHaveCancellationToken(Diagnostics.CtRequired)
                    .ToFlow()
                    .Select(m => new MethodModel(m));
            });

        pipeline
            .ReportAndContinue(context)
            .AddSource(context);
    }
}
```

# Symbol Matching DSL - Real-World Examples

## What Developers Actually Do

### 1. Detect Console.WriteLine in Production Code

**Before (verbose):**
```csharp
if (operation is IInvocationOperation invocation &&
    invocation.TargetMethod.ContainingType?.Name == "Console" &&
    invocation.TargetMethod.Name == "WriteLine")
{
    context.ReportDiagnostic(...);
}
```

**After (DSL):**
```csharp
var consoleWrite = Invoke.Method("WriteLine", "Write").OnConsole();

if (consoleWrite.Matches(operation))
    context.ReportDiagnostic(...);
```

---

### 2. Find Blocking Async Calls (Task.Result, .Wait())

```csharp
var blockingCalls = Invoke.Method("Wait", "GetAwaiter")
    .OnType("Task", "ValueTask");

var resultAccess = Match.Property("Result").DeclaredIn("Task");

// In analyzer:
foreach (var op in operations)
{
    if (blockingCalls.Matches(op) ||
        (op is IPropertyReferenceOperation prop && resultAccess.Matches(prop.Property)))
    {
        // Report: "Avoid blocking on async code"
    }
}
```

---

### 3. Detect Missing ConfigureAwait(false) in Library Code

```csharp
var awaitWithoutConfigure = Invoke.Method()
    .ReturningTask()
    .Where(i => !IsFollowedByConfigureAwait(i));

// Helper
bool IsFollowedByConfigureAwait(IInvocationOperation inv) =>
    inv.Parent is IAwaitOperation await &&
    await.Parent is IInvocationOperation configure &&
    configure.TargetMethod.Name == "ConfigureAwait";
```

---

### 4. Find String Concatenation in Loops

```csharp
var stringConcat = Invoke.Method("Concat").OnString();
// Or detect += on string
var stringAssign = /* assignment to string variable */;

// Check if inside loop
if (stringConcat.Matches(op) && op.IsInsideLoop())
{
    // Report: "Use StringBuilder for string concatenation in loops"
}
```

---

### 5. Validate Dispose Pattern Implementation

```csharp
var disposableType = Match.Type()
    .Implements("IDisposable");

var disposeMethod = Match.Method("Dispose")
    .Public()
    .ReturningVoid()
    .WithNoParameters();

var finalizer = Match.Method()
    .Finalizer();

// Check: If type implements IDisposable, it should have proper Dispose
foreach (var type in types.Where(disposableType.Matches))
{
    var hasDispose = type.GetMembers().Any(disposeMethod.Matches);
    var hasFinalizer = type.GetMembers().Any(finalizer.Matches);

    if (hasFinalizer && !hasDispose)
    {
        // Report: "Implement IDisposable pattern correctly"
    }
}
```

---

### 6. Detect Empty Catch Blocks

```csharp
// Find catch blocks that swallow exceptions
var catchHandler = /* ICatchClauseOperation */;
if (catchHandler.Handler.Operations.Length == 0 ||
    (catchHandler.Handler.Operations.Length == 1 &&
     catchHandler.Handler.Operations[0] is IEmptyOperation))
{
    // Report: "Empty catch block swallows exception"
}
```

---

### 7. Find Async Methods Without CancellationToken

```csharp
var asyncMethod = Match.Method()
    .Async()
    .Public()
    .Where(m => !m.Parameters.Any(p => p.Type.Name == "CancellationToken"));

foreach (var method in publicMethods.Where(asyncMethod.Matches))
{
    // Report: "Async method should accept CancellationToken"
}
```

---

### 8. Detect HttpClient Instantiation (Should Use IHttpClientFactory)

```csharp
var httpClientCtor = Match.Method()
    .Constructor()
    .DeclaredIn("HttpClient");

// In operation analyzer:
if (operation is IObjectCreationOperation creation &&
    creation.Type?.Name == "HttpClient")
{
    // Report: "Use IHttpClientFactory instead of new HttpClient()"
}
```

---

### 9. Source Generator: Find All Classes with Attribute

```csharp
var serializableType = Match.Type()
    .Class()
    .WithAttribute("System.SerializableAttribute")
    .Public();

// In generator pipeline:
var typesToGenerate = context.SyntaxProvider
    .CreateSyntaxProvider(...)
    .Where(ctx => serializableType.Matches(ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)));
```

---

### 10. Validate Test Method Conventions

```csharp
var testMethod = Match.Method()
    .WithAttribute("Xunit.FactAttribute", "Xunit.TheoryAttribute",
                   "NUnit.Framework.TestAttribute")
    .Public()
    .NotStatic()
    .ReturningVoid();

var asyncTestMethod = Match.Method()
    .WithAttribute("Xunit.FactAttribute", "Xunit.TheoryAttribute")
    .Public()
    .Async()
    .ReturningTask();

// Validate test methods have correct signature
foreach (var method in testClass.GetMembers().OfType<IMethodSymbol>())
{
    if (method.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Test") == true))
    {
        if (!testMethod.Matches(method) && !asyncTestMethod.Matches(method))
        {
            // Report: "Test method has incorrect signature"
        }
    }
}
```

---

## Pattern Composition

Matchers are composable - build complex patterns from simple ones:

```csharp
// Base patterns
var publicApi = Match.Method().Public().NotStatic();
var asyncApi = publicApi.Async().ReturningTask();
var syncApi = publicApi.NotAsync().ReturningVoid();

// Specific patterns built on base
var disposableApi = syncApi.Named("Dispose");
var asyncDisposableApi = asyncApi.Named("DisposeAsync");
```

## Performance Note

Matchers compile predicates into a list. Each `Matches()` call evaluates all predicates.
For hot paths, consider caching the matcher instance:

```csharp
// Cache at class level
private static readonly InvocationMatcher ConsoleWrite =
    Invoke.Method("WriteLine", "Write").OnConsole();

// Reuse in analyzer
public override void Analyze(OperationAnalysisContext context)
{
    if (ConsoleWrite.Matches(context.Operation))
        context.ReportDiagnostic(...);
}
```

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

### 4. Validate Dispose Pattern Implementation

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

### 5. Find Async Methods Without CancellationToken

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

### 6. Source Generator: Find All Classes with Attribute

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

### 7. Validate Test Method Conventions

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

## Feature Detection Patterns

These patterns show how to detect framework features in a compilation, similar to how ASP.NET Core
detects SignalR, Blazor, and other features at compile time.

### 8. Feature Detection with Symbol Caching

Use a symbols class to cache well-known types, avoiding repeated lookups:

```csharp
// Symbol cache for startup analysis
internal class StartupSymbols
{
    public INamedTypeSymbol? IEndpointRouteBuilder { get; }
    public INamedTypeSymbol? IApplicationBuilder { get; }
    public INamedTypeSymbol? Hub { get; }
    public INamedTypeSymbol? ComponentBase { get; }

    public bool HasRequiredSymbols =>
        IEndpointRouteBuilder is not null &&
        IApplicationBuilder is not null;

    public StartupSymbols(Compilation compilation)
    {
        IEndpointRouteBuilder = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");
        IApplicationBuilder = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Builder.IApplicationBuilder");
        Hub = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.SignalR.Hub");
        ComponentBase = compilation.GetTypeByMetadataName(
            "Microsoft.AspNetCore.Components.ComponentBase");
    }
}

// Usage with our matchers
public class FeatureDetector
{
    private readonly StartupSymbols _symbols;
    private readonly InvocationMatcher _signalRGesture;
    private readonly InvocationMatcher _blazorGesture;

    public FeatureDetector(Compilation compilation)
    {
        _symbols = new StartupSymbols(compilation);

        _signalRGesture = Invoke.Method("MapHub", "MapBlazorHub")
            .OnTypeImplementing("IEndpointRouteBuilder");

        _blazorGesture = Invoke.Method("MapRazorComponents", "AddInteractiveServerComponents")
            .OnTypeImplementing("IEndpointRouteBuilder");
    }

    public bool DetectSignalR(IOperation operation) =>
        _symbols.HasRequiredSymbols && _signalRGesture.Matches(operation);

    public bool DetectBlazor(IOperation operation) =>
        _symbols.HasRequiredSymbols && _blazorGesture.Matches(operation);
}
```

---

### 9. ASP.NET Core Middleware Detection

```csharp
// Detect middleware pipeline configuration
var middlewareGestures = new Dictionary<string, InvocationMatcher>
{
    ["CORS"] = Invoke.Method("UseCors", "AddCors"),
    ["HTTPS"] = Invoke.Method("UseHttpsRedirection", "UseHsts"),
    ["Auth"] = Invoke.Method("UseAuthentication", "UseAuthorization"),
    ["Routing"] = Invoke.Method("UseRouting", "UseEndpoints"),
    ["StaticFiles"] = Invoke.Method("UseStaticFiles", "UseDefaultFiles"),
    ["Swagger"] = Invoke.Method("UseSwagger", "UseSwaggerUI", "AddSwaggerGen"),
    ["HealthChecks"] = Invoke.Method("UseHealthChecks", "MapHealthChecks"),
    ["RateLimiting"] = Invoke.Method("UseRateLimiter", "AddRateLimiter"),
};

// Scan for all middleware in use
public IReadOnlySet<string> DetectMiddleware(IOperation configureOperation)
{
    var detected = new HashSet<string>();

    foreach (var invocation in configureOperation.DescendantsOfType<IInvocationOperation>())
    {
        foreach (var (feature, gesture) in middlewareGestures)
        {
            if (gesture.Matches(invocation))
            {
                detected.Add(feature);
                break; // One feature per invocation
            }
        }
    }

    return detected;
}
```

---

### 10. Dependency Injection Registration Analysis

```csharp
// Detect service registrations
var diRegistrations = Invoke.Method()
    .Named("AddSingleton", "AddScoped", "AddTransient",
           "AddHostedService", "AddDbContext", "AddHttpClient")
    .OnTypeImplementing("IServiceCollection");

// Find all registered service types
public IEnumerable<(string Lifetime, ITypeSymbol ServiceType)> AnalyzeRegistrations(
    IOperation operation)
{
    foreach (var invocation in operation.DescendantsOfType<IInvocationOperation>())
    {
        if (!diRegistrations.Matches(invocation))
            continue;

        var lifetime = invocation.TargetMethod.Name switch
        {
            "AddSingleton" => "Singleton",
            "AddScoped" => "Scoped",
            "AddTransient" => "Transient",
            "AddHostedService" => "Singleton",
            "AddDbContext" => "Scoped",
            "AddHttpClient" => "Transient",
            _ => "Unknown"
        };

        // Extract service type from generic argument
        if (invocation.TargetMethod.TypeArguments.Length > 0)
        {
            yield return (lifetime, invocation.TargetMethod.TypeArguments[0]);
        }
    }
}
```

---

### 11. Entity Framework Query Analysis

```csharp
// Detect potential N+1 queries
var efQueryMethods = Invoke.Method()
    .Named("ToList", "ToListAsync", "First", "FirstAsync",
           "Single", "SingleAsync", "ToArray", "ToArrayAsync")
    .OnTypeImplementing("IQueryable");

var includeMethod = Invoke.Method("Include", "ThenInclude")
    .OnTypeImplementing("IQueryable");

public void AnalyzeForNPlusOne(IOperation loopBody)
{
    // Find query materializations inside loops
    foreach (var invocation in loopBody.DescendantsOfType<IInvocationOperation>())
    {
        if (efQueryMethods.Matches(invocation))
        {
            // Check if there's an Include before materialization
            var hasInclude = invocation
                .Ancestors()
                .OfType<IInvocationOperation>()
                .Any(includeMethod.Matches);

            if (!hasInclude)
            {
                // Report: "Potential N+1 query - consider using Include()"
            }
        }
    }
}
```

---

## Operation Tree Traversal

### 12. Using OperationExtensions for Navigation

```csharp
// Our OperationExtensions provide navigation helpers:

// Find containing method
var containingMethod = operation.GetContainingMethod();

// Check context
if (operation.IsInsideTryBlock())
    // Inside try block

if (operation.IsInsideLoop())
    // Inside for/foreach/while/do

if (operation.IsInExpressionTree())
    // Inside Expression<Func<>>

// Navigate ancestors
var containingBlock = operation.FindAncestor<IBlockOperation>();
var containingLambda = operation.FindAncestor<IAnonymousFunctionOperation>();

// Find all descendants of type
var allInvocations = operation.DescendantsOfType<IInvocationOperation>();
var allAssignments = operation.DescendantsOfType<IAssignmentOperation>();

// Check if operation contains specific patterns
if (operation.ContainsOperation<IThrowOperation>())
    // Contains a throw statement
```

---

### 13. Combining Matchers with Operation Context

```csharp
// Detect async void event handlers (acceptable) vs async void methods (bad)
var asyncVoidMethod = Match.Method()
    .Async()
    .ReturningVoid();

var eventHandlerSignature = Match.Method()
    .WithParameters(2)
    .Where(m =>
        m.Parameters[0].Type.Name == "Object" &&
        m.Parameters[1].Type.Name.EndsWith("EventArgs"));

public void AnalyzeAsyncVoid(IMethodSymbol method)
{
    if (!asyncVoidMethod.Matches(method))
        return;

    // Async void is OK for event handlers
    if (eventHandlerSignature.Matches(method))
        return;

    // Report: "Avoid async void except for event handlers"
}
```

---

### 14. Flow-Based Analysis with DiagnosticFlow

```csharp
// Combine matching with DiagnosticFlow for railway-oriented analysis
var publicAsyncApi = Match.Method()
    .Public()
    .Async()
    .ReturningTask();

var hasCancellation = Match.Parameter()
    .CancellationToken();

public DiagnosticFlow<IMethodSymbol> ValidateAsyncApi(IMethodSymbol method)
{
    return method.ToFlow(Diagnostics.NotFound)
        .Where(
            m => publicAsyncApi.Matches(m),
            Diagnostics.NotPublicAsync)
        .WarnIf(
            m => !m.Parameters.Any(hasCancellation.Matches),
            Diagnostics.MissingCancellationToken)
        .WarnIf(
            m => !m.Name.EndsWith("Async"),
            Diagnostics.MissingAsyncSuffix);
}
```

---

## Pattern Composition

> **Important:** Matchers mutate `this` when chaining. Each call modifies the same instance.
> Create new matchers for each distinct pattern to avoid unintended side effects.

```csharp
// WRONG - mutates the same instance!
var publicApi = Match.Method().Public().NotStatic();
var asyncApi = publicApi.Async();  // This modifies publicApi!

// CORRECT - create separate matchers for each pattern
var asyncApi = Match.Method().Public().NotStatic().Async().ReturningTask();
var syncApi = Match.Method().Public().NotStatic().NotAsync().ReturningVoid();
var disposableApi = Match.Method().Public().NotStatic().NotAsync().ReturningVoid().Named("Dispose");
var asyncDisposableApi = Match.Method().Public().NotStatic().Async().ReturningTask().Named("DisposeAsync");

// Using factory methods for DRY patterns
static MethodMatcher PublicInstanceMethod() => Match.Method().Public().NotStatic();

var asyncApi = PublicInstanceMethod().Async().ReturningTask();
var syncApi = PublicInstanceMethod().NotAsync().ReturningVoid();
```


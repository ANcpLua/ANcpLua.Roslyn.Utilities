# Domain Contexts

Pre-cached symbol lookups for common patterns. Create once per compilation.

## AwaitableContext

Task and async pattern checking:

```csharp
var ctx = new AwaitableContext(compilation);

// Task types
ctx.IsTaskLike(type)                    // Task, Task<T>, ValueTask, ValueTask<T>
ctx.IsAwaitable(type)                   // Has GetAwaiter()
ctx.IsAwaitable(type, model, position)  // Includes extension methods
ctx.ConformsToAwaiterPattern(awaiter)   // IsCompleted, GetResult, OnCompleted
ctx.GetTaskResultType(taskType)         // T from Task<T>

// Async patterns
ctx.CanUseAsyncKeyword(method)          // Returns Task/ValueTask
ctx.IsAsyncEnumerable(type)             // IAsyncEnumerable<T>
ctx.IsAsyncEnumerator(type)             // IAsyncEnumerator<T>
ctx.IsConfiguredAwaitable(type)         // ConfiguredTaskAwaitable

// Properties
ctx.Task                 // System.Threading.Tasks.Task
ctx.TaskOfT              // System.Threading.Tasks.Task<T>
ctx.ValueTask            // System.Threading.Tasks.ValueTask
ctx.ValueTaskOfT         // System.Threading.Tasks.ValueTask<T>
ctx.IAsyncEnumerable     // System.Collections.Generic.IAsyncEnumerable<T>
```

## AspNetContext

ASP.NET Core pattern checking:

```csharp
var ctx = new AspNetContext(compilation);

// Controllers
ctx.IsController(type)          // Inherits ControllerBase
ctx.IsApiController(type)       // Has [ApiController]
ctx.IsAction(method)            // Public method on controller
ctx.HasHttpMethodAttribute(m)   // [HttpGet], [HttpPost], etc.
ctx.HasRouteAttribute(symbol)   // [Route]

// Results
ctx.IsActionResult(type)        // IActionResult or ActionResult<T>

// Parameters
ctx.HasBindingAttribute(param)  // Any binding attribute
ctx.IsFromBody(parameter)       // [FromBody]
ctx.IsFromQuery(parameter)      // [FromQuery]
ctx.IsFromRoute(parameter)      // [FromRoute]
ctx.IsFromServices(parameter)   // [FromServices]
ctx.IsFormFile(type)            // IFormFile

// Properties
ctx.ControllerBase       // Microsoft.AspNetCore.Mvc.ControllerBase
ctx.Controller           // Microsoft.AspNetCore.Mvc.Controller
ctx.IActionResult        // Microsoft.AspNetCore.Mvc.IActionResult
ctx.HttpGetAttribute     // [HttpGet]
ctx.HttpPostAttribute    // [HttpPost]
ctx.ApiControllerAttribute
```

## DisposableContext

IDisposable pattern checking:

```csharp
var ctx = new DisposableContext(compilation);

// Disposability
ctx.IsDisposable(type)           // IDisposable or IAsyncDisposable
ctx.IsSyncDisposable(type)       // IDisposable
ctx.IsAsyncDisposable(type)      // IAsyncDisposable
ctx.HasDisposeMethod(type)       // Has Dispose()
ctx.HasDisposeAsyncMethod(type)  // Has DisposeAsync()

// Common disposable types
ctx.IsStream(type)               // System.IO.Stream
ctx.IsDbConnection(type)         // System.Data.Common.DbConnection
ctx.IsHttpClient(type)           // System.Net.Http.HttpClient
ctx.IsSynchronizationPrimitive(type)  // Mutex, Semaphore, etc.

// Properties
ctx.IDisposable           // System.IDisposable
ctx.IAsyncDisposable      // System.IAsyncDisposable
ctx.Stream                // System.IO.Stream
ctx.DbConnection          // System.Data.Common.DbConnection
```

## CollectionContext

Collection type pattern checking:

```csharp
var ctx = new CollectionContext(compilation);

// Collection types
ctx.IsEnumerable(type)       // IEnumerable<T>
ctx.IsCollection(type)       // ICollection<T>
ctx.IsList(type)             // IList<T> or List<T>
ctx.IsDictionary(type)       // IDictionary<K,V> or Dictionary<K,V>
ctx.IsSet(type)              // ISet<T> or HashSet<T>

// Immutable collections
ctx.IsImmutable(type)        // System.Collections.Immutable.*
ctx.IsFrozen(type)           // FrozenDictionary, FrozenSet

// Span types
ctx.IsSpanLike(type)         // Span<T>, ReadOnlySpan<T>
ctx.IsMemoryLike(type)       // Memory<T>, ReadOnlyMemory<T>

// Characteristics
ctx.HasCountProperty(type)   // Has Count property
ctx.IsReadOnly(type)         // IReadOnlyCollection<T>
ctx.GetElementType(type)     // T from IEnumerable<T>

// Properties
ctx.IEnumerable         // System.Collections.Generic.IEnumerable<T>
ctx.ICollection         // System.Collections.Generic.ICollection<T>
ctx.ImmutableArray      // System.Collections.Immutable.ImmutableArray<T>
ctx.Span                // System.Span<T>
ctx.ReadOnlySpan        // System.ReadOnlySpan<T>
```

## Usage Pattern

Create contexts once, reuse throughout generation:

```csharp
context.RegisterSourceOutput(
    context.CompilationProvider,
    (ctx, compilation) =>
    {
        var awaitable = new AwaitableContext(compilation);
        var asp = new AspNetContext(compilation);
        // Use throughout generation
    });
```

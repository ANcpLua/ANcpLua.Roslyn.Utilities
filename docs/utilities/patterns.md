# Pattern Matching

Composable patterns for matching Roslyn symbols. Replace verbose if-statements with declarative patterns.

## SymbolPattern

Base pattern type with composition operators:

```csharp
// Build patterns
var asyncTask = SymbolPattern.Method()
    .Async()
    .ReturnsTask()
    .WithCancellationToken()
    .Public()
    .Build();

// Compose with operators
var combined = pattern1 & pattern2;  // AND
var either = pattern1 | pattern2;    // OR
var inverted = !pattern1;            // NOT

// Match
if (asyncTask.Matches(method))
{
    // Handle async task method
}
```

## Method Patterns

```csharp
SymbolPattern.Method()
    .Named("Execute")           // Exact name
    .NameStartsWith("Get")      // Name prefix
    .NameEndsWith("Async")      // Name suffix
    .Async()                    // Is async
    .NotAsync()                 // Not async
    .ReturnsTask()              // Returns Task/Task<T>/ValueTask
    .ReturnsVoid()              // Returns void
    .Returns(typeSymbol)        // Returns specific type
    .ParameterCount(2)          // Exact parameter count
    .ParameterCountAtLeast(1)   // Minimum parameters
    .WithCancellationToken()    // Has CancellationToken param
    .Public()                   // Public accessibility
    .Private()                  // Private accessibility
    .Static()                   // Is static
    .Instance()                 // Not static
    .Virtual()                  // Is virtual
    .Override()                 // Is override
    .Abstract()                 // Is abstract
    .IsConstructor()            // Is constructor
    .IsOperator()               // Is operator
    .ImplementsInterface()      // Implements interface member
    .HasAttribute("Attr")       // Has attribute
    .Build();
```

## Type Patterns

```csharp
SymbolPattern.Type()
    .IsClass()                  // Is class
    .IsStruct()                 // Is struct
    .IsInterface()              // Is interface
    .IsRecord()                 // Is record
    .IsEnum()                   // Is enum
    .IsDelegate()               // Is delegate
    .HasMultipleDeclarations()  // Has multiple partial declarations
    .Public()                   // Public accessibility
    .Sealed()                   // Is sealed
    .Abstract()                 // Is abstract
    .Static()                   // Is static
    .IsGeneric()                // Has type parameters
    .IsNotGeneric()             // No type parameters
    .TypeParameterCount(2)      // Exact type parameter count
    .Implements("IDisposable")  // Implements interface
    .InheritsFrom("BaseClass")  // Inherits from
    .InNamespace("MyNs")        // In exact namespace
    .InNamespaceStartsWith("My") // Namespace prefix
    .HasMethod("Dispose")       // Has method with name
    .HasProperty("Id")          // Has property with name
    .IsNested()                 // Is nested type
    .IsTopLevel()               // Is top-level type
    .IsDisposable()             // Implements IDisposable
    .HasAttribute("Attr")       // Has attribute
    .Build();
```

## Match.* DSL

Fluent matching without building:

```csharp
// Method matching
if (Match.Method()
    .Named("Execute")
    .Async()
    .ParameterCount(2)
    .Matches(method))
{
    // Handle
}

// Type matching
if (Match.Type()
    .IsClass()
    .Public()
    .Implements("IDisposable")
    .HasParameterlessConstructor()  // Available via Match.Type()
    .Matches(type))
{
    // Handle
}

// Property/Field matching
Match.Property().ReadOnly().Required().Matches(prop);
Match.Field().Const().Public().Matches(field);
```

## Invoke.* (Operation Matching)

Match method invocations:

```csharp
// Match specific method
Invoke.Method("Dispose")
    .OnTypeImplementing("IDisposable")
    .WithNoArguments()
    .Matches(invocation);

// Match LINQ methods
Invoke.Method()
    .Linq()
    .Named("Where")
    .Matches(invocation);

// Match string methods
Invoke.Method()
    .OnType("System.String")
    .Named("Contains")
    .Matches(invocation);
```

## Composition Examples

```csharp
// Handler method: async, public, returns Task, has CancellationToken
var handler = SymbolPattern.Method()
    .Async()
    .Public()
    .ReturnsTask()
    .WithCancellationToken()
    .Build();

// Disposable type: class, implements IDisposable
var disposable = SymbolPattern.Type()
    .IsClass()
    .Implements("System.IDisposable")
    .Build();

// Either async void or async Task
var asyncMethod = SymbolPattern.Method().Async().ReturnsVoid().Build()
    | SymbolPattern.Method().Async().ReturnsTask().Build();

// Public and not abstract
var concrete = SymbolPattern.Type().Public().Build()
    & !SymbolPattern.Type().Abstract().Build();
```

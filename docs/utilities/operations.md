# Operation Extensions

Extension methods for `IOperation` tree traversal and analysis.

## Tree Navigation

```csharp
// Ancestors
operation.Ancestors()                    // IEnumerable<IOperation>
operation.FindAncestor<TOperation>()     // First ancestor of type
operation.IsDescendantOf<TOperation>()   // Has ancestor of type

// Descendants
operation.Descendants()                  // IEnumerable<IOperation>
operation.DescendantsOfType<T>()         // Filtered descendants
operation.ContainsOperation<T>()         // Has descendant of type

// Container lookup
operation.GetContainingMethod()          // IMethodSymbol
operation.GetContainingType()            // INamedTypeSymbol
operation.GetContainingBlock()           // IBlockOperation
```

## Context Detection

```csharp
// Expression context
operation.IsInNameofOperation()          // Inside nameof()
operation.IsInExpressionTree()           // Inside Expression<>
operation.IsInStaticContext()            // In static method/property

// Block context
operation.IsInsideLoop()                 // In for/foreach/while/do
operation.IsInsideTryBlock()             // In try { }
operation.IsInsideCatchBlock()           // In catch { }
operation.IsInsideFinallyBlock()         // In finally { }

// Statement context
operation.IsInsideLockStatement()        // In lock { }
operation.IsInsideUsingStatement()       // In using { }
```

## Unwrapping

```csharp
// Remove implicit conversions
operation.UnwrapImplicitConversions()

// Remove all conversions
operation.UnwrapAllConversions()

// Remove parentheses
operation.UnwrapParenthesized()
```

## Value Analysis

```csharp
// Type
operation.GetActualType()                // Underlying type

// Constants
operation.IsConstantZero()               // Is literal 0
operation.IsConstantNull()               // Is null
operation.IsConstant()                   // Is any constant
operation.TryGetConstantValue<T>(out value)  // Get constant value

// Assignment
operation.IsAssignmentTarget()           // Being assigned to
operation.IsLeftSideOfAssignment()       // Left of =
operation.IsPassedByRef()                // ref/out parameter
```

## Language Version

```csharp
operation.GetCSharpLanguageVersion()     // LanguageVersion from tree
```

## InvocationExtensions

For `IInvocationOperation`:

```csharp
// Arguments
invocation.GetArgument("paramName")      // By name
invocation.GetArgument(index)            // By index
invocation.HasArgumentOfType(typeSymbol)
invocation.TryGetConstantArgument<T>(index, out value)
invocation.TryGetStringArgument(index, out value)
invocation.GetExplicitArguments()        // Non-default arguments
invocation.GetArgumentCount()
invocation.HasOptionalArgumentsNotProvided()
invocation.AllArgumentsAreConstant()

// Method identification
invocation.IsMethodNamed("name")
invocation.IsMethodNamed("name", "ContainingType")
invocation.IsExtensionMethodOn(typeSymbol)
invocation.IsInstanceMethodOn(typeSymbol)
invocation.GetReceiverType()

// Method characteristics
invocation.ReturnsVoid()
invocation.IsAsyncMethod()
invocation.HasCancellationTokenParameter()
invocation.IsCancellationTokenPassed()

// Classification
invocation.IsLinqMethod()                // System.Linq
invocation.IsStringMethod()              // System.String
invocation.IsObjectMethod()              // System.Object

// Null-conditional
invocation.IsNullConditionalAccess()     // obj?.Method()
```

## Examples

### Find All Awaited Operations in Method

```csharp
var method = operation.GetContainingMethod();
var awaits = operation
    .Ancestors()
    .First(a => a is IMethodBodyOperation)
    .DescendantsOfType<IAwaitOperation>();
```

### Check if Inside Cancellation-Aware Context

```csharp
bool IsCancellationAware(IOperation op)
{
    var method = op.GetContainingMethod();
    if (method == null) return false;

    return method.Parameters.Any(p =>
        p.Type.GetMetadataName() == "System.Threading.CancellationToken");
}
```

### Analyze LINQ Chain

```csharp
foreach (var invocation in block.DescendantsOfType<IInvocationOperation>())
{
    if (invocation.IsLinqMethod())
    {
        var methodName = invocation.TargetMethod.Name;
        var receiverType = invocation.GetReceiverType();
        // Analyze LINQ usage
    }
}
```

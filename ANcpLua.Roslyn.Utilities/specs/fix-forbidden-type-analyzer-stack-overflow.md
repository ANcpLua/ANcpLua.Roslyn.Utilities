# Spec: Fix ForbiddenTypeAnalyzer Stack Overflow

## Problem

The `ForbiddenTypeAnalyzer.AnalyzeGeneratorRun` method uses recursive traversal via a nested `Visit` function. When analyzing generator outputs with deeply nested object graphs, this causes stack overflow:

```
Stack overflow.
   at System.String.Concat(System.String, System.String, System.String)
   at ANcpLua.Roslyn.Utilities.Testing.ForbiddenTypeAnalyzer.<AnalyzeGeneratorRun>g__Visit|2_0(...)
   at ANcpLua.Roslyn.Utilities.Testing.ForbiddenTypeAnalyzer.<AnalyzeGeneratorRun>g__Visit|2_0(...)
   ... (thousands of frames)
```

This occurs in ErrorOrX when the `AotJsonGenerator` produces outputs that contain deeply nested type structures during property traversal.

## Root Cause

The current implementation (lines 136-169) uses a recursive local function:

```csharp
void Visit(object? node, string step, string path)
{
    // ... check node ...
    foreach (var field in GetRelevantFields(type))
    {
        Visit(field.GetValue(node), step, $"{path}.{field.Name}"); // RECURSIVE!
    }
}
```

Each recursive call:
1. Adds a stack frame (~200-400 bytes)
2. Builds path strings via `$"{path}.{field.Name}"` (allocations)
3. Can go thousands of levels deep before hitting a cycle

The default .NET stack is ~1MB, allowing only ~2,500-5,000 recursive calls before overflow.

## Solution: Convert to Iterative Traversal

Replace recursion with an explicit `Stack<T>` for depth-first traversal:

```csharp
public static IReadOnlyList<ForbiddenTypeViolation> AnalyzeGeneratorRun(GeneratorDriverRunResult run)
{
    List<ForbiddenTypeViolation> violations = [];
    HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
    Stack<(object Node, string Step, string Path)> pending = new();

    // Seed the stack with initial outputs
    foreach (var (stepName, steps) in run.Results.SelectMany(static r => r.TrackedSteps))
    foreach (var step in steps)
    foreach (var (output, _) in step.Outputs)
        if (output is not null)
            pending.Push((output, stepName, "Output"));

    // Iterative traversal
    while (pending.Count > 0 && violations.Count < 256)
    {
        var (node, step, path) = pending.Pop();

        var type = node.GetType();

        // Cycle detection
        if (!type.IsValueType && !visited.Add(node))
            continue;

        if (IsForbiddenType(type))
        {
            violations.Add(new ForbiddenTypeViolation(step, type, path));
            continue;
        }

        if (IsAllowedType(type))
            continue;

        // Handle collections
        if (node is IEnumerable collection and not string)
        {
            var index = 0;
            foreach (var element in collection)
                if (element is not null)
                    pending.Push((element, step, $"{path}[{index++}]"));
            continue;
        }

        // Handle object fields
        foreach (var field in GetRelevantFields(type))
        {
            var value = field.GetValue(node);
            if (value is not null)
                pending.Push((value, step, $"{path}.{field.Name}"));
        }
    }

    return violations;
}
```

## Additional Safety: Depth Limit

Add a configurable maximum depth to prevent pathological cases:

```csharp
private const int MaxTraversalDepth = 100;

// In the work item tuple:
Stack<(object Node, string Step, string Path, int Depth)> pending = new();

// When processing:
if (depth >= MaxTraversalDepth)
    continue; // Skip overly deep structures

// When pushing children:
pending.Push((value, step, $"{path}.{field.Name}", depth + 1));
```

## Benefits

| Aspect | Before (Recursive) | After (Iterative) |
|--------|-------------------|-------------------|
| Stack usage | O(depth) frames | O(1) frames |
| Max depth | ~2,500 | Unlimited (heap) |
| Memory | Stack-limited | Heap-backed Stack<T> |
| Performance | Same | Same (DFS order preserved) |

## Files to Modify

1. **`ANcpLua.Roslyn.Utilities.Testing/ForbiddenTypeAnalyzer.cs`**
   - Replace recursive `Visit` with iterative loop
   - Add `MaxTraversalDepth` constant
   - Update XML docs

## Testing

1. Existing tests should continue to pass
2. Add test with deeply nested structure (1000+ levels)
3. Verify ErrorOrX `AotJsonGeneratorTests` no longer crash

## Implementation Checklist

- [x] Convert `Visit` function to iterative `while` loop
- [x] Add `MaxTraversalDepth` constant (100)
- [x] Add depth tracking to work items
- [x] Update XML documentation
- [x] Add default ImmutableArray check for step.Outputs
- [x] Add default ImmutableArray check for collection enumeration
- [ ] Add unit test for deep nesting
- [ ] Verify ErrorOrX tests pass

## Additional Fix: IsCached Step Filtering

The `IsCached()` method in `GeneratorResult.cs` must filter forbidden type violations by step names when step names are provided:

```csharp
public GeneratorResult IsCached(params string[] stepNames)
{
    var report = CachingReport;

    // When step names are provided, only check forbidden types in those steps
    var violationsToCheck = stepNames.Length > 0
        ? report.ForbiddenTypeViolations.Where(v => stepNames.Contains(v.StepName, StringComparer.Ordinal)).ToList()
        : report.ForbiddenTypeViolations;

    if (violationsToCheck.Count > 0)
    {
        Fail("Forbidden types cached", ViolationFormatter.FormatGrouped(violationsToCheck));
    }

    // ... rest of method
}
```

This allows generators using `ForAttributeWithMetadataName` to test caching of their custom steps without failing on internal Roslyn framework steps.

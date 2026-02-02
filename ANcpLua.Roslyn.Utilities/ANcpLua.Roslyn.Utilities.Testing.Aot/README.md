# ANcpLua.Roslyn.Utilities.Testing.Aot

Attributes and utilities for testing AOT compilation and IL trimming behavior in .NET applications.

## Features

- **[AotTest]** - Marks methods for AOT (Ahead-of-Time) compilation testing
- **[TrimTest]** - Marks methods for IL trimming validation
- **[AotSafe] / [TrimSafe]** - Marks code as verified AOT/trim compatible
- **TrimAssert** - Assertions for verifying type preservation/removal after trimming
- **FeatureSwitches** - Common feature switch constants for AOT scenarios

## Usage

```csharp
using ANcpLua.Roslyn.Utilities.Testing.Aot;

public class MyAotTests
{
    // AOT test - returns 100 for success
    [AotTest]
    public static int BasicAotTest()
    {
        // Test code runs in AOT-compiled binary
        return 100; // Success
    }

    // Trim test with full trimming
    [TrimTest(TrimMode = TrimMode.Full)]
    public static int TrimTest()
    {
        TrimAssert.TypePreserved("System.String", "System.Private.CoreLib");
        return 100;
    }

    // AOT test with disabled feature switches
    [AotTest(DisabledFeatureSwitches = [FeatureSwitches.JsonReflection])]
    public static int AotWithoutJsonReflection()
    {
        // JSON source generation only
        return 100;
    }
}
```

## Requirements

- Target netstandard2.0 or later
- MSBuild orchestration provided by ANcpLua.NET.Sdk (EnableAotTesting=true)

## License

MIT

# Test Projects

The SDK automatically detects and configures test projects with modern testing frameworks.

## Detection

Test projects are detected by:
- Project name ending in `.Tests.csproj`
- Located in `tests/` folder
- `IsTestProject=true` property

## Auto-Injected Packages

All test projects receive:

| Package | Purpose |
|---------|---------|
| `xunit.v3.mtp-v2` | xUnit v3 with Microsoft Testing Platform |
| Parallel test framework | Fast test execution |
| `AwesomeAssertions` | Fluent assertions |
| AwesomeAssertions analyzers | Assertion best practices |
| `GitHubActionsTestLogger` | CI-friendly output (on CI) |
| TRX report generation | Test result reports |

## Integration Tests

Projects in `Integration/` or `E2E/` folders receive additional packages:

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Mvc.Testing` | WebApplicationFactory |
| `IntegrationTestBase.cs` | Base class for integration tests |

## Analyzer Tests

Projects with "Analyzer" in the name receive:

| Package | Purpose |
|---------|---------|
| Analyzer testing packages | Roslyn analyzer verification |
| `AnalyzerTest.cs` | Base class for analyzer tests |
| `CodeFixTest.cs` | Base class for code fix tests |

## Example Test

```csharp
public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsSum()
    {
        var result = Calculator.Add(2, 3);

        result.Should().Be(5);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    public void Add_WithVariousInputs_ReturnsExpected(int a, int b, int expected)
    {
        Calculator.Add(a, b).Should().Be(expected);
    }
}
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~CalculatorTests"
```

## CI Configuration

On CI, tests automatically:
- Use `GitHubActionsTestLogger` for better output
- Generate TRX reports
- Report failures with annotations

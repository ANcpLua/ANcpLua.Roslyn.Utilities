# ANcpLua.Roslyn.Utilities.Testing

Test utilities for Roslyn analyzers and generators. net10.0.

## Usage

```csharp
using var result = await Test<MyGenerator>.Run(source);
result
    .Produces("Output.g.cs", expectedContent)
    .IsCached()
    .IsClean();
// Verify() called automatically on dispose
```

## GeneratorResult Assertions

```csharp
result.Produces(hintName, expected, exactMatch)  // file exists with content
result.Produces(hintName)                        // file exists
result.IsClean()                                 // no diagnostics
result.Compiles()                                // no errors
result.IsCached(stepNames)                       // caching validated
result.HasDiagnostic(id, severity)
result.HasNoDiagnostic(id)
result.HasNoForbiddenTypes()                     // no ISymbol/Compilation cached
result.File(hintName, content => assert)         // custom assertion
result.Verify()                                  // throws if failures
```

## GeneratorResult Properties

```csharp
result.Files                    // IEnumerable<GeneratedFile>
result.Diagnostics              // IReadOnlyList<Diagnostic>
result.CachingReport            // GeneratorCachingReport
result.FirstRun                 // GeneratorDriverRunResult
result.SecondRun                // GeneratorDriverRunResult
result[hintName]                // GeneratedFile?
```

## GeneratorTestEngine

```csharp
var engine = new GeneratorTestEngine<TGenerator>()
    .WithSource(code)
    .WithReference(reference)
    .WithAdditionalText(path, content)
    .WithAnalyzerConfigOptions(provider)
    .WithLanguageVersion(version)
    .WithReferenceAssemblies(assemblies);

var (first, second) = await engine.RunTwiceAsync(ct);
```

## TestConfiguration

```csharp
TestConfiguration.LanguageVersion          // AsyncLocal, default Preview
TestConfiguration.ReferenceAssemblies      // AsyncLocal, default net10.0
TestConfiguration.AdditionalReferences     // AsyncLocal

using (TestConfiguration.WithLanguageVersion(version)) { }
using (TestConfiguration.WithReferenceAssemblies(assemblies)) { }
```

## Caching Analysis

```csharp
// GeneratorCachingReport
report.GeneratorName
report.ObservableSteps              // user pipeline steps
report.ForbiddenTypeViolations
report.ProducedOutput

// GeneratorStepAnalysis
step.StepName
step.Cached / Unchanged / Modified / New / Removed
step.HasForbiddenTypes
step.IsCachedSuccessfully
step.FormatBreakdown()

// ForbiddenTypeViolation
violation.StepName
violation.ForbiddenType
violation.Path
```

## Files

- `Test.cs` - entry point + TextUtilities
- `GeneratorResult.cs` - result + assertions + GeneratedFile record
- `GeneratorTestEngine.cs` - compilation + driver execution
- `TestConfiguration.cs` - thread-safe config
- `GeneratorCachingReport.cs` - caching report factory
- `GeneratorStepAnalyzer.cs` - step extraction
- `ForbiddenTypeAnalyzer.cs` - ISymbol/Compilation detection
- `Analysis/StepClassification.cs` - GeneratorStepAnalysis + step classification
- `Formatting/ReportFormatter.cs` - failure formatting + ViolationFormatter
- `Formatting/AssertionHelpers.cs` - message helpers + StepFormatter

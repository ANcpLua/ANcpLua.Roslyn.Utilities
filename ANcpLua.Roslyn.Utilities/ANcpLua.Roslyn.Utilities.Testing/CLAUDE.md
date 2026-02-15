# ANcpLua.Roslyn.Utilities.Testing

**Test utilities for Roslyn analyzers and source generators.** Target: `net10.0`.

This library provides comprehensive testing infrastructure for Roslyn tooling, including generator validation, analyzer testing, MSBuild integration testing, and dynamic compilation.

---

## Quick Reference

| Category | Key Types |
|----------|-----------|
| **Generator Testing** | `Test<TGenerator>`, `GeneratorResult`, `GeneratorTestEngine<TGenerator>` |
| **Analyzer Testing** | `AnalyzerTest<TAnalyzer>`, `CodeFixTest<TAnalyzer, TCodeFix>` |
| **MSBuild Testing** | `ProjectBuilder`, `BuildResult`, `BuildResultAssertions` |
| **Dynamic Compilation** | `Compile`, `CompileResult`, `CompileResultAssertions` |
| **Log Testing** | `LogAssert` |
| **Configuration** | `TestConfiguration` |

---

## Generator Testing

### Quick Start

```csharp
using var result = await Test<MyGenerator>.Run(source);
result
    .Produces("Output.g.cs", expectedContent)
    .IsCached()
    .IsClean();
// Verify() called automatically on dispose
```

### GeneratorResult Assertions

```csharp
result.Produces(hintName, expected, exactMatch)  // file exists with content
result.Produces(hintName)                        // file exists
result.IsClean()                                 // no diagnostics
result.Compiles()                                // no errors
result.IsCached(stepNames)                       // caching validated (all steps, names = existence check)
result.HasDiagnostic(id, severity)
result.HasNoDiagnostic(id)
result.HasNoForbiddenTypes()                     // no ISymbol/Compilation cached
result.File(hintName, content => assert)         // custom assertion
result.Verify()                                  // throws if failures
```

### GeneratorResult Properties

```csharp
result.Files                    // IEnumerable<GeneratedFile>
result.Diagnostics              // IReadOnlyList<Diagnostic>
result.CachingReport            // GeneratorCachingReport
result.FirstRun                 // GeneratorDriverRunResult
result.SecondRun                // GeneratorDriverRunResult
result[hintName]                // GeneratedFile?
```

### GeneratorTestEngine

For more control over the test setup:

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

### TestConfiguration

Thread-safe global configuration via `AsyncLocal`:

```csharp
TestConfiguration.LanguageVersion          // AsyncLocal, default Preview
TestConfiguration.ReferenceAssemblies      // AsyncLocal, default net10.0
TestConfiguration.AdditionalReferences     // AsyncLocal

using (TestConfiguration.WithLanguageVersion(version)) { }
using (TestConfiguration.WithReferenceAssemblies(assemblies)) { }
```

---

## Analyzer Testing

### AnalyzerTest

Base class for analyzer-only tests:

```csharp
public class MyAnalyzerTests : AnalyzerTest<MyAnalyzer>
{
    [Fact]
    public async Task Test() => await Verify("""
        class C { [|string|] s; }  // [|...|] marks expected diagnostic span
        """);
}
```

### CodeFixTest

Base class for analyzer + code fix tests:

```csharp
public class MyCodeFixTests : CodeFixTest<MyAnalyzer, MyCodeFix>
{
    [Fact]
    public async Task Test() => await Verify(
        source: """class C { [|string|] s; }""",
        fixedSource: """class C { string? s; }""");
}
```

### CodeFixTestWithEditorConfig

Tests with EditorConfig support:

```csharp
public class MyTests : CodeFixTestWithEditorConfig<MyAnalyzer, MyCodeFix>
{
    protected override string EditorConfig => """
        [*.cs]
        my_option = value
        """;
}
```

### RefactoringTest

Base class for refactoring provider tests:

```csharp
public class MyRefactoringTests : RefactoringTest<MyRefactoringProvider>
{
    [Fact]
    public async Task Test() => await Verify(
        source: """class C { [||]void M() { } }""",  // [||] marks cursor position
        fixedSource: """class C { async Task M() { } }""");
}
```

---

## MSBuild Integration Testing

### ProjectBuilder

Fluent builder for isolated .NET project tests:

```csharp
await using var builder = new ProjectBuilder(testOutputHelper);

var result = await builder
    .WithTargetFramework(Tfm.Net100)
    .WithOutputType(Val.Library)
    .WithLangVersion(Val.Latest)
    .WithProperty(Prop.Nullable, Val.Enable)
    .WithPackage("Microsoft.CodeAnalysis.CSharp", "4.12.0")
    .AddSource("Program.cs", "namespace Test; public class Foo { }")
    .BuildAsync();

result.ShouldSucceed();
```

### ProjectBuilder Methods

```csharp
// Configuration
builder.WithDotnetSdkVersion(NetSdkVersion.Net100)
builder.WithTargetFramework(Tfm.Net100)
builder.WithOutputType(Val.Library)
builder.WithLangVersion(Val.Latest)
builder.WithProperty(name, value)
builder.WithProperties((key, value), ...)
builder.WithPackage(name, version)
builder.WithRootSdk("Microsoft.NET.Sdk.Web")
builder.WithFilename("MyProject.csproj")
builder.WithMtpMode()                         // Microsoft Testing Platform

// Files
builder.AddSource(filename, content)
builder.AddFile(relativePath, content)
builder.WithDirectoryBuildProps(content)
builder.WithDirectoryPackagesProps(content)

// NuGet
builder.WithNuGetConfig(content)
builder.WithPackageSource(name, path, patterns...)

// Execute
await builder.BuildAsync(args, envVars)
await builder.RunAsync(args, envVars)
await builder.TestAsync(args, envVars)
await builder.PackAsync(args, envVars)
await builder.RestoreAsync(args, envVars)
await builder.ExecuteDotnetCommandAsync(command, args, envVars)

// Properties
builder.RootFolder                            // FullPath to temp directory
builder.GitHubEnvironmentVariables            // CI simulation
builder.GetGitHubStepSummaryContent()
```

### BuildResult

```csharp
result.Succeeded
result.Failed
result.ExitCode
result.Output                                 // ProcessOutputCollection

// SARIF diagnostics
result.HasError()
result.HasError(ruleId)
result.HasWarning()
result.HasWarning(ruleId)
result.HasNote(ruleId)
result.HasInfo(ruleId)
result.GetAllDiagnostics()
result.GetErrors()
result.GetWarnings()

// Output inspection
result.OutputContains(text, comparison)
result.OutputDoesNotContain(text, comparison)

// Binary log inspection
result.GetBinLogFiles()
result.GetMsBuildItems(name)
result.GetMsBuildPropertyValue(name)
result.IsMsBuildTargetExecuted(name)
```

### BuildResultAssertions

Fluent assertions for build results:

```csharp
result.ShouldSucceed(because)
result.ShouldFail(because)
result.ShouldHaveWarning(ruleId)
result.ShouldNotHaveWarning(ruleId)
result.ShouldHaveError(ruleId)
result.ShouldNotHaveError(ruleId)
result.ShouldContainOutput(text)
result.ShouldNotContainOutput(text)
result.ShouldHavePropertyValue(name, value, ignoreCase)
result.ShouldHaveExecutedTarget(targetName)
result.ShouldNotHaveExecutedTarget(targetName)
```

### MSBuild Constants

Strongly-typed constants for MSBuild projects:

```csharp
// Target Frameworks (Tfm)
Tfm.NetStandard20, Tfm.NetStandard21
Tfm.Net60, Tfm.Net70, Tfm.Net80, Tfm.Net90, Tfm.Net100

// Property Names (Prop)
Prop.TargetFramework, Prop.TargetFrameworks, Prop.OutputType
Prop.Nullable, Prop.ImplicitUsings, Prop.LangVersion
Prop.TreatWarningsAsErrors, Prop.IsPackable, Prop.GenerateDocumentationFile
Prop.ManagePackageVersionsCentrally, Prop.CentralPackageTransitivePinningEnabled
Prop.Version, Prop.PackageId, Prop.Authors, Prop.Description
// ... and more

// Property Values (Val)
Val.Library, Val.Exe, Val.WinExe
Val.True, Val.False, Val.Enable, Val.Disable
Val.Latest, Val.Preview, Val.All, Val.None, Val.Snupkg

// Item Names (Item)
Item.PackageReference, Item.PackageVersion, Item.ProjectReference
Item.Compile, Item.Content, Item.None, Item.EmbeddedResource
Item.InternalsVisibleTo, Item.Using, Item.Analyzer

// Attribute Names (Attr)
Attr.Include, Attr.Exclude, Attr.Remove, Attr.Update
Attr.Version, Attr.VersionOverride
Attr.PrivateAssets, Attr.IncludeAssets, Attr.ExcludeAssets
Attr.Condition, Attr.Label

// XML Snippet Builder
XmlSnippetBuilder.TargetFramework(tfm)
XmlSnippetBuilder.LangVersion(version)
XmlSnippetBuilder.OutputType(type)
XmlSnippetBuilder.Property(name, value)
```

### RepositoryRoot

Locate repository root directory:

```csharp
var root = RepositoryRoot.Locate();           // Finds *.sln, *.slnx, or .git
var root = RepositoryRoot.Locate("package.json", ".git");
FullPath path = root["src/MyProject"];        // Indexer for relative paths
```

### DotNetSdkHelpers

Automatic SDK download and caching:

```csharp
var dotnetPath = await DotNetSdkHelpers.Get(NetSdkVersion.Net100);
DotNetSdkHelpers.ClearCache();                // Clear in-memory cache
```

---

## Dynamic Compilation

### Compile

Quick compilation for unit tests:

```csharp
// Basic compilation with assertions
Compile.Source(code)
    .WithCommonReferences()
    .Build()
    .ShouldSucceed();

// Get assembly directly (throws on failure)
var assembly = Compile.Source(code).BuildOrThrow();

// Create instance from compiled code
var instance = Compile.Source(code)
    .Build()
    .ShouldSucceed()
    .CreateInstance<IGreeter>("Greeter");
```

### Compile Builder Methods

```csharp
Compile.Source(code)                          // Create with source
Compile.Empty()                               // Create empty
    .WithSource(code)                         // Add source
    .WithSources(code1, code2)                // Add multiple sources
    .WithReference<T>()                       // Reference from type
    .WithReference(assembly)                  // Reference from assembly
    .WithReference(path)                      // Reference from path
    .WithReferences(types)                    // Multiple type references
    .WithCommonReferences()                   // Console, Linq, Collections
    .WithAssemblyName(name)
    .WithOutputKind(kind)
    .AsExecutable()                           // ConsoleApplication
    .WithLanguageVersion(version)
    .WithOptimization()                       // Release mode
    .WithUnsafe()                             // Allow unsafe
    .Build()                                  // Returns CompileResult
    .BuildOrThrow()                           // Returns Assembly
```

### CompileResult

```csharp
result.Succeeded
result.Failed
result.Assembly                               // Loaded assembly if succeeded
result.Compilation                            // CSharpCompilation
result.Diagnostics                            // All diagnostics
result.Errors                                 // Error diagnostics
result.Warnings                               // Warning diagnostics

// Query methods
result.HasError(errorId)
result.HasWarning(warningId)
result.ContainsType(typeName)
result.GetType(name)
result.GetRequiredType(name)                  // Throws if not found
result.CreateInstance(typeName)
result.CreateInstance<T>(typeName)
result.CreateRequiredInstance<T>(typeName)    // Throws if null
result.FormatDiagnostics()
result.GetSourceText()                        // Combined source from all trees
result.GetSemanticModel()                     // SemanticModel for first tree
```

### CompileResultAssertions

```csharp
result.ShouldSucceed(because)
result.ShouldFail(because)
result.ShouldHaveNoWarnings()
result.ShouldHaveError(errorId)
result.ShouldHaveWarning(warningId)
result.ShouldHaveErrors(count)
result.ShouldContainType(typeName)
```

---

## Log Testing

### LogAssert

Fluent assertions for `FakeLogCollector`:

```csharp
// Fluent chaining
collector
    .ShouldHaveCount(3)
    .ShouldContain("started")
    .ShouldHaveNoErrors();

// Async waiting
await collector.ShouldEventuallyContain("completed");
```

### LogAssert Methods

```csharp
// Count assertions
collector.ShouldHaveCount(expected)
collector.ShouldHaveExactCount(expected)
collector.ShouldBeEmpty()

// Content assertions
collector.ShouldContain(text, comparison)
collector.ShouldNotContain(text, comparison)
collector.ShouldMatch(pattern)                // Regex

// Level assertions
collector.ShouldHaveLevel(level)
collector.ShouldNotHaveLevel(level)
collector.ShouldHaveNoErrors()
collector.ShouldHaveNoWarnings()
collector.ShouldBeClean()                     // No errors or warnings

// Combined assertions
collector.ShouldHave(level, containsText)

// Predicate assertions
collector.ShouldHaveAny(predicate, because)
collector.ShouldHaveAll(predicate, because)
collector.ShouldHaveNone(predicate, because)

// Async waiting
await collector.ShouldEventuallyContain(text, timeout, ct)
await collector.ShouldEventuallyHaveCount(count, timeout, ct)
await collector.ShouldEventuallyHaveLevel(level, timeout, ct)
await collector.ShouldEventuallySatisfy(condition, because, timeout, ct)

// Utility
collector.FormatLogs()                        // For display
```

---

## Caching Analysis

### GeneratorCachingReport

Report on generator caching behavior:

```csharp
report.GeneratorName
report.ObservableSteps              // user pipeline steps
report.ForbiddenTypeViolations
report.ProducedOutput
```

### GeneratorStepAnalysis

Per-step caching analysis:

```csharp
step.StepName
step.Cached, step.Unchanged, step.Modified, step.New, step.Removed
step.HasForbiddenTypes
step.IsCachedSuccessfully                    // Cached + Unchanged = OK
step.IsTrulyCached                           // Cached only (no re-computation)
step.FormatBreakdown()
```

### ForbiddenTypeViolation

Detected caching violations:

```csharp
violation.StepName
violation.ForbiddenType
violation.Path
```

---

## Instrumentation

OpenTelemetry integration for test observability:

### ActivityInstrumentation

```csharp
var activity = ActivityInstrumentation.StartActivity("TestName");
activity?.SetTag("key", "value");
```

### MetricsInstrumentation

```csharp
MetricsInstrumentation.RecordTestDuration(testName, duration);
MetricsInstrumentation.IncrementCounter(name);
```

### LogEnricherInfrastructure

Structured logging enrichment for tests.

---

## File Structure

```
ANcpLua.Roslyn.Utilities.Testing/
├── Test.cs                      # Generator test entry point
├── GeneratorResult.cs           # Result + assertions + GeneratedFile record
├── GeneratorTestEngine.cs       # Compilation + driver execution
├── TestConfiguration.cs         # Thread-safe config (AsyncLocal)
├── GeneratorCachingReport.cs    # Caching report factory
├── GeneratorStepAnalyzer.cs     # Step extraction from driver results
├── ForbiddenTypeAnalyzer.cs     # ISymbol/Compilation detection
├── TextUtilities.cs             # Text normalization utilities
├── AnalyzerTest.cs              # Base class for analyzer tests
├── CodeFixTest.cs               # Base class for code fix tests
├── CodeFixTestWithEditorConfig.cs  # Code fix tests with EditorConfig
├── RefactoringTest.cs           # Base class for refactoring tests
├── Compile.cs                   # Dynamic compilation + CompileResult + assertions
├── LogAssert.cs                 # FakeLogCollector assertions
├── Analysis/
│   └── StepClassification.cs    # GeneratorStepAnalysis + classification
├── Formatting/
│   ├── ReportFormatter.cs       # Failure formatting + ViolationFormatter
│   └── AssertionHelpers.cs      # Message helpers + StepFormatter
├── Instrumentation/
│   ├── ActivityInstrumentation.cs
│   ├── MetricsInstrumentation.cs
│   ├── LogEnricherInfrastructure.cs
│   ├── LoggingConventions.cs
│   └── DataClassificationHelpers.cs
└── MSBuild/
    ├── ProjectBuilder.cs        # Fluent MSBuild project builder
    ├── DotNetSdkHelpers.cs      # SDK download/cache + NetSdkVersion enum
    ├── MSBuildConstants.cs      # Tfm, Prop, Val, Item, Attr, XmlSnippetBuilder
    ├── BuildResult.cs           # Build result + SARIF parsing
    ├── BuildResultAssertions.cs # Fluent assertions for BuildResult
    └── RepositoryRoot.cs        # Repository root locator
```

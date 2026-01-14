# ANcpLua.Roslyn.Utilities.Testing

Test utilities for Roslyn analyzers and generators. net10.0.

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
result.IsCached(stepNames)                       // caching validated
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

```csharp
TestConfiguration.LanguageVersion          // AsyncLocal, default Preview
TestConfiguration.ReferenceAssemblies      // AsyncLocal, default net10.0
TestConfiguration.AdditionalReferences     // AsyncLocal

using (TestConfiguration.WithLanguageVersion(version)) { }
using (TestConfiguration.WithReferenceAssemblies(assemblies)) { }
```

## Analyzer Testing

### AnalyzerTest

```csharp
public class MyAnalyzerTests : AnalyzerTest<MyAnalyzer>
{
    [Fact]
    public async Task Test() => await Verify("""
        class C { [|string|] s; }  // [|...|] marks the span of an expected diagnostic
        """);
}
```

### CodeFixTest

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

```csharp
public class MyTests : CodeFixTestWithEditorConfig<MyAnalyzer, MyCodeFix>
{
    protected override string EditorConfig => """
        [*.cs]
        my_option = value
        """;
}
```

## MSBuild Integration Testing

### ProjectBuilder

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

```csharp
// Target Frameworks (Tfm)
Tfm.NetStandard20, Tfm.NetStandard21
Tfm.Net60, Tfm.Net70, Tfm.Net80, Tfm.Net90, Tfm.Net100

// Property Names (Prop)
Prop.TargetFramework, Prop.TargetFrameworks, Prop.OutputType
Prop.Nullable, Prop.ImplicitUsings, Prop.LangVersion
Prop.TreatWarningsAsErrors, Prop.IsPackable
Prop.ManagePackageVersionsCentrally, Prop.Version, Prop.PackageId
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

```csharp
var root = RepositoryRoot.Locate();           // Finds *.sln, *.slnx, or .git
var root = RepositoryRoot.Locate("package.json", ".git");
FullPath path = root["src/MyProject"];        // Indexer for relative paths
```

### DotNetSdkHelpers

```csharp
var dotnetPath = await DotNetSdkHelpers.Get(NetSdkVersion.Net100);
DotNetSdkHelpers.ClearCache();                // Clear in-memory cache
```

## Caching Analysis

### GeneratorCachingReport

```csharp
report.GeneratorName
report.ObservableSteps              // user pipeline steps
report.ForbiddenTypeViolations
report.ProducedOutput
```

### GeneratorStepAnalysis

```csharp
step.StepName
step.Cached, step.Unchanged, step.Modified, step.New, step.Removed
step.HasForbiddenTypes
step.IsCachedSuccessfully
step.FormatBreakdown()
```

### ForbiddenTypeViolation

```csharp
violation.StepName
violation.ForbiddenType
violation.Path
```

## Files

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
├── Analysis/
│   └── StepClassification.cs    # GeneratorStepAnalysis + classification
├── Formatting/
│   ├── ReportFormatter.cs       # Failure formatting + ViolationFormatter
│   └── AssertionHelpers.cs      # Message helpers + StepFormatter
└── MSBuild/
    ├── ProjectBuilder.cs        # Fluent MSBuild project builder
    ├── DotNetSdkHelpers.cs      # SDK download/cache + NetSdkVersion enum
    ├── MSBuildConstants.cs      # Tfm, Prop, Val, Item, Attr, XmlSnippetBuilder
    ├── BuildResult.cs           # Build result + SARIF parsing
    ├── BuildResultAssertions.cs # Fluent assertions for BuildResult
    └── RepositoryRoot.cs        # Repository root locator
```

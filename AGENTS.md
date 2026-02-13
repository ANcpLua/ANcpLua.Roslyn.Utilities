# AGENTS.md

> Compressed context for Roslyn utilities. Always loaded, always present.
>
> **IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning.**
> **ALWAYS use ANcpLua.Roslyn.Utilities first. Do not write raw Roslyn helpers.**
> Read the relevant CLAUDE.md before implementing.

## Repository

ANcpLua.Roslyn.Utilities | LAYER 0 (upstream source of truth) | netstandard2.0
Packages: Core (utilities), Testing (test infra), Sources (compile-time).
No dependency on ANcpLua.NET.Sdk (circular). Publish here before SDK updates.

## Decision Tree

```text
IF validating arguments
  -> Guard.NotNull/NotNullOrEmpty/NotNegative/Positive/DefinedEnum (Guard.cs)
  -> Guard.NotNullOrElse() for fallback (eager or lazy)
  -> Guard.Unreachable<T>() for switch exhaustiveness

IF matching symbol patterns (method/type/property/field)
  -> Match.Method().Named().Async().Public().Matches() (Matching/SymbolMatch.cs)
  -> Match.Type().Class().Implements().Matches()
  -> IMPORTANT: Matchers mutate. Create a fresh matcher per pattern.

IF matching invocation operations
  -> Invoke.Method("Name").OnType("T").Matches() (Matching/InvocationMatch.cs)
  -> Invoke.Method().Linq().Named("Where").Matches()

IF accumulating errors in generator pipeline
  -> DiagnosticFlow<T>.Ok/Fail -> .Then() -> .Select() -> .Where() (DiagnosticFlow.cs)
  -> Use IncrementalValuesProviderExtensions flow helpers

IF caching arrays in source generator
  -> EquatableArray<T> wraps ImmutableArray with value equality (EquatableArray.cs)
  -> All incremental collections use EquatableArray

IF need well-known type checks
  -> AwaitableContext (Task/ValueTask)
  -> AspNetContext (controllers/actions)
  -> DisposableContext (IDisposable/IAsyncDisposable)
  -> CollectionContext (IEnumerable)
  -> Create once per compilation, reuse for all checks

IF working with collections
  -> .OrEmpty() .WhereNotNull() .ToImmutableArrayOrEmpty() (EnumerableExtensions.cs)

IF building generated source
  -> IndentedStringBuilder for structured code (CodeGeneration.cs)
  -> ValueStringBuilder for stack-allocated strings (ValueStringBuilder.cs)

IF writing an analyzer
  -> Inherit DiagnosticAnalyzerBase (Analyzers/DiagnosticAnalyzerBase.cs)

IF writing a code fix
  -> Inherit CodeFixProviderBase<T> (CodeFixes/CodeFixProviderBase.cs)
  -> Use SyntaxModifierExtensions for transforms

IF writing tests
  -> Read Testing/CLAUDE.md for Test<T>, AnalyzerTest, CodeFixTest patterns
  -> MSBuild integration: ProjectBuilder/BuildResult (Testing/MSBuild/)

IF comparing strings
  -> .EqualsOrdinal() .ContainsOrdinal() (StringComparisonExtensions.cs)
  -> Never use bare == for string comparison

IF safe casting/conversion
  -> ObjectExtensions.As<T>() for safe cast
  -> TryExtensions.TryParse*() for parse with null on failure
  -> NullableExtensions.Select() for nullable transforms
```

## Compressed Docs Index

```text
[CLAUDE.md Deep Docs]|root: .
|IMPORTANT: Read before writing ANY code. Check utility catalog first.
|CLAUDE.md (ecosystem, utility catalog, helper selection guide)
|ANcpLua.Roslyn.Utilities/CLAUDE.md (full API reference for all utilities)
|ANcpLua.Roslyn.Utilities.Testing/CLAUDE.md (test infrastructure reference)

[Core]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities
|Guard.cs|SemanticGuard.cs|DiagnosticFlow.cs|EquatableArray.cs
|HashCombiner.cs|ValueStringBuilder.cs|CodeGeneration.cs

[Matching DSL]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/Matching
|SymbolMatch.cs|InvocationMatch.cs

[Extensions]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities
|SymbolExtensions.cs|TypeSymbolExtensions.cs|MethodSymbolExtensions.cs
|OperationExtensions.cs|InvocationExtensions.cs|SyntaxExtensions.cs
|CompilationExtensions.cs|EnumerableExtensions.cs|NullableExtensions.cs
|StringExtensions.cs|StringComparisonExtensions.cs|TryExtensions.cs
|DictionaryExtensions.cs|ListExtensions.cs|ObjectExtensions.cs
|ConvertExtensions.cs|AttributeExtensions.cs|DocumentationExtensions.cs
|LocationExtensions.cs|NamespaceExtensions.cs|LanguageVersionExtensions.cs

[Contexts]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities/Contexts
|AwaitableContext.cs|AspNetContext.cs|DisposableContext.cs|CollectionContext.cs

[Pipeline]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities
|IncrementalValuesProviderExtensions.cs|SyntaxValueProviderExtensions.cs
|SourceProductionContextExtensions.cs

[Infrastructure]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities
|Analyzers/DiagnosticAnalyzerBase.cs|CodeFixes/CodeFixProviderBase.cs
|CodeFixes/SyntaxModifierExtensions.cs

[Testing]|root: ./ANcpLua.Roslyn.Utilities/ANcpLua.Roslyn.Utilities.Testing
|Test.cs|AnalyzerTest.cs|CodeFixTest.cs|RefactoringTest.cs
|GeneratorTestEngine.cs|GeneratorResult.cs|Compile.cs|LogAssert.cs
|MSBuild/{ProjectBuilder.cs,BuildResult.cs,PackageTestBase.cs}
|Instrumentation/{ActivityInstrumentation.cs,MetricsInstrumentation.cs}
```

## Build

```bash
dotnet build -c Release
dotnet pack -c Release
dotnet test
```

# ANcpLua.Roslyn.Utilities Public API

## Packages
- `ANcpLua.Roslyn.Utilities` (netstandard2.0) – helper extensions and value types for incremental source generators. Polyfills for modern language features are injected via `ANcpLua.NET.Sdk`.
- `ANcpLua.Roslyn.Utilities.Testing` (net10.0) – fluent test harness for incremental generators with caching/forbidden-type validation.

## Architectural Boundaries
- **Public API (external)**: extension methods over Roslyn types, equatable file/diagnostic models, and generator pipeline helpers. These are what generator authors should depend on.
- **Contract layer (stability focus)**: value-equality models used inside incremental pipelines to keep caching deterministic (`EquatableArray<T>`, `FileWithName`, `ResultWithDiagnostics<T>`, `DiagnosticInfo`, `LocationInfo`, `EquatableMessageArgs`). Keep these additive-only.
- **Implementation/internal**: helpers that do not need long-term compatibility (string/collection utilities, pooling, numeric helpers). These can move under an internal namespace if surface reduction is desired.
- **Testing surface**: `Test<TGenerator>`, `GeneratorTestEngine<TGenerator>`, `GeneratorResult`, `GeneratorCachingReport`, `GeneratorStepAnalysis`, `ForbiddenTypeViolation`, and `TestConfiguration` form the public testing contract; other types remain internal.

## Main Library API (`ANcpLua.Roslyn.Utilities`)

### Value/Contract Models
- `EquatableArray<T>`: value-equal wrapper over `ImmutableArray<T>` with indexer, `IsDefault`, `IsDefaultOrEmpty`, `IsEmpty`, `Length`, `AsImmutableArray()`, `AsSpan()`, `ToArray()`, `ToImmutableArray()`, `GetEnumerator()`, implicit cast to `ImmutableArray<T>`, equality operators.
- `EquatableArray` (static): `AsEquatableArray<T>(ImmutableArray<T>)`, `FromImmutableArray<T>(ImmutableArray<T>)`.
- `FileWithName` record: `Name`, `Text`, `Empty`, `IsEmpty`.
- `LocationInfo` record: `From(Location|SyntaxNode|SyntaxToken)`, `ToLocation()`.
- `DiagnosticInfo` record: `Create` overloads for token/node/location/args, `ToDiagnostic()`.
- `EquatableMessageArgs` record: `Args`, `Empty`.
- `ResultWithDiagnostics<T>` record: `Result`, `Diagnostics`; `ResultWithDiagnosticsExtensions.ToResultWithDiagnostics()` overloads.
- `ClassWithAttributesContext` record (Roslyn symbols; use only in initial transforms to avoid caching issues).

### Generator Pipeline Helpers
- `IncrementalValuesProviderExtensions`: `AddSource` (value/values providers of `FileWithName`), `AddSources` (value/values providers of `EquatableArray<FileWithName>`), `CollectAsEquatableArray<T>()`, `SelectAndReportExceptions` (value/values providers; `Func<T, CancellationToken, TResult>` and `Func<T, TResult>` overloads), `SelectAndReportDiagnostics` (value/values providers of `ResultWithDiagnostics<T?>`), `WhereNotNull` (class/struct overloads).
- `SyntaxValueProviderExtensions`: `ForAttributeWithMetadataNameOfClassesAndRecords()`, `SelectAllAttributes()`, `SelectManyAllAttributesOfCurrentClassSyntax()`, `DetectVersion()` for RecognizeFramework_Version.
- `SourceProductionContextExtensions`: `AddSourceWithHeader` (string or `StringBuilder`), `ReportDiagnostic(DiagnosticInfo)`, `ReportDiagnostics(EquatableArray<DiagnosticInfo>)`, `ReportDiagnostics<T>(ResultWithDiagnostics<T>)`, `ReportException(string id, Exception, string? prefix)`, `Exception.ToDiagnostic()`.
- `CompilationExtensions`: `HasLanguageVersionAtLeastEqualTo()`, `HasAccessibleTypeWithMetadataName()`, `IsNet9OrGreater()`, `GetBestTypeByMetadataName()`.
- `AnalyzerConfigOptionsProviderExtensions`: `GetValueOrNull()`, `GetGlobalProperty()`, `GetAdditionalTextMetadata()`, `GetRequiredGlobalProperty()`, `GetRequiredAdditionalTextMetadata()`, `TryGetGlobalBool()`, `GetGlobalBoolOrDefault()`, `TryGetGlobalInt()`, `GetGlobalIntOrDefault()`, `IsDesignTimeBuild()`.
- `AnalyzerOptionsExtensions`: `TryGetConfigurationValue()`, `GetConfigurationValue()` overloads for `bool`, `int`, `string`, and `enum`.
- `SemanticModelExtensions`: `IsConstant()`, `AllConstant()`, `GetConstantValueOrDefault<T>()`, `IsNullConstant()`.

### Symbol/Type Introspection
- `TypeSymbolExtensions`: `GetAllInterfacesIncludingThis()`, `InheritsFrom()`, `Implements()`, `IsOrImplements()`, `GetAttributes/GetAttribute/HasAttribute()` by type symbol, `IsOrInheritsFrom()`, `IsEqualToAny()`, primitive/type checks (`IsObject`, `IsString`, `IsChar`, `IsInt32`, `IsInt64`, `IsBoolean`, `IsDateTime`, `IsByte`, `IsSByte`, `IsInt16`, `IsUInt16`, `IsUInt32`, `IsUInt64`, `IsSingle`, `IsDouble`, `IsDecimal`), `IsEnumeration()/GetEnumerationType()`, `IsNumberType()`, `GetUnderlyingNullableTypeOrSelf()`, `IsUnitTestClass()`, `IsPotentialStatic()`, record/struct checks (`IsRecord`, `IsReadOnlyStruct`, `IsRefStruct`), sequence/task checks (`IsSpanType`, `IsMemoryType`, `IsTaskType`, `IsEnumerableType`), `GetElementType()`.
- `SymbolExtensions`: `IsEqualTo()`, `GetFullyQualifiedName()`, `GetMetadataName()`, `HasAttribute()/GetAttribute()` by string name, `IsVisibleOutsideOfAssembly()`, `IsOperator()`, `IsConst()`, `GetAllMembers()` overloads (with name support), `IsTopLevelStatement()`, `GetSymbolType()`, `GetNamespaceName()`, `GetMethod()`, `GetProperty()`, `ExplicitlyImplements()`, `IsDefinition()`.
- `MethodSymbolExtensions`: `IsInterfaceImplementation()` (method/property/event), `GetImplementingInterfaceSymbol()`, `IsOrOverrideMethod()`, `OverridesMethod()`.
- `NamespaceSymbolExtensions`: `IsNamespace(string[] parts)`, `IsNamespace(string namespaceName)`.
- `LocationExtensions`: `GetLineSpan()` overloads for token/node/trivia/node-or-token, `GetLine()` overloads, `GetEndLine()` overloads, `SpansMultipleLines()` for node/trivia.
- `SyntaxExtensions`: `GetMethodName()`, `GetIdentifierName()`, `HasModifier()`, `IsPartial()`, `IsPrimaryConstructorType()`, `GetNameLocation()` for type/method declarations.
- `LanguageVersionExtensions`: `IsCSharp8OrAbove()`, `IsCSharp9OrAbove()`, `IsCSharp10OrAbove()`, `IsCSharp10OrBelow()`, `IsCSharp11OrAbove()`, `IsCSharp12OrAbove()`, `IsCSharp13OrAbove()`, `IsCSharp14OrAbove()`.

### Conversion/Text Utilities
- `ConvertExtensions`: `ToBoolean()`, `ToNullableBoolean()`, `ToEnum<T>(defaultValue)`, `ToEnum<T?>()`.
- `EnumerableExtensions`: `Inject()`, `SelectManyOrEmpty()`, `OrEmpty()`, `ToImmutableArrayOrEmpty()`, `HasDuplicates()` overloads, `SingleOrDefaultIfMultiple()`, `WhereNotNull()` overloads for reference/value types.
- `StringExtensions`: `SplitLines(string|ReadOnlySpan<char>)` with `LineSplitEnumerator`/`LineSplitEntry` structs, `ToPropertyName()`, `ToParameterName()` (keyword-safe), `TrimBlankLines()`, `NormalizeLineEndings()`, `ExtractNamespace()`, `ExtractSimpleName()`, `WithGlobalPrefix()`.
- `NumericHelpers`: `IsZero(object? value)`.

### Pooling
- `ObjectPool<T>` abstract (`Get()`, `Return()`), static `ObjectPool` factory (`SharedStringBuilderPool`, `Create<T>()`, `CreateStringBuilderPool()`), `IPooledObjectPolicy<T>`, `ObjectPoolProvider`, `DefaultObjectPool<T>`, `DefaultObjectPoolProvider`, `DefaultPooledObjectPolicy<T>`, `PooledObjectPolicy<T>`, `IResettable`, `DisposableObjectPool<T>`, `StringBuilderPooledObjectPolicy`.

## Testing Library API (`ANcpLua.Roslyn.Utilities.Testing`)
- `Test<TGenerator>`: `Run(string source, CancellationToken)`; `Run(Action<GeneratorTestEngine<TGenerator>>, CancellationToken)`.
- `GeneratorTestEngine<TGenerator>`: `WithSource()`, `WithReference()`, `WithAdditionalText()`, `WithAnalyzerConfigOptions()`, `WithLanguageVersion()`, `WithReferenceAssemblies()`, `WithoutStepTracking()`, `GetCompilationAsync()`, `RunAsync()`, `RunTwiceAsync()`.
- `GeneratorResult`: accessors `Files`, `Diagnostics`, `CachingReport`, `FirstRun`, `SecondRun`, indexer by hint name; assertions `Produces(hintName[, expectedContent, exactMatch])`, `IsClean()`, `Compiles()`, `IsCached(params string[] stepNames)`, `HasDiagnostic(id, severity?)`, `HasNoDiagnostic(id)`, `HasNoForbiddenTypes()`, `File(hintName, Action<string>)`, `Verify()` (via `Dispose()`).
- `GeneratedFile` record: `HintName`, `Content`, helpers `Contains(string)`, `Matches(string)`.
- `GeneratorAssertionException` exception type.
- `GeneratorCachingReport`: properties `GeneratorName`, `ObservableSteps`, `SinkSteps`, `ForbiddenTypeViolations`, `ProducedOutput`, `IsCorrect`; static `Create(firstRun, secondRun, Type)`, `BuildComprehensiveFailureReport(List<GeneratorStepAnalysis> failedCaching, string[]? requiredSteps)`.
- `GeneratorStepAnalysis` struct: `StepName`, counts `Cached/Unchanged/Modified/New/Removed`, `HasForbiddenTypes`, `TotalOutputs`, `IsCachedSuccessfully`, `FormatBreakdown()`.
- `ForbiddenTypeViolation` record: `StepName`, `ForbiddenType`, `Path`.
- `TestConfiguration`: constants/properties `EnableJsonReporting`, `LanguageVersion`, `ReferenceAssemblies`, `AdditionalReferences`; scopes `WithLanguageVersion(LanguageVersion)`, `WithReferenceAssemblies(ReferenceAssemblies)`, `WithAdditionalReferences(ImmutableArray<PortableExecutableReference>)`.

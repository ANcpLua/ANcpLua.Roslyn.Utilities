# ANcpLua.Roslyn.Utilities

Comprehensive utilities for Roslyn analyzers and source generators. netstandard2.0.

## Quick Reference

| Category             | Key Types                                                                                        |
|----------------------|--------------------------------------------------------------------------------------------------|
| **Flow Control**     | `DiagnosticFlow<T>`                                                                              |
| **Pattern Matching** | `Match.*` (symbols), `Invoke.*` (operations)                                                     |
| **Validation**       | `SemanticGuard<T>`                                                                               |
| **Contexts**         | `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext`                    |
| **Code Generation**  | `IndentedStringBuilder`, `GeneratedCodeHelpers`, `ValueStringBuilder`, `TypedConstantExtensions` |

---

## DiagnosticFlow - Railway-Oriented Programming

Carry values AND diagnostics through transformations. Never lose an error.

```csharp
// Basic usage
DiagnosticFlow.Ok(model)
    .Then(Validate)                    // chain operations
    .Select(m => m.Transform())        // map values
    .Where(m => m.IsValid, errorDiag)  // filter with diagnostics
    .WarnIf(m => m.IsOld, warnDiag)    // conditional warnings
    .Then(Generate);

// From nullable
symbol.ToFlow(nullDiag)               // fails if null
    .Then(ProcessSymbol);

// Combine multiple
DiagnosticFlow.Zip(flow1, flow2)      // (T1, T2) tuple
DiagnosticFlow.Collect(flows)         // all must succeed
DiagnosticFlow.CollectSuccesses(flows) // keep successes

// Pipeline integration
provider
    .SelectFlow(ExtractModel)
    .ThenFlow(ValidateModel)
    .ReportAndContinue(context)
    .AddSource(context);
```

## Symbol Pattern Matching

### Match.* DSL (fluent symbol matching)

> **Note:** Matchers mutate `this` when chaining. Create new matchers for each distinct pattern.

```csharp
// Method matching
Match.Method()
    .Named("Execute")
    .Async()
    .WithParameters(2)
    .WithCancellationToken()
    .Matches(method);

// Match multiple attributes (any match) - varargs
Match.Method()
    .WithAttribute("Xunit.FactAttribute", "Xunit.TheoryAttribute", "NUnit.Framework.TestAttribute")
    .Public()
    .Matches(method);

// Finalizer matching
Match.Method().Finalizer().Matches(method);

// Type matching
Match.Type()
    .Class()
    .Public()
    .Implements("IDisposable")
    .HasParameterlessConstructor()
    .Matches(type);

// Property/Field/Parameter matching
Match.Property().ReadOnly().Required().Matches(prop);
Match.Field().Const().Public().Matches(field);
Match.Parameter().CancellationToken().Matches(param);

// Factory pattern for reusable base matchers (avoids mutation issues)
static MethodMatcher PublicInstance() => Match.Method().Public().NotStatic();
var asyncApi = PublicInstance().Async().ReturningTask();
var syncApi = PublicInstance().NotAsync().ReturningVoid();
```

### Invoke.* (operation matching)

```csharp
Invoke.Method("Dispose")
    .OnTypeImplementing("IDisposable")
    .WithNoArguments()
    .Matches(invocation);

Invoke.Method()
    .Linq()                           // System.Linq extensions
    .Named("Where")
    .Matches(invocation);

// Match multiple method names (any match) - varargs
Invoke.Method("WriteLine", "Write").OnConsole().Matches(invocation);

// Match multiple receiver types (any match) - varargs
Invoke.Method("Wait", "GetAwaiter")
    .OnType("Task", "ValueTask")
    .Matches(invocation);

// Additional methods: Named(name, params additionalNames)
Invoke.Method().Named("Add", "Remove", "Clear").Matches(invocation);
```

## SemanticGuard - Declarative Validation

```csharp
SemanticGuard.ForMethod(method)
    .MustBeAsync(asyncRequired)
    .MustReturnTask(taskRequired)
    .MustHaveCancellationToken(ctRequired)
    .MustNotBeStatic(instanceRequired)
    .ToFlow();  // -> DiagnosticFlow<IMethodSymbol>

SemanticGuard.ForType(type)
    .MustBePartial(partialRequired)
    .MustBeClass(classRequired)
    .MustImplement(interfaceType, implRequired)
    .ToFlow();
```

---

## Symbol Extensions

```csharp
// SymbolExtensions.cs
symbol.IsEqualTo(other)              // SymbolEqualityComparer.Default
symbol.HasAttribute("Full.Name")
symbol.HasAttribute(typeSymbol, inherits)
symbol.GetAttribute("Full.Name")
symbol.GetAttributes(typeSymbol, inherits)
symbol.IsVisibleOutsideOfAssembly()
symbol.IsOperator()
symbol.IsConst()
symbol.IsTopLevelStatement(ct)
symbol.GetNamespaceName()
symbol.GetFullyQualifiedName()
symbol.GetMetadataName()
symbol.GetAllMembers()
symbol.GetSymbolType()
symbol.GetMethod(name)
symbol.GetProperty(name)
method.ExplicitlyImplements(interfaceMethod)
symbol.ExplicitOrImplicitInterfaceImplementations()
symbol.GetAllTypeParameters()
symbol.GetTypeArguments()
symbol.GetOverriddenMember()

// TypeSymbolExtensions.cs
type.InheritsFrom(baseType)
type.Implements(interfaceType)
type.IsOrImplements(interfaceType)
type.IsOrInheritsFrom(expectedType)
type.IsObject/String/Char/Int32/Boolean/...()
type.IsEnumeration()
type.IsNumberType()
type.IsUnitTestClass()               // xUnit, NUnit, MSTest
type.IsPotentialStatic(ct)
type.IsSpanType()
type.IsMemoryType()
type.IsTaskType()
type.IsEnumerableType()
type.GetUnderlyingNullableTypeOrSelf()
type.GetElementType()

// MethodSymbolExtensions.cs
method.IsInterfaceImplementation()
method.IsOrOverrideMethod(baseMethod)
method.OverridesMethod(baseMethod)

// NamespaceExtensions.cs
ns.IsNamespace(string[] parts)       // zero-alloc
ns.GetAllTypes()                     // recursive
ns.GetAllNamespaces()
ns.GetPublicTypes()
```

## Operation Extensions

```csharp
// OperationExtensions.cs - Tree navigation
operation.Ancestors()
operation.FindAncestor<T>()
operation.IsDescendantOf<T>()
operation.Descendants()
operation.DescendantsOfType<T>()
operation.ContainsOperation<T>()
operation.GetContainingMethod()
operation.GetContainingType()
operation.GetContainingBlock()

// Context detection
operation.IsInNameofOperation()
operation.IsInExpressionTree()
operation.IsInStaticContext()
operation.IsInsideLoop()
operation.IsInsideTryBlock()
operation.IsInsideCatchBlock()
operation.IsInsideFinallyBlock()
operation.IsInsideLockStatement()
operation.IsInsideUsingStatement()

// Unwrapping
operation.UnwrapImplicitConversions()
operation.UnwrapAllConversions()
operation.UnwrapParenthesized()

// Value analysis
operation.GetActualType()
operation.IsConstantZero()
operation.IsConstantNull()
operation.TryGetConstantValue<T>(out value)
operation.IsAssignmentTarget()
operation.IsLeftSideOfAssignment()
operation.IsPassedByRef()
operation.GetCSharpLanguageVersion()
```

## Invocation Extensions

```csharp
// InvocationExtensions.cs
invocation.GetArgument("paramName")
invocation.GetArgument(index)
invocation.HasArgumentOfType(typeSymbol)
invocation.TryGetConstantArgument<T>(index, out value)
invocation.TryGetStringArgument(index, out value)
invocation.IsMethodNamed("name")
invocation.IsExtensionMethodOn(typeSymbol)
invocation.IsInstanceMethodOn(typeSymbol)
invocation.GetReceiverType()
invocation.ReturnsVoid()
invocation.IsAsyncMethod()
invocation.HasCancellationTokenParameter()
invocation.IsCancellationTokenPassed()
invocation.IsLinqMethod()
invocation.IsStringMethod()
invocation.IsObjectMethod()
invocation.GetExplicitArguments()
invocation.AllArgumentsAreConstant()
invocation.IsNullConditionalAccess()
```

## Domain Contexts

> **Note:** Context classes cache well-known type symbols from a `Compilation` for efficient repeated lookups.
> Create one context per compilation and reuse it for multiple checks.

### AwaitableContext

```csharp
var ctx = new AwaitableContext(compilation);
ctx.IsTaskLike(type)
ctx.IsAwaitable(type)
ctx.IsAwaitable(type, semanticModel, position)  // includes extensions
ctx.ConformsToAwaiterPattern(awaiterType)
ctx.CanUseAsyncKeyword(method)
ctx.IsAsyncEnumerable(type)
ctx.IsAsyncEnumerator(type)
ctx.IsConfiguredAwaitable(type)
ctx.GetTaskResultType(taskType)
// Properties: Task, TaskOfT, ValueTask, ValueTaskOfT, IAsyncEnumerable...
```

### AspNetContext

```csharp
var ctx = new AspNetContext(compilation);
ctx.IsController(type)
ctx.IsApiController(type)
ctx.IsAction(method)
ctx.HasHttpMethodAttribute(method)
ctx.HasRouteAttribute(symbol)
ctx.IsActionResult(type)
ctx.HasBindingAttribute(parameter)
ctx.IsFromBody(parameter)
ctx.IsFromServices(parameter)
ctx.IsFormFile(type)
// Properties: ControllerBase, Controller, IActionResult, HttpGetAttribute...
```

### DisposableContext

```csharp
var ctx = new DisposableContext(compilation);
ctx.IsDisposable(type)
ctx.IsSyncDisposable(type)
ctx.IsAsyncDisposable(type)
ctx.IsStream(type)
ctx.IsTextReaderOrWriter(type)
ctx.IsDbConnection(type)
ctx.IsDbCommand(type)
ctx.IsDbDataReader(type)
ctx.IsHttpClient(type)
ctx.IsSynchronizationPrimitive(type)
ctx.IsCancellationTokenSource(type)
ctx.IsSafeHandle(type)
ctx.ShouldBeDisposed(type)            // opinionated - excludes DI-managed types
// Properties: IDisposable, IAsyncDisposable, SafeHandle, Stream, DbConnection...

// For Dispose method detection, use extension methods (no context needed):
type.HasDisposeMethod()               // TypeSymbolExtensions
type.HasDisposeAsyncMethod()          // TypeSymbolExtensions
```

### CollectionContext

```csharp
var ctx = new CollectionContext(compilation);
ctx.IsEnumerable(type)
ctx.IsCollection(type)
ctx.IsList(type)
ctx.IsDictionary(type)
ctx.IsSet(type)
ctx.IsImmutable(type)
ctx.IsFrozen(type)
ctx.IsSpanLike(type)
ctx.IsMemoryLike(type)
ctx.IsReadOnly(type)
ctx.GetElementType(type)
// Properties: IEnumerable, ICollection, IList, IDictionary, ImmutableArray...

// For Count/Length property detection, use extension method (no context needed):
type.HasCountProperty()               // TypeSymbolExtensions
```

## Overload Analysis

```csharp
// OverloadFinder.cs
method.HasOverloadWithParameter(paramType)
method.HasOverloadWithParameters(param1, param2)
method.FindOverloadWithParameter(paramType)
method.FindOverloadWithParameters(params)
method.FindOverload(predicate)
method.FindAllOverloads()
method.HasOverloadWithReturnType(returnType)
method.HasOverloadWithFewerParameters()
```

## Namespace Extensions

```csharp
// NamespaceExtensions.cs
assembly.GetTypesRecursive()       // all types in assembly
assembly.GetPublicTypes()          // visible outside assembly
ns.GetAllTypes()                   // recursive type enumeration
ns.GetAllNamespaces()              // recursive namespace enumeration
ns.GetPublicTypes()                // visible types in namespace
```

## Pipeline Extensions

```csharp
// IncrementalValuesProviderExtensions.cs

// Source output
provider.AddSource(context)
provider.AddSources(context)

// Flow integration
provider.SelectFlow(ctx => DiagnosticFlow.Ok(model))
provider.ThenFlow(model => Validate(model))
provider.ReportAndContinue(context)  // reports diagnostics, returns successes
provider.ReportAndStop(context)
provider.CollectFlows()
provider.CollectFlowSuccesses()
provider.WarnIf(condition, warning)
provider.WhereFlow(predicate, onFail)

// Collection operations
provider.CollectAsEquatableArray()
provider.GroupBy(keySelector, elementSelector)
provider.WithIndex()
provider.Distinct(comparer)
provider.Batch(batchSize)
provider.Take(count)
provider.Skip(count)
provider.Count()
provider.Any()
provider.Any(predicate)
provider.FirstOrDefault()
provider.CombineWithCollected(other)

// Error handling
provider.SelectAndReportExceptions(selector, context)
provider.SelectAndReportDiagnostics(context)
provider.WhereNotNull()
```

## Code Generation

```csharp
// IndentedStringBuilder
var sb = new IndentedStringBuilder();
sb.AppendLine("namespace Foo");
using (sb.BeginBlock())           // adds { and }
{
    using (sb.BeginClass("public partial", "MyClass"))
    {
        using (sb.BeginMethod("public", "void", "Execute"))
        {
            sb.AppendLine("// code");
        }
    }
}

// Block helpers
sb.BeginNamespace("MyNamespace")
sb.BeginClass("public", "MyClass", "BaseClass")
sb.BeginMethod("public async", "Task", "RunAsync", "CancellationToken ct")
sb.BeginIf("condition")
sb.BeginElse()
sb.BeginForEach("var", "item", "collection")

// GeneratedCodeHelpers
GeneratedCodeHelpers.NullableEnable           // "#nullable enable"
GeneratedCodeHelpers.AutoGeneratedHeader("Tool")
GeneratedCodeHelpers.GeneratedCodeAttribute("Tool", "1.0")
GeneratedCodeHelpers.ExcludeFromCodeCoverage
GeneratedCodeHelpers.EditorBrowsableNever
GeneratedCodeHelpers.SuppressWarnings("CS1591", "CS0618")

// ValueStringBuilder
var vsb = new ValueStringBuilder(stackalloc char[256]);
vsb.Append("prefix_");
vsb.Append('x');
var value = vsb.ToString();
vsb.Dispose();
```

## Hash Combining

```csharp
var hash = HashCombiner.Create();
hash.Add(value1);
hash.Add(value2);
hash.AddRange(collection);
var code = hash.HashCode;

// Extensions
array.ToEquatableArray()
seq1.SequenceEquals(seq2)
collection.GetSequenceHashCode()
```

## Other Extensions

```csharp
// CompilationExtensions.cs
compilation.HasAccessibleTypeWithMetadataName(name)
compilation.IsNet9OrGreater()
compilation.IsNet10OrGreater()

// SemanticModelExtensions.cs
model.IsConstant(node, ct)
model.AllConstant(nodes, ct)
model.IsNullConstant(node, ct)

// LanguageVersionExtensions.cs
langVersion.IsCSharp10OrAbove()
langVersion.IsCSharp11OrAbove()
langVersion.IsCSharp12OrAbove()
langVersion.IsCSharp13OrAbove()
langVersion.IsCSharp14OrAbove()

// SyntaxExtensions.cs
member.HasModifier(SyntaxKind)
type.IsPartial()
type.IsPrimaryConstructorType()
node.GetNameLocation()

// LocationExtensions.cs
node.SpansMultipleLines(ct)

// StringExtensions.cs
str.SplitLines()             // zero-alloc enumerator
input.ToPropertyName()       // PascalCase + keyword escape
input.ToParameterName()      // camelCase + keyword escape
text.TrimBlankLines()
text.NormalizeLineEndings()

// ConvertExtensions.cs
typedConstant.ToBoolean(default)

// TypedConstantExtensions.cs
typedConstant.ToCSharpStringWithPostfix()

// EnumerableExtensions.cs
source.OrEmpty()
source.WhereNotNull()
source.ToImmutableArrayOrEmpty()
source.HasDuplicates()
source.SingleOrDefaultIfMultiple()
```

## Models

```csharp
// EquatableArray<T> - value-equal ImmutableArray wrapper
array.AsEquatableArray()
equatableArray.AsImmutableArray()
equatableArray.AsSpan()
equatableArray.IsDefaultOrEmpty
equatableArray.Length
equatableArray[index]

// DiagnosticInfo - equatable diagnostic for caching
DiagnosticInfo.Create(descriptor, location, args)
DiagnosticInfo.Create(descriptor, token, args)
DiagnosticInfo.Create(descriptor, node, args)

// LocationInfo - serializable location
LocationInfo.From(location)
LocationInfo.From(node)
LocationInfo.From(token)

// FileWithName - hint name + content
new FileWithName(name, content)
file.IsEmpty

// ResultWithDiagnostics<T>
new ResultWithDiagnostics<T>(result, diagnostics)
```

## Configuration Extensions

```csharp
// AnalyzerConfigOptionsProviderExtensions.cs
provider.GetRequiredGlobalProperty(name, prefix)
provider.GetRequiredAdditionalTextMetadata(text, name, prefix)
provider.TryGetGlobalBool(name, out value, prefix)
provider.GetGlobalBoolOrDefault(name, default, prefix)
provider.TryGetGlobalInt(name, out value, prefix)
provider.GetGlobalIntOrDefault(name, default, prefix)
provider.IsDesignTimeBuild()

// AnalyzerOptionsExtensions.cs
options.TryGetConfigurationValue(tree, key, out value)
options.GetConfigurationValue(tree, key, defaultBool)
options.GetConfigurationValue(tree, key, defaultInt)
options.GetConfigurationValue(tree, key, defaultString)
```

---

## Helper Extensions Philosophy

Each helper file answers ONE question. Use this guide to pick the right tool:

| File                              | Philosophy                                        | When to Use                                                   |
|-----------------------------------|---------------------------------------------------|---------------------------------------------------------------|
| **Guard.cs**                      | "Validate or throw. Provide defensive fallbacks." | Argument validation, fail-fast preconditions                  |
| **NullableExtensions.cs**         | "Functional transformation of nullable values"    | LINQ-style chaining, pipelines, `Select`/`Where`/`Do`/`Or`    |
| **ObjectExtensions.cs**           | "What type is this? Cast it safely."              | Safe casting (`As<T>`), type checking (`Is<T>`), reflection   |
| **TryExtensions.cs**              | "Parse or lookup, get null on failure"            | `TryParse*` methods, dictionary access, collection indexing   |
| **StringComparisonExtensions.cs** | "Compare strings with explicit semantics"         | `EqualsOrdinal`, `IndexOfOrdinal`, `ContainsIgnoreCase`, `HasValue` |

### Guard vs NullableExtensions

Both have `OrElse`-style fallback methods, but they serve different **semantic purposes**:

```csharp
// Guard: Defensive programming - "This parameter must not be null"
var config = Guard.NotNullOrElse(optionalConfig, DefaultConfig);

// Guard: Lazy evaluation for expensive defaults
var service = Guard.NotNullOrElse(injectedService, () => new ExpensiveService());

// NullableExtensions: Functional pipeline - "Transform this optional value"
var result = GetValue().Or(fallback).Select(transform);

// NullableExtensions: Lazy evaluation in pipelines
var data = GetValue().OrElse(() => LoadFromDisk());
```

**Use Guard when:**

- Validating method arguments at entry points
- The fallback signals a defensive default
- You want `CallerArgumentExpression` for debugging
- Use `NotNullOrElse(value, factory)` for expensive fallbacks (lazy evaluation)

**Use NullableExtensions when:**

- Chaining transformations in a pipeline
- Working with optional data (not parameters)
- You want LINQ-style composition
- Use `.OrElse(() => ...)` for lazy fallbacks in chains

### Migration from ObjectExtensions

The `IfNotNull` methods were removed (duplicated `NullableExtensions`):

```csharp
// BEFORE                                    // AFTER
obj.IfNotNull(x => x.Length)                → obj.Select(x => x.Length)
obj.IfNotNull(x => Process(x))              → obj.Do(x => Process(x))
obj.IfNotNull(x => x.Name, "default")       → obj.Select(x => x.Name).Or("default")
```

### Migration from Guard (v1.17 → v1.18)

The `NotNullOr*` methods were renamed to `NotNullOrElse*` for clarity, and lazy `Func<T>` overloads were added:

```csharp
// BEFORE                                    // AFTER
Guard.NotNullOr(value, fallback)            → Guard.NotNullOrElse(value, fallback)
Guard.NotNullOrEmptyOr(str, fallback)       → Guard.NotNullOrEmptyOrElse(str, fallback)
Guard.NotNullOrWhiteSpaceOr(str, fallback)  → Guard.NotNullOrWhiteSpaceOrElse(str, fallback)

// NEW: Lazy evaluation (Func<T> overloads)
Guard.NotNullOrElse(value, () => ExpensiveDefault())
Guard.NotNullOrEmptyOrElse(str, () => GenerateName())
Guard.NotNullOrWhiteSpaceOrElse(str, () => GenerateTitle())
```
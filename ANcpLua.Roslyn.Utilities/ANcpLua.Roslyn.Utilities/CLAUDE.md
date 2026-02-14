# ANcpLua.Roslyn.Utilities

**Core utilities for Roslyn analyzers and source generators.** Target: `netstandard2.0`.

This is the SOURCE OF TRUTH for Roslyn helpers. Before writing ANY utility code in downstream projects (ErrorOrX, ANcpLua.Analyzers, qyl), check this library first.

---

## Quick Reference

| Category | Key Types |
|----------|-----------|
| **Flow Control** | `DiagnosticFlow<T>`, `Result<T>` |
| **Pattern Matching** | `Match.*`, `Invoke.*` |
| **Validation** | `Guard`, `SemanticGuard<T>` |
| **Contexts** | `AwaitableContext`, `AspNetContext`, `DisposableContext`, `CollectionContext` |
| **Code Generation** | `IndentedStringBuilder`, `GeneratedCodeHelpers`, `ValueStringBuilder` |
| **Caching** | `EquatableArray<T>`, `DiagnosticInfo`, `LocationInfo` |
| **Analyzer Infrastructure** | `DiagnosticAnalyzerBase`, `CodeFixProviderBase<T>` |

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

---

## Result&lt;T&gt; - General-Purpose Success/Failure

For domain logic and non-Roslyn error handling. Use `DiagnosticFlow<T>` for Roslyn pipelines.

```csharp
// Create
Result<int> ok = Result<int>.Ok(42);
Result<int> ok2 = 42;                              // implicit
Result<int> fail = new Error("bad", "Bad input");  // implicit

// Pattern match
string msg = ok.Match(v => $"Got {v}", e => e.Message);

// Railway composition (same naming as DiagnosticFlow)
Result<string> pipeline = ok
    .Where(x => x > 0, new Error("neg", "Must be positive"))
    .Select(x => x.ToString())
    .Tap(Console.WriteLine);

// Bind (flatMap)
Result<Order> result = orderId
    .Then(id => repo.Find(id))   // returns Result<Order>
    .Where(o => !o.IsCancelled, Errors.Cancelled());

// Async pipelines
var shipped = await repo.LoadAsync(id, ct)
    .ToResult(Errors.NotFound(id))
    .ThenAsync(order => order.Match(
        draft: _ => Result<OrderShipped>.Fail(Errors.MustBePaid()),
        paid: p  => Task.FromResult(p.Ship(shipment)),
        shipped: _ => Result<OrderShipped>.Fail(Errors.AlreadyShipped()),
        cancelled: _ => Result<OrderShipped>.Fail(Errors.Cancelled())))
    .TapAsync(s => repo.SaveAsync(s, ct));

// Factory methods
Result.Ok(42)
Result.Fail<int>(new Error("x", "y"))
Result.FromNullable(maybeNull, errorIfNull)
Result.Try(() => Parse(input), ex => new Error("parse", ex.Message))
```

**When to use which:**
| Scenario | Use |
|----------|-----|
| Roslyn generator pipeline with diagnostics | `DiagnosticFlow<T>` |
| Domain logic success/failure | `Result<T>` |
| Validate arguments (throw on invalid) | `Guard` |

---

## Symbol Pattern Matching

### Match.* DSL (fluent symbol matching)

**⚠️ IMPORTANT:** Matchers mutate `this` when chaining. Create new matchers for each distinct pattern using factory methods.

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

#### MethodMatcher - All Methods

```csharp
// Base methods (inherited by all matchers)
.Named(name)                    // exact name match
.NameMatches(regex)             // regex pattern
.NameStartsWith(prefix)
.NameEndsWith(suffix)
.NameContains(substring)
.Public() / .Private() / .Internal() / .Protected()
.VisibleOutsideAssembly()
.Static() / .NotStatic()
.Abstract() / .Sealed() / .Virtual() / .Override()
.WithAttribute(fullyQualifiedName)
.WithAttribute(name, params additionalNames)  // any match
.WithoutAttribute(fullyQualifiedName)
.DeclaredIn(typeName)           // containing type
.InNamespace(namespaceName)
.Where(predicate)               // custom predicate

// Method-specific
.Constructor() / .Finalizer()
.Async() / .NotAsync()
.Extension() / .NotExtension()
.Generic() / .NotGeneric()
.WithTypeParameters(count)
.WithNoParameters()
.WithParameters(count)
.WithMinParameters(count)
.WithCancellationToken()
.ReturningVoid()
.ReturningTask()                // Task or ValueTask
.ReturningBool()
.ReturningString()
.Returning(typeName)
.ExplicitImplementation()
```

#### TypeMatcher - All Methods

```csharp
.Class() / .Struct() / .Interface() / .Enum() / .Record()
.Generic() / .NotGeneric()
.InheritsFrom(baseTypeName)
.Implements(interfaceName)
.Disposable()                   // shortcut for IDisposable
.Nested() / .TopLevel()
.StaticClass()
.HasMember(name)
.HasParameterlessConstructor()
```

#### PropertyMatcher / FieldMatcher / ParameterMatcher

```csharp
// PropertyMatcher
.WithGetter() / .WithSetter() / .WithInitSetter()
.ReadOnly()
.Indexer()
.Required()
.OfType(typeName)

// FieldMatcher
.Const() / .ReadOnly() / .Volatile()
.OfType(typeName)
.BackingField() / .NotBackingField()

// ParameterMatcher
.Ref() / .Out() / .In()
.Params()
.Optional()
.OfType(typeName)
.CancellationToken()            // shortcut for CancellationToken type
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

#### InvocationMatcher - All Methods

```csharp
// Method name matching
.Named(name)
.Named(name, params additionalNames)  // any match
.NameStartsWith(prefix)
.NameEndsWith(suffix)
.NameContains(substring)

// Receiver type matching
.OnType(typeName)
.OnType(typeName, params additionalTypeNames)  // any match
.OnTypeInheritingFrom(baseTypeName)
.OnTypeImplementing(interfaceName)

// Method characteristics
.Extension() / .NotExtension()
.Static() / .Instance()
.Async()
.Generic()

// Return type
.ReturningVoid()
.ReturningTask()
.Returning(typeName)

// Arguments
.WithNoArguments()
.WithArguments(count)
.WithMinArguments(count)
.WithConstantArg(index)
.WithConstantStringArg(index)
.WithNullArg(index)
.WithArgOfType(index, typeName)
.WithAllConstantArgs()

// Namespace
.InNamespace(namespaceName)
.InNamespaceStartingWith(prefix)

// Shortcuts
.Linq()                         // System.Linq extensions
.OnString()                     // String receiver
.OnTask()                       // Task receiver
.OnConsole()                    // Console receiver

// Custom
.Where(predicate)
```

---

## Guard - Argument Validation

Expressive argument validation with `CallerArgumentExpression` for automatic parameter names.
All methods use `[MethodImpl(AggressiveInlining)]` where appropriate for hot path performance.

### Null Validation

```csharp
Guard.NotNull(value)                          // throws if null
Guard.NotNullOrElse(value, fallback)          // returns fallback if null
Guard.NotNullOrElse(value, () => Expensive()) // lazy fallback
Guard.NotNullWithMember(obj, obj?.Member)     // validates both
Guard.MemberNotNull(obj, obj.Member)          // validates member only
```

### String Validation

```csharp
Guard.NotNullOrEmpty(str)
Guard.NotNullOrWhiteSpace(str)
Guard.NotNullOrEmptyOrElse(str, fallback)
Guard.NotNullOrWhiteSpaceOrElse(str, fallback)
```

### String Length Validation

```csharp
Guard.HasLength(str, 2)                       // exact length
Guard.HasMinLength(str, 8)                    // minimum length
Guard.HasMaxLength(str, 50)                   // maximum length
Guard.HasLengthBetween(str, 3, 100)           // length range
```

### Value Type Validation

```csharp
Guard.NotDefault(value)                       // value type not default(T)
Guard.NotEmpty(guid)                          // Guid is not Guid.Empty
```

### Collection Validation

```csharp
Guard.NotNullOrEmpty(collection)              // IReadOnlyCollection<T>
Guard.NoDuplicates(items)                     // throws with duplicate value
Guard.NoDuplicates(items, comparer)           // with custom IEqualityComparer<T>
```

### Set Membership Validation

```csharp
Guard.OneOf(value, new[] { "a", "b", "c" })   // value must be in allowed set
Guard.OneOf(value, allowedHashSet)            // O(1) lookup variant
Guard.NotOneOf(value, new[] { 0, 80, 443 })   // value must not be in disallowed set
Guard.NotOneOf(value, disallowedHashSet)      // O(1) lookup variant
```

### File System Validation

```csharp
Guard.FileExists(path)                        // validates file exists
Guard.DirectoryExists(path)                   // validates directory exists
Guard.ValidFileName(name)                     // no invalid filename chars
Guard.ValidFileNameOrNull(name)               // nullable variant
Guard.ValidPath(path)                         // no invalid path chars
Guard.ValidPathOrNull(path)                   // nullable variant
Guard.ValidExtension(ext)                     // no leading dot, no separators
Guard.NormalizedExtension(ext)                // ensures leading dot (accepts both "txt" and ".txt")
```

### Type Validation

```csharp
Guard.DefinedEnum(enumValue)                  // validates enum is defined
Guard.NotNullableType(type)                   // not Nullable<T>
Guard.AssignableTo<IService>(type)            // type implements IService
Guard.AssignableFrom<MyClass>(type)           // MyClass assignable to type
```

### Numeric Validation (int, long, double, decimal)

```csharp
Guard.NotZero(value)
Guard.NotNegative(value)
Guard.Positive(value)                         // > 0
Guard.NotGreaterThan(value, max)              // value ≤ max
Guard.NotLessThan(value, min)                 // value ≥ min
Guard.LessThan(value, max)                    // value < max
Guard.GreaterThan(value, min)                 // value > min
Guard.InRange(value, min, max)                // min ≤ value ≤ max
Guard.ValidIndex(index, count)                // 0 ≤ index < count

// Double-specific (NaN-aware comparisons)
Guard.NotNaN(value)
Guard.Finite(value)                           // not NaN or Infinity
```

### Condition Validation

```csharp
Guard.That(condition, message)                // throws if false
Guard.Satisfies(value, predicate, message)    // throws if predicate false
Guard.Unreachable()                           // for unreachable code
Guard.Unreachable<T>()                        // in expression contexts
Guard.UnreachableIf(condition)                // throws only if condition is true
```

---

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
symbol.GetAttributeTypeArguments(attrName)  // sorted EquatableArray<string> of typeof() args

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
type.GetContainingTypeChain()        // "Outer.Middle.Inner" for nested types
type.GetGenericParameterClause()     // "<T, U>" or null

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

---

## Operation Extensions

```csharp
// OperationExtensions.cs - Tree navigation
operation.Ancestors()
operation.FindAncestor<T>()
operation.IsDescendantOf<T>()
operation.Descendants()
operation.DescendantsAndSelf()
operation.DescendantsOfType<T>()
operation.ContainsOperation<T>()
operation.GetContainingMethod(ct)              // ⚠️ requires CancellationToken
operation.GetContainingType(ct)                // ⚠️ requires CancellationToken
operation.GetContainingBlock()

// Context detection
operation.IsInNameofOperation()
operation.IsInExpressionTree(expressionSymbol) // ⚠️ requires Expression<T> symbol
operation.IsInStaticContext(ct)                // ⚠️ requires CancellationToken
operation.IsInsideLoop()
operation.IsInsideTryBlock()
operation.IsInsideCatchBlock()
operation.IsInsideFinallyBlock()
operation.IsInsideLockStatement()
operation.IsInsideUsingStatement()
operation.IsUsingStatement()                   // is IUsingOperation or IUsingDeclarationOperation

// Unwrapping
operation.UnwrapImplicitConversions()
operation.UnwrapAllConversions()
operation.UnwrapParenthesized()
operation.UnwrapLabeledOperations()

// Value analysis
operation.GetActualType()
operation.IsConstantZero()
operation.IsConstantNull()
operation.IsNull()                             // alias for IsConstantNull
operation.IsConstant(out value)
operation.IsConstant<T>(value)                 // equals specific value
operation.TryGetConstantValue<T>(out value)
operation.IsKind(OperationKind)
operation.IsAssignmentTarget()
operation.IsLeftSideOfAssignment()
operation.IsPassedByRef()
operation.GetCSharpLanguageVersion()

// Human-readable names (for diagnostics)
operation.GetOperandName(fallback)             // "myVar", "GetValue()", "this"
operation.GetCollectionSourceName()            // source name from foreach collection
```

### Getting Expression Tree Symbol

```csharp
// For IsInExpressionTree, get the Expression<T> symbol from compilation:
var expressionSymbol = compilation.GetTypeByMetadataName(
    "System.Linq.Expressions.Expression`1");
if (operation.IsInExpressionTree(expressionSymbol))
{
    // Inside expression tree - different runtime semantics
}
```

---

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

---

## Domain Contexts

**Note:** Context classes cache well-known type symbols from a `Compilation` for efficient repeated lookups. Create one context per compilation and reuse it for multiple checks.

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

---

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

---

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

---

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

---

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

---

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
input.ToShortHash()          // deterministic 8-char hex hash (SHA-256)
label.EscapeDotLabel()       // escape for Graphviz DOT labels
label.EscapeMermaidLabel()   // escape for Mermaid diagram labels

// ReflectionExtensions.cs
method.InvokeUnwrapped(target, args)                  // invoke without TargetInvocationException
specializedType.GetMethodFromGenericDefinition(method) // generic method lookup across TFMs

// RuntimeTypeExtensions.cs
type.IsGenericTask()           // is Task<T>
type.IsGenericValueTask()      // is ValueTask<T>
type.IsTaskLike()              // Task, Task<T>, ValueTask, ValueTask<T>
type.GetTaskResultType()       // extract T from Task<T>/ValueTask<T>
type.ImplementsOpenGeneric(openGenericInterface)       // e.g. IHandler<>
type.GetClosedImplementations(openGenericInterface)    // all IHandler<T> implementations

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

---

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

## Analyzer Infrastructure

Base classes that eliminate boilerplate for analyzers and code fixes.

### DiagnosticAnalyzerBase

```csharp
// Automatically configures:
// - GeneratedCodeAnalysisFlags.None (skip generated code)
// - EnableConcurrentExecution() (better performance)

public class MyAnalyzer : DiagnosticAnalyzerBase
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ...;

    protected override void InitializeCore(AnalysisContext context)
    {
        // Register your analysis actions here
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }
}
```

### CodeFixProviderBase&lt;TSyntax&gt;

```csharp
// Eliminates boilerplate for common pattern:
// 1. Find syntax node at diagnostic location
// 2. Get semantic model
// 3. Transform node
// 4. Replace in document

public class MyCodeFix : CodeFixProviderBase<InvocationExpressionSyntax>
{
    protected override string Title => "Use better API";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("MY001");

    protected override InvocationExpressionSyntax? Transform(
        InvocationExpressionSyntax node,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        // Return transformed node, or null to skip
        return node.WithExpression(...);
    }
}
```

**Features:**
- Automatic `FixAllProvider` (BatchFixer)
- Null-safe semantic model handling
- Returns original document if Transform returns null or same node

---

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

---

## Helper Extensions Philosophy

Each helper file answers ONE question. Use this guide to pick the right tool:

| File | Philosophy | When to Use |
|------|------------|-------------|
| **Guard.cs** | "Validate or throw. Provide defensive fallbacks." | Argument validation, fail-fast preconditions |
| **NullableExtensions.cs** | "Functional transformation of nullable values" | LINQ-style chaining, pipelines, `Select`/`Where`/`Do`/`Or` |
| **ObjectExtensions.cs** | "What type is this? Cast it safely." | Safe casting (`As<T>`), type checking (`Is<T>`), reflection |
| **TryExtensions.cs** | "Parse or lookup, get null on failure" | `TryParse*` methods, dictionary access, collection indexing |
| **StringComparisonExtensions.cs** | "Compare strings with explicit semantics" | `EqualsOrdinal`, `IndexOfOrdinal`, `ContainsIgnoreCase`, `HasValue` |
| **ReflectionExtensions.cs** | "Invoke without TIE wrapping, find generic methods" | Runtime dispatch, handler tables, type-erased invocation |
| **RuntimeTypeExtensions.cs** | "What async/generic shape is this runtime Type?" | Handler registries, middleware, open generic interface scanning |

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

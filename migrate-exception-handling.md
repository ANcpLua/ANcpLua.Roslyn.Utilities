# Exception & Fallback Migration Prompt

**Purpose**: Scan a C#/.NET codebase and replace every try/catch, fallback, and exception-swallowing pattern with the correct non-throwing alternative. Zero try/catch in application code when done.

---

## Non-Negotiable Rules

1. **No bare `catch` or `catch (Exception)`.** Ever.
2. **No `return default/null/false` from a catch.** That is a bug, not a strategy.
3. **No try/catch for flow control.** The BCL has `Try*` for every expected failure.
4. **If an exception indicates a bug** (e.g., property getter throws, index out of range in code you control), **remove the catch and let it surface.**
5. **If a failure is expected**, model it explicitly: `T?`, `Try*` method, or `DiagnosticFlow<T>`.
6. **No suppressions, no TODOs, no "hybrid" compromises.**

---

## Decision Tree

For every `try/catch` or fallback pattern found:

```
Is the caught exception indicating a BUG in our code?
├── YES → REMOVE the try/catch. Let it throw. Fix the bug.
│
└── NO → Does the BCL have a Try* / non-throwing API?
    ├── YES → Use it. (See replacement table below)
    │
    └── NO → Is this a system boundary? (middleware, background loop, generator pipeline)
        ├── YES → Catch Exception, convert to TYPED RESULT (DiagnosticFlow<T>, Result<T>).
        │         Exception is DATA, not swallowed. Method name must contain "Try".
        │
        └── NO → Does a non-throwing alternative exist?
            ├── YES → Use it. (File.Exists before File.Read, null check before .Value, etc.)
            │
            └── NO → Create a private Try* boundary method:
                      - Method name starts with "Try"
                      - Return type is T? (nullable)
                      - Catches ONLY the specific exception (XmlException, not Exception)
                      - ONE location, all callers use null-conditional
```

---

## Replacement Table

### Parsing — NEVER catch FormatException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { int.Parse(s); } catch (FormatException) { ... }` | `s.TryParseInt32()` → `int?` |
| `try { int.Parse(s); } catch { return defaultVal; }` | `s.TryParseInt32(defaultVal)` → `int` |
| `try { bool.Parse(s); } catch { ... }` | `s.TryParseBool()` → `bool?` |
| `try { Guid.Parse(s); } catch { ... }` | `s.TryParseGuid()` → `Guid?` |
| `try { Enum.Parse<T>(s); } catch { ... }` | `s.TryParseEnum<T>()` → `T?` |
| `try { DateTime.Parse(s); } catch { ... }` | `s.TryParseDateTime()` → `DateTime?` |
| `try { double.Parse(s); } catch { ... }` | `s.TryParseDouble()` → `double?` |
| `try { decimal.Parse(s); } catch { ... }` | `s.TryParseDecimal()` → `decimal?` |
| `try { TimeSpan.Parse(s); } catch { ... }` | `s.TryParseTimeSpan()` → `TimeSpan?` |
| `try { DateTimeOffset.Parse(s); } catch { ... }` | `s.TryParseDateTimeOffset()` → `DateTimeOffset?` |

Every numeric/primitive type has two overloads:
- `s.TryParseXxx()` → returns `T?` (null on failure)
- `s.TryParseXxx(defaultValue)` → returns `T` (default on failure)

### Dictionary — NEVER catch KeyNotFoundException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { return dict[key]; } catch (KeyNotFoundException) { return null; }` | `dict.GetOrNull(key)` |
| `try { return dict[key]; } catch { return default; }` | `dict.GetOrDefault(key, defaultValue)` |
| `if (dict.ContainsKey(k)) { var v = dict[k]; }` (TOCTOU) | `dict.GetOrNull(key)?.Process()` |
| `dict.TryGetValue(k, out var v) ? v : null` | `dict.GetOrNull(key)` |
| `dict.TryGetValue(k, out var v) ? v : expensive()` | `dict.GetOrElse(key, () => Expensive())` |

Overloads exist for `IDictionary<K,V>` and `IReadOnlyDictionary<K,V>`.
- Reference types: `GetOrNull` → `TValue?`
- Value types: `GetValueOrNull` → `TValue?` (nullable struct)
- Any type: `GetOrDefault(key, fallback)`, `GetOrElse(key, () => factory)`

### Collections — NEVER catch IndexOutOfRangeException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { return list[i]; } catch (ArgumentOutOfRangeException) { ... }` | `list.ElementAtOrNull(i)` (ref types) |
| `try { return list[i]; } catch { return default; }` | `list.ValueAtOrNull(i)` (value types) |
| `try { return list[i]; } catch { return fallback; }` | `list.ElementAtOrDefault(i, fallback)` |
| `items ?? Array.Empty<T>()` | `items.OrEmpty()` |
| `items ?? Enumerable.Empty<T>()` | `items.OrEmpty()` |
| `items ?? []` | `items.OrEmpty()` |
| `items?.Where(...)` (null-guarded LINQ) | `items.OrEmpty().Where(...)` |
| `items?.FirstOrDefault()` | `items.OrEmpty().FirstOrDefault()` |
| `items?.ToList() ?? new()` | `items.OrEmpty().ToList()` |

### Null Handling — NEVER catch NullReferenceException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `value ?? throw new ArgumentNullException()` | `Guard.NotNull(value)` |
| `if (x == null) throw new ArgumentNullException(...)` | `Guard.NotNull(x)` |
| `x ?? defaultValue` (where null is a bug) | `Guard.NotNull(x)` |
| `x ?? defaultValue` (where null is expected) | `Guard.NotNullOrElse(x, defaultValue)` |
| `x ?? expensiveDefault()` | `Guard.NotNullOrElse(x, () => ExpensiveDefault())` |
| `string.IsNullOrEmpty(s) ? throw : s` | `Guard.NotNullOrEmpty(s)` |
| `string.IsNullOrWhiteSpace(s) ? throw : s` | `Guard.NotNullOrWhiteSpace(s)` |
| `if (s == null) return ""; else return s;` | `Guard.NotNullOrEmptyOrElse(s, "")` |

### Nullable Transforms — Replace imperative null checks

| Anti-Pattern | Replacement |
|-------------|-------------|
| `if (x != null) return Transform(x); return null;` | `x.Select(Transform)` |
| `if (x != null) { var y = GetY(x); if (y != null) ... }` | `x.SelectMany(GetY)` |
| `if (x != null && Condition(x)) return x; return null;` | `x.Where(Condition)` |
| `if (x != null) DoSomething(x);` | `x.Do(DoSomething)` |
| `x ?? defaultValue` | `x.Or(defaultValue)` |
| `x ?? factory()` | `x.OrElse(factory)` |
| `x ?? throw new InvalidOperationException()` | `x.OrThrow(() => new InvalidOperationException())` |
| `x != null ? Transform(x) : fallback` | `x.Match(Transform, () => fallback)` |
| `x == sentinel ? null : x` | `x.NullIf(sentinel)` |

### Type Conversion — NEVER catch InvalidCastException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { return (T)obj; } catch (InvalidCastException) { ... }` | `if (obj is T t) return t;` |
| `try { Convert.ChangeType(v, typeof(T)); } catch { ... }` | `if (v is T t) return t;` |
| `(obj as T) ?? default` | `obj is T t ? t : default` |

### File System — NEVER catch FileNotFoundException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { File.ReadAllText(p); } catch (FileNotFoundException) { ... }` | `Guard.FileExists(p)` then read |
| `try { ... } catch (DirectoryNotFoundException) { ... }` | `Guard.DirectoryExists(p)` then operate |
| `try { File.Open(...) } catch (IOException) { ... }` | Check `File.Exists()`, handle `IOException` at system boundary only |

### Validation — NEVER catch ArgumentException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `if (value < 0) throw new ArgumentOutOfRangeException(...)` | `Guard.NotNegative(value)` |
| `if (value <= 0) throw ...` | `Guard.Positive(value)` |
| `if (value > max) throw ...` | `Guard.NotGreaterThan(value, max)` |
| `if (value < min \|\| value > max) throw ...` | `Guard.InRange(value, min, max)` |
| `if (!Enum.IsDefined(...)) throw ...` | `Guard.DefinedEnum(value)` |
| `if (value == default) throw ...` | `Guard.NotDefault(value)` |
| `if (guid == Guid.Empty) throw ...` | `Guard.NotEmpty(guid)` |

### Reflection — NEVER catch TargetInvocationException

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { prop.GetValue(obj); } catch { return null; }` | `prop != null ? prop.GetValue(obj) : null` — if it throws, it's a bug |
| `try { method.Invoke(...); } catch (TIE ex) { throw ex.InnerException; }` | `method.InvokeUnwrapped(...)` (from ReflectionExtensions) |

### String Operations — NEVER catch for string checks

| Anti-Pattern | Replacement |
|-------------|-------------|
| `s.Equals(other, StringComparison.Ordinal)` | `s.EqualsOrdinal(other)` |
| `s.StartsWith(prefix, StringComparison.Ordinal)` | `s.StartsWithOrdinal(prefix)` |
| `s.EndsWith(suffix, StringComparison.Ordinal)` | `s.EndsWithOrdinal(suffix)` |
| `s.Contains(sub, StringComparison.OrdinalIgnoreCase)` | `s.ContainsOrdinalIgnoreCase(sub)` |

### Enumerable Safety — NEVER catch InvalidOperationException from LINQ

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { items.Single(); } catch { ... }` | `items.SingleOrDefaultIfMultiple()` |
| `items.Where(x => x != null).Cast<T>()` | `items.WhereNotNull()` |
| `items.Where(x => !condition(x))` | `items.WhereNot(condition)` |
| `items?.ToImmutableArray() ?? ImmutableArray<T>.Empty` | `items.ToImmutableArrayOrEmpty()` |
| `items.Any(x => x != null)` | For tuples: `tuple.AnyNotNull()` |
| `items.All(x => x == null)` | For tuples: `tuple.AllNull()` |

---

## Pipeline Error Handling (Roslyn Source Generators)

For source generator pipelines, use `DiagnosticFlow<T>` instead of try/catch:

| Anti-Pattern | Replacement |
|-------------|-------------|
| `try { Extract(ctx); } catch { return null; }` | `DiagnosticFlow.Try(() => Extract(ctx), ex => ToDiagnostic(ex))` |
| `if (x == null) { report error; return; }` | `DiagnosticFlow.FromNullable(x, nullDiag)` |
| `var a = Get(); var b = Get(); if (a == null \|\| b == null) ...` | `DiagnosticFlow.Zip(flowA, flowB)` |
| `foreach (var item in items) { try { process; } catch { skip; } }` | `items.Select(i => flow(i)).Sequence()` then `.Report(ctx)` |
| Manual error accumulation across steps | `.Then(Validate).Where(x => x.IsValid, errDiag).Select(Transform)` |

---

## System Boundary Pattern (The Only Legitimate Catch)

At true system boundaries — and ONLY there — catch Exception and convert to a typed result:

```csharp
// ✅ CORRECT: Named "Try", typed result, exception is DATA not swallowed
public static DiagnosticFlow<T> Try<T>(
    Func<T> factory,
    Func<Exception, DiagnosticInfo> onException)
{
    try
    {
        return Ok(factory());
    }
    catch (Exception ex)
    {
        return Fail<T>(onException(ex));
    }
}
```

Requirements for legitimate catches:
- Method name contains `Try`
- Return type encodes failure (`T?`, `DiagnosticFlow<T>`, `Result<T>`)
- Exception is captured in the result, not discarded
- Catches the narrowest possible type
- Located at infrastructure level (pipeline, middleware, background loop)

---

## Where to NOT Catch (BCL has non-throwing API)

| Operation | Throwing API | Non-Throwing API |
|-----------|-------------|-----------------|
| Parse int | `int.Parse()` | `int.TryParse()` → `s.TryParseInt32()` |
| Parse enum | `Enum.Parse()` | `Enum.TryParse()` → `s.TryParseEnum<T>()` |
| Parse XML | `XElement.Parse()` | Create a `Try*` boundary (no BCL TryParse) |
| Parse JSON | `JsonSerializer.Deserialize()` | `Utf8JsonReader` + check, or `Try*` boundary |
| Dictionary lookup | `dict[key]` | `dict.TryGetValue()` → `dict.GetOrNull(key)` |
| List access | `list[i]` | `list.ElementAtOrNull(i)` |
| Type cast | `(T)obj` | `obj is T t` |
| File read | `File.ReadAllText()` | `File.Exists()` + read |
| Reflection | `prop.GetValue()` | Null-check `prop` first |
| String convert | `Convert.ChangeType()` | `is T` pattern match |

---

## Workflow

1. **Scan**: Find all `try`, `catch`, `catch (Exception`, `catch {`, `return default`, `return null` inside catch blocks
2. **Classify** each as: bug | expected-failure | system-boundary
3. **Replace** using the table above
4. **Build**: Must compile with zero warnings
5. **Verify**: No `try` keyword remains in application/extension code (only in infrastructure boundaries)

```bash
# Scan commands
grep -rn "try\s*{" --include="*.cs" .
grep -rn "catch\s*(" --include="*.cs" .
grep -rn "catch\s*{" --include="*.cs" .
grep -rn "return null;" --include="*.cs" . | grep -B5 "catch"
grep -rn "return default" --include="*.cs" . | grep -B5 "catch"

# Verify: should return 0 for application code
grep -rn "catch (Exception)" --include="*.cs" . | grep -v "Try\|Pipeline\|Middleware\|BackgroundService" | wc -l
```

---

## Required Package

All replacements use utilities from **ANcpLua.Roslyn.Utilities**:

```xml
<!-- DLL reference (for analyzers/generators that ship as DLL) -->
<PackageReference Include="ANcpLua.Roslyn.Utilities" Version="1.37.1" />

<!-- Source-only (for analyzers/generators that embed source) -->
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" Version="1.37.1" />

<!-- Polyfills only, no Roslyn dependency (for any netstandard2.0 project) -->
<PackageReference Include="ANcpLua.Roslyn.Utilities.Polyfills" Version="1.37.1" />
```

---

## Success Criteria

| Check | Target |
|-------|--------|
| `catch` blocks in extension/utility methods | 0 |
| `catch (Exception)` in application code | 0 |
| `return null/default` inside catch | 0 |
| Bare `catch {` blocks | 0 |
| `try/catch` for parsing | 0 |
| `try/catch` for dictionary lookup | 0 |
| `try/catch` for type conversion | 0 |
| Build warnings | 0 |
| Tests passing | All |

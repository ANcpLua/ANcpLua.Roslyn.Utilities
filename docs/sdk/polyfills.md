# Polyfills (Legacy TFM Support)

Enable modern C# features on older target frameworks.

## Enabling Polyfills

Add to your project file:

```xml
<PropertyGroup>
  <InjectANcpLuaLanguageFeaturesPolyfills>true</InjectANcpLuaLanguageFeaturesPolyfills>
</PropertyGroup>
```

## Available Polyfills

| Feature | TFM Range | What It Enables |
|---------|-----------|-----------------|
| **Records** | < net5.0 | `record` keyword |
| **Index/Range** | < netcoreapp3.1 | `arr[^1]`, `arr[1..3]` |
| **Required Members** | < net7.0 | `required` keyword |
| **System.Threading.Lock** | < net9.0 | New `Lock` type |
| **TimeProvider** | < net8.0 | Testable time abstraction |
| **Init-only Setters** | < net5.0 | `init` keyword |
| **Nullable Attributes** | < netstandard2.1 | `[NotNull]`, `[MaybeNull]` |
| **CallerArgumentExpression** | < net6.0 | Better exception messages |

## Example: Records on netstandard2.0

```csharp
// Works on netstandard2.0 with polyfills enabled
public record Person(string Name, int Age);

var person = new Person("Alice", 30);
var older = person with { Age = 31 };
```

## Example: Index/Range on net48

```csharp
// Works on .NET Framework 4.8 with polyfills enabled
var array = new[] { 1, 2, 3, 4, 5 };

var last = array[^1];           // 5
var slice = array[1..3];        // [2, 3]
var lastTwo = array[^2..];      // [4, 5]
```

## Example: TimeProvider on net6.0

```csharp
// Works on .NET 6.0 with polyfills enabled
public class MyService(TimeProvider time)
{
    public bool IsExpired(DateTimeOffset expiry)
    {
        return time.GetUtcNow() > expiry;
    }
}
```

## Guard Clauses

Guard clauses are enabled by default (no property needed):

```csharp
// Available on all target frameworks
public void Process(string name, object data)
{
    Throw.IfNullOrEmpty(name);    // with CallerArgumentExpression
    Throw.IfNull(data);
}
```

## Polyfill Properties

| Property | Default | Description |
|----------|---------|-------------|
| `InjectANcpLuaLanguageFeaturesPolyfills` | `false` | Enable all language polyfills |
| `InjectANcpLuaIndexRangePolyfills` | `false` | Enable only Index/Range |
| `InjectANcpLuaGuardClauses` | `true` | Enable Throw.* guard clauses |

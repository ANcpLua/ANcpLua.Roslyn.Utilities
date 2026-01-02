# Banned APIs

The SDK blocks 50+ dangerous or deprecated APIs at build time, providing suggested alternatives.

## Common Banned APIs

| Banned | Alternative |
|--------|-------------|
| `DateTime.Now` | `TimeProvider.System.GetUtcNow()` |
| `DateTime.UtcNow` | `TimeProvider.System.GetUtcNow()` |
| `DateTimeOffset.Now` | `TimeProvider.System.GetUtcNow()` |
| `ArgumentNullException.ThrowIfNull()` | `Throw.IfNull()` |
| `ArgumentException.ThrowIfNullOrEmpty()` | `Throw.IfNullOrEmpty()` |
| `StringComparison.InvariantCulture` | `StringComparison.Ordinal` |
| `StringComparison.InvariantCultureIgnoreCase` | `StringComparison.OrdinalIgnoreCase` |
| `file.CreationTime` | `file.CreationTimeUtc` |
| `file.LastAccessTime` | `file.LastAccessTimeUtc` |
| `file.LastWriteTime` | `file.LastWriteTimeUtc` |

## Why These Are Banned

### DateTime.Now / DateTime.UtcNow

Direct time access is:
- **Untestable** - Can't mock time in unit tests
- **Inconsistent** - Different results on different machines

Use `TimeProvider` instead:

```csharp
// Inject TimeProvider
public class MyService(TimeProvider time)
{
    public void Process()
    {
        var now = time.GetUtcNow();
    }
}

// In tests
var fakeTime = new FakeTimeProvider();
var service = new MyService(fakeTime);
fakeTime.Advance(TimeSpan.FromHours(1));
```

### ArgumentNullException.ThrowIfNull

The SDK provides `Throw.IfNull` with better diagnostics:

```csharp
// SDK-provided guard
Throw.IfNull(argument);           // CallerArgumentExpression included
Throw.IfNullOrEmpty(str);         // String-specific
Throw.IfNullOrWhiteSpace(str);    // Whitespace check
```

### InvariantCulture

`InvariantCulture` is rarely what you want:
- For machine-readable data: Use `Ordinal`
- For user-facing data: Use `CurrentCulture`

## Suppressing Banned APIs

For legitimate use cases, suppress with a comment:

```csharp
#pragma warning disable RS0030 // Banned API
var legacy = DateTime.Now; // Required for legacy compatibility
#pragma warning restore RS0030
```

Or in `.editorconfig`:

```ini
[**/LegacyCode/**/*.cs]
dotnet_diagnostic.RS0030.severity = none
```

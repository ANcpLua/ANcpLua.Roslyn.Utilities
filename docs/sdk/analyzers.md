# Auto-Injected Analyzers

The SDK automatically includes three analyzer packages, pre-configured for maximum code quality.

## Included Packages

| Package | Purpose |
|---------|---------|
| `ANcpLua.Analyzers` | Custom rules (AL0001-AL0017) |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | API enforcement |
| Comprehensive `.editorconfig` | 100s of rules pre-configured |

## ANcpLua.Analyzers Rules

| Rule | Severity | Description |
|------|----------|-------------|
| AL0001 | Error | Prohibit primary constructor reassignment |
| AL0002 | Warning | Don't repeat negated patterns |
| AL0003 | Error | Don't divide by constant zero |
| AL0004 | Warning | Use pattern matching for Span constants |
| AL0005 | Warning | Use SequenceEqual for Span comparison |
| AL0006 | Warning | Field name conflicts with primary constructor |
| AL0007 | Warning | GetSchema should be explicit |
| AL0008 | Warning | GetSchema must return null |
| AL0009 | Warning | Don't call GetSchema |
| AL0010 | Info | Type should be partial |
| AL0011 | Warning | Avoid lock on non-Lock types |
| AL0012 | Warning | Deprecated OTel attribute |
| AL0013 | Info | Missing telemetry schema URL |
| AL0014 | Info | Prefer pattern matching |
| AL0015 | Info | Normalize null-guard style |
| AL0016 | Info | Combine declaration with null-check |
| AL0017 | Warning | Hardcoded package version |

## Configuration

Override any rule severity in `.editorconfig`:

```ini
[*.cs]
# Disable a rule
dotnet_diagnostic.AL0001.severity = none

# Change severity
dotnet_diagnostic.AL0014.severity = warning

# Suppress in specific files
[**/Generated/**/*.cs]
dotnet_diagnostic.AL0010.severity = none
```

## Related

- [ANcpLua.Analyzers Documentation](https://ancplua.github.io/ANcpLua.Analyzers/)
- [Banned APIs](banned-apis.md)

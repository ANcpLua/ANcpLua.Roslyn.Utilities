; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

 Rule ID | Category | Severity | Notes
---------|----------|----------|----------------------------------------------------------------
 AL0300  | Usage    | Error    | DiscriminatedUnion root must be a partial record
 AL0301  | Usage    | Error    | DiscriminatedUnion root must declare at least one nested partial record case
 AL0302  | Usage    | Error    | DiscriminatedUnion case must be a partial record nested in the union root
 AL0303  | Usage    | Error    | DiscriminatedUnion root must not declare primary-constructor parameters

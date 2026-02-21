; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|------
AL0104  | Usage    | Error    | Handler missing IWorkflowContext parameter
AL0105  | Usage    | Error    | Handler has invalid return type
AL0106  | Usage    | Error    | Executor with [MessageHandler] must be partial
AL0107  | Usage    | Warning  | [MessageHandler] on non-Executor class
AL0108  | Usage    | Error    | Handler has insufficient parameters
AL0109  | Usage    | Info     | ConfigureRoutes already defined
AL0110  | Usage    | Error    | Handler cannot be static

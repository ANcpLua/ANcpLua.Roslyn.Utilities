using ANcpLua.Roslyn.Utilities.Testing.Analysis;

namespace ANcpLua.Roslyn.Utilities.Testing.Formatting;

/// <summary>
///     Formats forbidden type violations for reports.
/// </summary>
internal static class ViolationFormatter
{
    /// <summary>
    ///     Formats violations grouped by step.
    /// </summary>
    public static string FormatGrouped(IReadOnlyList<ForbiddenTypeViolation> violations)
    {
        var sb = new StringBuilder();
        foreach (var group in violations.GroupBy(static v => v.StepName))
        {
            sb.AppendLine($"Step '{group.Key}':");
            foreach (var v in group)
            {
                sb.AppendLine($"  - {v.ForbiddenType.Name} at {v.Path}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    ///     Formats an issue block for violations in a step.
    /// </summary>
    public static string FormatIssueBlock(int issueNumber, IGrouping<string, ForbiddenTypeViolation> group)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Issue #{issueNumber}: Forbidden types in step '{group.Key}'");
        foreach (var v in group)
        {
            sb.AppendLine($"  ✗ {v.ForbiddenType.Name} at {v.Path}");
        }
        return sb.ToString();
    }
}

using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
/// Fluent assertion extensions for <see cref="BuildResult"/>.
/// </summary>
public static class BuildResultAssertions
{
    /// <summary>Asserts the build succeeded (exit code 0).</summary>
    public static BuildResult ShouldSucceed(this BuildResult result, string? because = null)
    {
        Assert.True(result.ExitCode is 0,
            because ?? $"Build should succeed. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the build failed (non-zero exit code).</summary>
    public static BuildResult ShouldFail(this BuildResult result, string? because = null)
    {
        Assert.True(result.ExitCode is not 0,
            because ?? $"Build should fail. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the build has a specific warning.</summary>
    public static BuildResult ShouldHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.True(result.HasWarning(ruleId),
            $"Expected warning {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the build does not have a specific warning.</summary>
    public static BuildResult ShouldNotHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.False(result.HasWarning(ruleId),
            $"Did not expect warning {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the build has a specific error.</summary>
    public static BuildResult ShouldHaveError(this BuildResult result, string ruleId)
    {
        Assert.True(result.HasError(ruleId),
            $"Expected error {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the build does not have a specific error.</summary>
    public static BuildResult ShouldNotHaveError(this BuildResult result, string ruleId)
    {
        Assert.False(result.HasError(ruleId),
            $"Did not expect error {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the output contains a specific string.</summary>
    public static BuildResult ShouldContainOutput(this BuildResult result, string text)
    {
        Assert.True(result.OutputContains(text),
            $"Expected output to contain '{text}'. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts the output does not contain a specific string.</summary>
    public static BuildResult ShouldNotContainOutput(this BuildResult result, string text)
    {
        Assert.True(result.OutputDoesNotContain(text),
            $"Expected output to NOT contain '{text}'. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts a specific MSBuild property has an expected value.</summary>
    public static BuildResult ShouldHavePropertyValue(this BuildResult result, string name, string? expectedValue, bool ignoreCase = true)
    {
        var actual = result.GetMsBuildPropertyValue(name);
        Assert.Equal(expectedValue, actual, ignoreCase);
        return result;
    }

    /// <summary>Asserts a specific MSBuild target was executed.</summary>
    public static BuildResult ShouldHaveExecutedTarget(this BuildResult result, string targetName)
    {
        Assert.True(result.IsMsBuildTargetExecuted(targetName),
            $"Expected target '{targetName}' to be executed. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>Asserts a specific MSBuild target was not executed.</summary>
    public static BuildResult ShouldNotHaveExecutedTarget(this BuildResult result, string targetName)
    {
        Assert.False(result.IsMsBuildTargetExecuted(targetName),
            $"Expected target '{targetName}' to NOT be executed. Output: {result.ProcessOutput}");
        return result;
    }
}

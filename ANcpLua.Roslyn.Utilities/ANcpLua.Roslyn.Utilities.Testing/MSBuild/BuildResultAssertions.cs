using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.MSBuild;

/// <summary>
///     Provides fluent assertion extension methods for <see cref="BuildResult" />.
/// </summary>
/// <remarks>
///     <para>
///         This class enables expressive, chainable assertions for validating MSBuild
///         execution results in integration tests. All assertion methods return the
///         original <see cref="BuildResult" /> to support fluent chaining.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Build success/failure validation</description>
///         </item>
///         <item>
///             <description>Warning and error detection by rule ID</description>
///         </item>
///         <item>
///             <description>Output content verification</description>
///         </item>
///         <item>
///             <description>MSBuild property value validation</description>
///         </item>
///         <item>
///             <description>MSBuild target execution verification</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// await runner.BuildAsync()
///     .ShouldSucceed()
///     .ShouldNotHaveWarning("CS8618")
///     .ShouldHaveExecutedTarget("Build")
///     .ShouldContainOutput("Build succeeded");
/// </code>
/// </example>
/// <seealso cref="BuildResult" />
/// <seealso cref="BuildResult.Succeeded" />
/// <seealso cref="BuildResult.Failed" />
public static class BuildResultAssertions
{
    /// <summary>
    ///     Asserts that the build succeeded with exit code 0.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="because">Optional custom failure message explaining why the build should succeed.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Validates that <see cref="BuildResult.ExitCode" /> equals 0.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.Succeeded" />
    /// <seealso cref="ShouldFail" />
    public static BuildResult ShouldSucceed(this BuildResult result, string? because = null)
    {
        Assert.True(result.ExitCode is 0,
            because ?? $"Build should succeed. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build failed with a non-zero exit code.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="because">Optional custom failure message explaining why the build should fail.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Validates that <see cref="BuildResult.ExitCode" /> is not 0.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.Failed" />
    /// <seealso cref="ShouldSucceed" />
    public static BuildResult ShouldFail(this BuildResult result, string? because = null)
    {
        Assert.True(result.ExitCode is not 0,
            because ?? $"Build should fail. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build produced a warning with the specified rule ID.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="ruleId">The diagnostic rule ID to check for (e.g., "CS8618", "CA1062").</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses SARIF output to detect warnings by rule ID.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.HasWarning(string)" />
    /// <seealso cref="ShouldNotHaveWarning" />
    /// <seealso cref="ShouldHaveError" />
    public static BuildResult ShouldHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.True(result.HasWarning(ruleId),
            $"Expected warning {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build did not produce a warning with the specified rule ID.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="ruleId">The diagnostic rule ID that should not be present (e.g., "CS8618", "CA1062").</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses SARIF output to verify absence of the warning.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.HasWarning(string)" />
    /// <seealso cref="ShouldHaveWarning" />
    /// <seealso cref="ShouldNotHaveError" />
    public static BuildResult ShouldNotHaveWarning(this BuildResult result, string ruleId)
    {
        Assert.False(result.HasWarning(ruleId),
            $"Did not expect warning {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build produced an error with the specified rule ID.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="ruleId">The diagnostic rule ID to check for (e.g., "CS0246", "CA2000").</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses SARIF output to detect errors by rule ID.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.HasError(string)" />
    /// <seealso cref="ShouldNotHaveError" />
    /// <seealso cref="ShouldHaveWarning" />
    public static BuildResult ShouldHaveError(this BuildResult result, string ruleId)
    {
        Assert.True(result.HasError(ruleId),
            $"Expected error {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build did not produce an error with the specified rule ID.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="ruleId">The diagnostic rule ID that should not be present (e.g., "CS0246", "CA2000").</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Uses SARIF output to verify absence of the error.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.HasError(string)" />
    /// <seealso cref="ShouldHaveError" />
    /// <seealso cref="ShouldNotHaveWarning" />
    public static BuildResult ShouldNotHaveError(this BuildResult result, string ruleId)
    {
        Assert.False(result.HasError(ruleId),
            $"Did not expect error {ruleId}. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build output contains the specified text.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="text">The text that should be present in the output.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Performs a case-sensitive ordinal comparison by default.</description>
    ///         </item>
    ///         <item>
    ///             <description>Searches across all lines of process output.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.OutputContains" />
    /// <seealso cref="ShouldNotContainOutput" />
    public static BuildResult ShouldContainOutput(this BuildResult result, string text)
    {
        Assert.True(result.OutputContains(text),
            $"Expected output to contain '{text}'. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that the build output does not contain the specified text.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="text">The text that should not be present in the output.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Performs a case-sensitive ordinal comparison by default.</description>
    ///         </item>
    ///         <item>
    ///             <description>Searches across all lines of process output.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.OutputDoesNotContain" />
    /// <seealso cref="ShouldContainOutput" />
    public static BuildResult ShouldNotContainOutput(this BuildResult result, string text)
    {
        Assert.True(result.OutputDoesNotContain(text),
            $"Expected output to NOT contain '{text}'. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that an MSBuild property has the expected value.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="name">The name of the MSBuild property to check.</param>
    /// <param name="expectedValue">The expected value of the property, or <c>null</c> if the property should not exist.</param>
    /// <param name="ignoreCase">
    ///     If <c>true</c>, performs a case-insensitive comparison; otherwise, performs a case-sensitive comparison.
    ///     Defaults to <c>true</c>.
    /// </param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Extracts property values from the binary log.</description>
    ///         </item>
    ///         <item>
    ///             <description>Retrieves the last evaluated value of the property.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.GetMsBuildPropertyValue" />
    public static BuildResult ShouldHavePropertyValue(this BuildResult result, string name, string? expectedValue,
        bool ignoreCase = true)
    {
        var actual = result.GetMsBuildPropertyValue(name);
        Assert.Equal(expectedValue, actual, ignoreCase);
        return result;
    }

    /// <summary>
    ///     Asserts that a specific MSBuild target was executed during the build.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="targetName">The name of the MSBuild target that should have been executed.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Analyzes the binary log to determine target execution.</description>
    ///         </item>
    ///         <item>
    ///             <description>A target is considered executed if it was invoked and not skipped.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.IsMsBuildTargetExecuted" />
    /// <seealso cref="ShouldNotHaveExecutedTarget" />
    public static BuildResult ShouldHaveExecutedTarget(this BuildResult result, string targetName)
    {
        Assert.True(result.IsMsBuildTargetExecuted(targetName),
            $"Expected target '{targetName}' to be executed. Output: {result.ProcessOutput}");
        return result;
    }

    /// <summary>
    ///     Asserts that a specific MSBuild target was not executed during the build.
    /// </summary>
    /// <param name="result">The build result to validate.</param>
    /// <param name="targetName">The name of the MSBuild target that should not have been executed.</param>
    /// <returns>The same <see cref="BuildResult" /> instance for fluent chaining.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Analyzes the binary log to determine target execution.</description>
    ///         </item>
    ///         <item>
    ///             <description>Passes if the target was either not invoked or was skipped.</description>
    ///         </item>
    ///         <item>
    ///             <description>On failure, includes the full process output in the assertion message.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <seealso cref="BuildResult.IsMsBuildTargetExecuted" />
    /// <seealso cref="ShouldHaveExecutedTarget" />
    public static BuildResult ShouldNotHaveExecutedTarget(this BuildResult result, string targetName)
    {
        Assert.False(result.IsMsBuildTargetExecuted(targetName),
            $"Expected target '{targetName}' to NOT be executed. Output: {result.ProcessOutput}");
        return result;
    }
}
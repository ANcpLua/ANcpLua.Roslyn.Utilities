using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing;

/// <summary>
///     Provides fluent assertion extension methods for <see cref="CompileResult" />.
/// </summary>
/// <remarks>
///     <para>
///         This class enables expressive, chainable assertions for validating compilation
///         results in tests. All assertion methods return the original <see cref="CompileResult" />
///         to support fluent chaining.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// Compile.Source(code)
///     .Build()
///     .ShouldSucceed()
///     .ShouldHaveNoWarnings()
///     .ShouldContainType("MyNamespace.MyClass");
/// </code>
/// </example>
/// <seealso cref="CompileResult" />
/// <seealso cref="Compile" />
public static class CompileResultAssertions
{
    /// <summary>
    ///     Asserts that the compilation succeeded.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldSucceed(this CompileResult result, string? because = null)
    {
        Assert.True(result.Succeeded,
            because ?? $"Compilation should succeed. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation failed.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="because">Optional custom failure message.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldFail(this CompileResult result, string? because = null)
    {
        Assert.True(result.Failed,
            because ?? "Compilation should fail but succeeded.");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation has no warnings.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveNoWarnings(this CompileResult result)
    {
        Assert.True(result.Warnings.Count is 0,
            $"Expected no warnings, got {result.Warnings.Count}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation produced an error with the specified ID.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="errorId">The diagnostic ID to check for.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveError(this CompileResult result, string errorId)
    {
        Assert.True(result.HasError(errorId),
            $"Expected error {errorId}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation produced a warning with the specified ID.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="warningId">The diagnostic ID to check for.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveWarning(this CompileResult result, string warningId)
    {
        Assert.True(result.HasWarning(warningId),
            $"Expected warning {warningId}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that the compilation has at least N errors.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="count">The minimum number of errors expected.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldHaveErrors(this CompileResult result, int count)
    {
        Assert.True(result.Errors.Count >= count,
            $"Expected at least {count} errors, got {result.Errors.Count}. Diagnostics:\n{result.FormatDiagnostics()}");
        return result;
    }

    /// <summary>
    ///     Asserts that a type exists in the compiled assembly.
    /// </summary>
    /// <param name="result">The compilation result to validate.</param>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The same <see cref="CompileResult" /> instance for fluent chaining.</returns>
    public static CompileResult ShouldContainType(this CompileResult result, string typeName)
    {
        result.ShouldSucceed();
        Assert.True(result.ContainsType(typeName),
            $"Type '{typeName}' not found in compiled assembly.");
        return result;
    }
}
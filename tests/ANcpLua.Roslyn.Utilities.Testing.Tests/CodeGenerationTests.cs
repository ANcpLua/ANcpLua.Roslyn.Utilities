using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using System;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class CodeGenerationTests
{
    [Theory]
    [InlineData(new[] { "CS0168", "CA1822" }, "CS0168, CA1822")]
    [InlineData(new[] { "0168", "CS0168", "IDE0051" }, "0168, CS0168, IDE0051")]
    public void SuppressWarnings_And_RestoreWarnings_RenderPragmasForValidInputs(string[] warningIds, string rendered)
    {
        GeneratedCodeHelpers.SuppressWarnings(warningIds).Should().Be($"#pragma warning disable {rendered}");
        GeneratedCodeHelpers.RestoreWarnings(warningIds).Should().Be($"#pragma warning restore {rendered}");
    }

    [Fact]
    public void SuppressWarnings_RejectsUnsafeWarningIdValues()
    {
        ((Action)(() => GeneratedCodeHelpers.SuppressWarnings(string.Empty))).Should().Throw<ArgumentException>();
        ((Action)(() => GeneratedCodeHelpers.SuppressWarnings("CS 0168"))).Should().Throw<ArgumentException>();
        ((Action)(() => GeneratedCodeHelpers.SuppressWarnings("CS0168\nCS0169"))).Should().Throw<ArgumentException>();
        ((Action)(() => GeneratedCodeHelpers.SuppressWarnings("CS0168,CA1822"))).Should().Throw<ArgumentException>();
    }
}

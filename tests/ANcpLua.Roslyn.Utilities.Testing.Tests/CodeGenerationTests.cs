using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using System;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class CodeGenerationTests
{
    [Fact]
    public void SuppressWarnings_And_RestoreWarnings_RenderPragmasForValidInputs()
    {
        GeneratedCodeHelpers.SuppressWarnings("CS0168", "CA1822").Should().Be("#pragma warning disable CS0168, CA1822");
        GeneratedCodeHelpers.RestoreWarnings("CS0168", "IDE0051").Should().Be("#pragma warning restore CS0168, IDE0051");
    }

    [Fact]
    public void SuppressWarnings_AcceptsNumericAndWellKnownPrefixes()
    {
        GeneratedCodeHelpers.SuppressWarnings("0168", "CS0168", "IDE0051").Should().Be("#pragma warning disable 0168, CS0168, IDE0051");
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

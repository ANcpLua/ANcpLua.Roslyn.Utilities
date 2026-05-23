using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class StringComparisonExtensionsTests
{
    [Fact]
    public void TruncateWithEllipsis_ReturnsNullForNullInput()
    {
        ((string?)null).TruncateWithEllipsis(3).Should().BeNull();
    }

    [Fact]
    public void TruncateWithEllipsis_ReturnsEmptyForNonPositiveMaxLength()
    {
        "abc".TruncateWithEllipsis(0).Should().Be(string.Empty);
        "abc".TruncateWithEllipsis(-5).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("abc", 5, "abc")]
    [InlineData("hello", 5, "hello")]
    public void TruncateWithEllipsis_LeavesShortValuesUnchanged(string value, int maxLength, string expected)
    {
        value.TruncateWithEllipsis(maxLength).Should().Be(expected);
    }

    [Fact]
    public void TruncateWithEllipsis_TruncatesWithUnicodeEllipsis()
    {
        "hello world".TruncateWithEllipsis(8, "…").Should().Be("hello w…");
    }

    [Fact]
    public void TruncateWithEllipsis_RespectsEllipsisLengthWhenMaxLengthIsSmall()
    {
        "hello world".TruncateWithEllipsis(2, "...").Should().Be("..");
    }
}

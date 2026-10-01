using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class StringComparisonExtensionsTests
{
    [Theory]
    [InlineData(null, 3, "...", null)]
    [InlineData("abc", 0, "...", "")]
    [InlineData("abc", -5, "...", "")]
    [InlineData("abc", 5, "...", "abc")]
    [InlineData("hello", 5, "...", "hello")]
    [InlineData("hello world", 8, "…", "hello w…")]
    [InlineData("hello world", 2, "...", "..")]
    public void TruncateWithEllipsis_FitsResultAndEllipsisIntoMaxLength(string? value, int maxLength, string ellipsis,
        string? expected) =>
        value.TruncateWithEllipsis(maxLength, ellipsis).Should().Be(expected);
}

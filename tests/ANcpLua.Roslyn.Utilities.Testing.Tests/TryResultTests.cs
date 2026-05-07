using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TryResultTests
{
    [Fact]
    public void Fail_DefaultsAllOutsAndReturnsFalse()
    {
        TryResult.Fail(out string? a).Should().BeFalse();
        a.Should().BeNull();

        TryResult.Fail(out int value).Should().BeFalse();
        value.Should().Be(default);

        TryResult.Fail(out string? b1, out int? b2).Should().BeFalse();
        b1.Should().BeNull();
        b2.Should().BeNull();

        TryResult.Fail(out string? c1, out int? c2, out double? c3).Should().BeFalse();
        c1.Should().BeNull();
        c2.Should().BeNull();
        c3.Should().BeNull();
    }

    [Fact]
    public void FailWithReturnValue_PropagatesReturnAndDefaultsOuts()
    {
        TryResult.Fail(ParseStatus.EmptyInput, out string? a).Should().Be(ParseStatus.EmptyInput);
        a.Should().BeNull();

        TryResult.Fail(-1, out int? b1, out string? b2).Should().Be(-1);
        b1.Should().BeNull();
        b2.Should().BeNull();

        TryResult.Fail("err", out int? c1, out int? c2, out int? c3).Should().Be("err");
        c1.Should().BeNull();
        c2.Should().BeNull();
        c3.Should().BeNull();
    }

    [Fact]
    public void Ok_AssignsValuesAndReturnsTrue()
    {
        TryResult.Ok(42, out int a).Should().BeTrue();
        a.Should().Be(42);

        TryResult.Ok(1, "x", out int b1, out string b2).Should().BeTrue();
        b1.Should().Be(1);
        b2.Should().Be("x");

        TryResult.Ok(1, "x", 3.14, out int c1, out string c2, out double c3).Should().BeTrue();
        c1.Should().Be(1);
        c2.Should().Be("x");
        c3.Should().Be(3.14);
    }

    [Fact]
    public void ComposesIntoReadableTryParse()
    {
        TryParseTwoInts("1,2", out var a, out var b).Should().BeTrue();
        a.Should().Be(1);
        b.Should().Be(2);

        TryParseTwoInts("oops", out a, out b).Should().BeFalse();
        a.Should().BeNull();
        b.Should().BeNull();

        TryParseTwoInts("1,bad", out a, out b).Should().BeFalse();
        a.Should().BeNull();
        b.Should().BeNull();
    }

    private static bool TryParseTwoInts(string input, out int? first, out int? second)
    {
        var parts = input.Split(',');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var parsedFirst)
            || !int.TryParse(parts[1], out var parsedSecond))
            return TryResult.Fail(out first, out second);

        return TryResult.Ok<int?, int?>(parsedFirst, parsedSecond, out first, out second);
    }

    private enum ParseStatus
    {
        Ok,
        EmptyInput,
        Invalid
    }
}

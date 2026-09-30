using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class DeltaTests
{
    private static EquatableArray<int> Eq(params int[] items) => ImmutableArray.Create(items).AsEquatableArray();

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 2, 3, 4, 5 }, new[] { 4, 5 })]
    [InlineData(new int[0], new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
    [InlineData(new[] { 1, 2, 3 }, new int[0], new int[0])]
    [InlineData(new[] { 1, 2, 3, 4 }, new[] { 2, 3 }, new int[0])]
    [InlineData(new[] { 1, 2 }, new[] { 3, 4 }, new[] { 3, 4 })]
    [InlineData(new[] { 1 }, new[] { 3, 1, 2 }, new[] { 3, 2 })]
    [InlineData(new[] { 1 }, new[] { 2, 1, 2, 3 }, new[] { 2, 2, 3 })]
    [InlineData(new[] { 1, 2, 3 }, new[] { 3, 4 }, new[] { 4 })]
    [InlineData(new[] { 3, 4 }, new[] { 1, 2, 3 }, new[] { 1, 2 })]
    public void Difference_KeepsItemsOfSecondMissingFromFirst_InOrderWithDuplicates(int[] first, int[] second,
        int[] expected) =>
        Delta.Difference(Eq(first), Eq(second)).AsImmutableArray().Should().Equal(expected);

    [Fact]
    public void Difference_DefaultImmutableArrayInputs_TreatedAsEmpty()
    {
        Delta.Difference(default(ImmutableArray<int>), default).IsDefaultOrEmpty.Should().BeTrue();
        Delta.Difference(default, Eq(1, 2).AsImmutableArray()).AsImmutableArray().Should().Equal(1, 2);
    }

    [Fact]
    public void Compute_ReportsAddedAndRemoved()
    {
        var delta = Delta.Compute(Eq(1, 2, 3), Eq(2, 3, 4));

        delta.Added.AsImmutableArray().Should().Equal(4);
        delta.Removed.AsImmutableArray().Should().Equal(1);
        delta.HasChanges.Should().BeTrue();
        delta.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Compute_SameSetInDifferentOrder_IsEmpty()
    {
        var delta = Delta.Compute(Eq(1, 2, 3), Eq(3, 2, 1));

        delta.IsEmpty.Should().BeTrue();
        delta.HasChanges.Should().BeFalse();
        delta.Added.IsDefaultOrEmpty.Should().BeTrue();
        delta.Removed.IsDefaultOrEmpty.Should().BeTrue();
    }

    [Fact]
    public void SetDelta_IsValueEquatable()
    {
        var a = Delta.Compute(Eq(1, 2), Eq(2, 3));
        var b = Delta.Compute(Eq(1, 2), Eq(2, 3));
        var c = Delta.Compute(Eq(1, 2), Eq(2, 4));

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    private sealed record Sym(string Name);

    [Fact]
    public void Compute_WorksWithValueEquatableRecordModels()
    {
        var previous = ImmutableArray.Create(new Sym("A"), new Sym("B")).AsEquatableArray();
        var current = ImmutableArray.Create(new Sym("B"), new Sym("C")).AsEquatableArray();

        var delta = Delta.Compute(previous, current);

        delta.Added.AsImmutableArray().Should().Equal(new Sym("C"));
        delta.Removed.AsImmutableArray().Should().Equal(new Sym("A"));
    }
}

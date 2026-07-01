using System.Collections.Immutable;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class DeltaTests
{
    private static EquatableArray<int> Eq(params int[] items) => ImmutableArray.Create(items).AsEquatableArray();

    [Fact]
    public void Difference_ReturnsItemsInSecondNotInFirst()
    {
        var result = Delta.Difference(Eq(1, 2, 3), Eq(2, 3, 4, 5));

        result.AsImmutableArray().Should().Equal(4, 5);
    }

    [Fact]
    public void Difference_FirstEmpty_ReturnsAllOfSecond()
    {
        Delta.Difference(Eq(), Eq(1, 2, 3)).AsImmutableArray().Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Difference_SecondEmpty_ReturnsEmpty()
    {
        Delta.Difference(Eq(1, 2, 3), Eq()).IsDefaultOrEmpty.Should().BeTrue();
    }

    [Fact]
    public void Difference_AllOfSecondPresentInFirst_ReturnsEmpty()
    {
        Delta.Difference(Eq(1, 2, 3, 4), Eq(2, 3)).IsDefaultOrEmpty.Should().BeTrue();
    }

    [Fact]
    public void Difference_DisjointInputs_ReturnsAllOfSecond()
    {
        Delta.Difference(Eq(1, 2), Eq(3, 4)).AsImmutableArray().Should().Equal(3, 4);
    }

    [Fact]
    public void Difference_PreservesOrderOfSecond()
    {
        Delta.Difference(Eq(9), Eq(3, 1, 2)).AsImmutableArray().Should().Equal(3, 1, 2);
    }

    [Fact]
    public void Difference_PreservesDuplicatesInSecond_WhenAbsentFromFirst()
    {
        // Documented behavior: filters second, does not deduplicate.
        Delta.Difference(Eq(1), Eq(2, 2, 3)).AsImmutableArray().Should().Equal(2, 2, 3);
    }

    [Fact]
    public void Difference_IsNotSymmetric()
    {
        var forward = Delta.Difference(Eq(1, 2, 3), Eq(3, 4)).AsImmutableArray();
        var backward = Delta.Difference(Eq(3, 4), Eq(1, 2, 3)).AsImmutableArray();

        forward.Should().Equal(4);
        backward.Should().Equal(1, 2);
    }

    [Fact]
    public void Difference_ImmutableArrayAndEquatableArrayOverloads_Agree()
    {
        var first = ImmutableArray.Create(1, 2, 3);
        var second = ImmutableArray.Create(2, 3, 4);

        var viaImmutable = Delta.Difference(first, second);
        var viaEquatable = Delta.Difference(first.AsEquatableArray(), second.AsEquatableArray());

        viaImmutable.Equals(viaEquatable).Should().BeTrue();
        viaImmutable.AsImmutableArray().Should().Equal(4);
    }

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

using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class CachingHintBuilderTests
{
    [Theory]
    [InlineData(ModelEqualityKind.Equatable, "IEquatable<T> is implemented")]
    [InlineData(ModelEqualityKind.EqualsOverride, "lacks IEquatable<T>")]
    [InlineData(ModelEqualityKind.ReferenceEquality, "no value equality")]
    [InlineData(ModelEqualityKind.Unknown, "equality broke")]
    public void Modified_HintMatchesEqualityKind(object kind, string expected) =>
        CachingHintBuilder.BuildHint((ModelEqualityKind)kind, modified: 1, @new: 0, removed: 0)
            .Should().Contain(expected);

    [Fact]
    public void New_Outputs_HintReportsCollectionShapeChange() =>
        CachingHintBuilder.BuildHint(ModelEqualityKind.Equatable, modified: 0, @new: 1, removed: 0)
            .Should().Contain("new outputs appeared").And.NotContain("IEquatable");

    [Fact]
    public void Removed_Outputs_HintReportsCollectionShapeChange() =>
        CachingHintBuilder.BuildHint(ModelEqualityKind.Equatable, modified: 0, @new: 0, removed: 1)
            .Should().Contain("outputs removed");

    [Fact]
    public void Modified_TakesPriorityOverNewAndRemoved() =>
        CachingHintBuilder.BuildHint(ModelEqualityKind.Equatable, modified: 1, @new: 1, removed: 1)
            .Should().Contain("IEquatable<T> is implemented");

    [Fact]
    public void Legend_NamesAllFiveCounters() =>
        CachingHintBuilder.Legend.Should()
            .Contain("C=Cached")
            .And.Contain("U=Unchanged")
            .And.Contain("M=Modified")
            .And.Contain("N=New")
            .And.Contain("R=Removed");
}

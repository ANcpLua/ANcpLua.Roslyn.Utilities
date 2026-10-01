using System.Text.RegularExpressions;
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

    [Theory]
    [InlineData(0, 1, 0, "new outputs appeared")]
    [InlineData(0, 0, 1, "outputs removed")]
    [InlineData(1, 1, 1, "IEquatable<T> is implemented")]
    public void BuildHint_ModifiedOutranksNewOutranksRemoved(int modified, int @new, int removed, string expected) =>
        CachingHintBuilder.BuildHint(ModelEqualityKind.Equatable, modified, @new, removed).Should().Contain(expected);

    [Fact]
    public void Legend_ExplainsEveryBreakdownCounter()
    {
        var breakdown = new GeneratorStepAnalysis("Step", 1, 2, 3, 4, 5, outputType: null, hasForbiddenTypes: false)
            .FormatBreakdown();

        var counters = Regex.Matches(breakdown, "([A-Z]):").Select(static match => match.Groups[1].Value).ToList();

        counters.Should().HaveCount(5)
            .And.OnlyContain(static counter => CachingHintBuilder.Legend.Contains(counter + "=", StringComparison.Ordinal));
    }
}

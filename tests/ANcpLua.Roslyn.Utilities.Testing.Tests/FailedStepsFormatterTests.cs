using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using ANcpLua.Roslyn.Utilities.Testing.Formatting;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

/// <summary>
///     End-to-end coverage of <see cref="AssertionHelpers.FormatFailedSteps" /> for the three
///     equality branches and the new legend / model-type surfacing.
/// </summary>
public sealed class FailedStepsFormatterTests
{
    private static string Format(Type? outputType, string stepName = "Step") =>
        AssertionHelpers.FormatFailedSteps([
            new GeneratorStepAnalysis(stepName, 0, 0, modified: 1, 0, 0, outputType, hasForbiddenTypes: false)
        ]);

    [Theory]
    [InlineData(typeof(EquatableButBrokenModel), "IEquatable<T> is implemented")]
    [InlineData(typeof(NoEqualityModel), "no value equality")]
    [InlineData(typeof(EqualsOverrideModel), "lacks IEquatable<T>")]
    public void Output_HintMatchesModelEqualityKind(Type model, string expected) =>
        Format(model).Should().Contain(expected);

    /// <summary>
    ///     Regression test for the ErrorOrContext bug: a model that implements IEquatable&lt;T&gt;
    ///     must not be told it "lacks IEquatable&lt;T&gt;".
    /// </summary>
    [Fact]
    public void EquatableModel_DoesNotClaimLacksIEquatable() =>
        Format(typeof(EquatableButBrokenModel)).Should().NotContain("model lacks IEquatable<T>");

    [Fact]
    public void Output_IncludesModelTypeName() =>
        Format(typeof(EquatableButBrokenModel)).Should().Contain(nameof(EquatableButBrokenModel));

    [Fact]
    public void Output_IncludesLegend() =>
        Format(typeof(EquatableButBrokenModel)).Should().Contain(CachingHintBuilder.Legend);

    [Fact]
    public void UnknownOutputType_FallsBackGracefully() =>
        Format(outputType: null, stepName: "EmptyStep").Should()
            .Contain("EmptyStep").And.Contain("equality broke");

    private sealed class EquatableButBrokenModel : IEquatable<EquatableButBrokenModel>
    {
        public bool Equals(EquatableButBrokenModel? other) => false;
        public override bool Equals(object? obj) => Equals(obj as EquatableButBrokenModel);
        public override int GetHashCode() => 0;
    }

    private sealed class NoEqualityModel;

    private sealed class EqualsOverrideModel
    {
        public override bool Equals(object? obj) => obj is EqualsOverrideModel;
        public override int GetHashCode() => 0;
    }
}

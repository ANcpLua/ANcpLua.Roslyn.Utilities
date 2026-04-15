using ANcpLua.Roslyn.Utilities.Testing.Analysis;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class ModelEqualityClassifierTests
{
    [Theory]
    [InlineData(null, ModelEqualityKind.Unknown)]
    [InlineData(typeof(PlainClass), ModelEqualityKind.ReferenceEquality)]
    [InlineData(typeof(PlainStruct), ModelEqualityKind.ReferenceEquality)]
    [InlineData(typeof(EqualsOnlyClass), ModelEqualityKind.EqualsOverride)]
    [InlineData(typeof(EquatableForOtherType), ModelEqualityKind.EqualsOverride)]
    [InlineData(typeof(EquatableClass), ModelEqualityKind.Equatable)]
    [InlineData(typeof(EquatableStruct), ModelEqualityKind.Equatable)]
    [InlineData(typeof(RecordModel), ModelEqualityKind.Equatable)]
    public void Classify_ReturnsExpectedKind(Type? type, object expected) =>
        ModelEqualityClassifier.Classify(type).Should().Be((ModelEqualityKind)expected);

    private sealed class PlainClass;

    private readonly struct PlainStruct;

    private sealed class EqualsOnlyClass
    {
        public override bool Equals(object? obj) => obj is EqualsOnlyClass;
        public override int GetHashCode() => 0;
    }

    private sealed class EquatableClass : IEquatable<EquatableClass>
    {
        public bool Equals(EquatableClass? other) => other is not null;
        public override bool Equals(object? obj) => Equals(obj as EquatableClass);
        public override int GetHashCode() => 0;
    }

    private readonly struct EquatableStruct : IEquatable<EquatableStruct>
    {
        public bool Equals(EquatableStruct other) => true;
        public override bool Equals(object? obj) => obj is EquatableStruct s && Equals(s);
        public override int GetHashCode() => 0;
    }

    private sealed record RecordModel(int Value);

    // IEquatable<SomethingElse> does not help Roslyn cache this type — so it must classify on its
    // object.Equals override, not on the foreign IEquatable.
    private sealed class EquatableForOtherType : IEquatable<PlainClass>
    {
        public bool Equals(PlainClass? other) => false;
        public override bool Equals(object? obj) => false;
        public override int GetHashCode() => 0;
    }
}

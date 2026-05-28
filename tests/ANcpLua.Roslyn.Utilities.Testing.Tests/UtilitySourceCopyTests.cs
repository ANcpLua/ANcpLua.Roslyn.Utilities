using System.Collections;
using System.Text;
using System.Threading;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class UtilitySourceCopyTests
{
    [Fact]
    public void DelegateDisposable_DisposeRunsCallbackOnce()
    {
        var count = 0;
        var disposable = new DelegateDisposable(() => count++);

        disposable.Dispose();
        disposable.Dispose();

        count.Should().Be(1);
    }

    [Fact]
    public void ReaderWriterLockSlimExtensions_ExitEnteredLockOnDispose()
    {
        using var rwLock = new ReaderWriterLockSlim();

        using (rwLock.WithReaderLock())
        {
            rwLock.IsReadLockHeld.Should().BeTrue();
        }

        rwLock.IsReadLockHeld.Should().BeFalse();

        var writeScope = rwLock.WithWriterLock();
        var writeScopeAlias = writeScope;
        rwLock.IsWriteLockHeld.Should().BeTrue();
        writeScope.Dispose();
        writeScopeAlias.Dispose();
        rwLock.IsWriteLockHeld.Should().BeFalse();
    }

    [Fact]
    public void WeakReferenceExtensions_ReturnLiveTargets()
    {
        var target = new object();
        var weakReference = new WeakReference(target);
        var genericWeakReference = new WeakReference<object>(target);

        weakReference.TryGetTarget(out var found).Should().BeTrue();
        found.Should().BeSameAs(target);
        weakReference.GetTargetOrDefault().Should().BeSameAs(target);
        genericWeakReference.GetTargetOrDefault().Should().BeSameAs(target);
    }

    [Fact]
    public void WeakReferenceExtensions_ReturnNullForMissingNonGenericTarget()
    {
        var weakReference = new WeakReference(null);

        weakReference.TryGetTarget(out var target).Should().BeFalse();
        target.Should().BeNull();
        weakReference.GetTargetOrDefault().Should().BeNull();
    }

    [Fact]
    public void FormattingExtensions_CopyAndClear()
    {
        Span<char> destination = stackalloc char[5];

        "hello".TryCopyTo(destination, out var charsWritten).Should().BeTrue();
        charsWritten.Should().Be(5);
        destination.ToString().Should().Be("hello");

        Span<char> tooSmall = stackalloc char[4];
        "hello".TryCopyTo(tooSmall, out charsWritten).Should().BeFalse();
        charsWritten.Should().Be(0);

        var builder = new StringBuilder("abc");
        builder.ToStringAndClear().Should().Be("abc");
        builder.Length.Should().Be(0);
    }

    [Fact]
    public void EmptyCollectionHelpers_ReturnCachedEmptyInstances()
    {
        var collection = ReadOnlyCollection.Empty<int>();
        var dictionary = ReadOnlyDictionary.Empty<string, int>();

        collection.Should().BeEmpty();
        dictionary.Should().BeEmpty();
        collection.Should().BeSameAs(ReadOnlyCollection.Empty<int>());
        dictionary.Should().BeSameAs(ReadOnlyDictionary.Empty<string, int>());
    }

    [Fact]
    public void EmptyEnumerators_NeverMoveNext()
    {
        Enumerator.Empty().MoveNext().Should().BeFalse();
        Enumerator.Empty<int>().MoveNext().Should().BeFalse();
    }

    [Fact]
    public void ToDictionaryEnumerator_AdaptsKeyValuePairEnumerator()
    {
        using var inner = new[] { new KeyValuePair<string, int>("answer", 42) }
            .AsEnumerable()
            .GetEnumerator();

        var enumerator = inner.ToDictionaryEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Key.Should().Be("answer");
        enumerator.Value.Should().Be(42);
        enumerator.Entry.Should().Be(new DictionaryEntry("answer", 42));
        ((IDisposable)enumerator).Dispose();
    }

    [Fact]
    public void EnumerableCompatibilityAliases_MatchExpectedBehavior()
    {
        string?[] values = ["a", null, "b"];
        int?[] numbers = [1, null, 2];

        values.NotNull().Should().Equal("a", "b");
        numbers.NotNull().Should().Equal(1, 2);
        new[] { "only" }.Only().Should().Be("only");
        new[] { 1, 2, 3 }.Only(static value => value == 2).Should().Be(2);
        Array.Empty<int>().OnlyOrDefault().Should().Be(0);
        new[] { 1, 2 }.OnlyOrDefault().Should().Be(0);
        new[] { 1, 2, 3 }.Join('|').Should().Be("1|2|3");
        new[] { 1, 2, 3 }.JoinToString(", ").Should().Be("1, 2, 3");
    }

    [Fact]
    public void Only_ThrowsWhenSequenceDoesNotContainExactlyOneElement()
    {
        var empty = () => Array.Empty<int>().Only();
        var multiple = () => new[] { 1, 2 }.Only();

        empty.Should().Throw<ArgumentException>();
        multiple.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnumerableCompatibilityAliases_UseGuardForNullInputs()
    {
        var nullReferenceNotNull = () => ((IEnumerable<string?>)null!).NotNull();
        var nullValueNotNull = () => ((IEnumerable<int?>)null!).NotNull();
        var nullOnly = () => ((IEnumerable<int>)null!).Only();
        var nullOnlyPredicate = () => new[] { 1 }.Only(null!);
        var nullJoin = () => ((IEnumerable<int>)null!).JoinToString(",");

        nullReferenceNotNull.Should().Throw<ArgumentNullException>();
        nullValueNotNull.Should().Throw<ArgumentNullException>();
        nullOnly.Should().Throw<ArgumentNullException>();
        nullOnlyPredicate.Should().Throw<ArgumentNullException>();
        nullJoin.Should().Throw<ArgumentNullException>();
    }
}

using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
/// Murmur3-based hash combiner for implementing GetHashCode in equatable types.
/// This is a mutable struct for performance; use it in a single method scope.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
struct HashCombiner
{
    private uint _len;
    private uint _hash;

    public readonly int HashCode
    {
        get
        {
            unchecked
            {
                var result = _hash ^ _len;
                result ^= result >> 16;
                result *= 0x85ebca6b;
                result ^= result >> 13;
                result *= 0xc2b2ae35;
                result ^= result >> 16;
                return (int)result;
            }
        }
    }

    public static HashCombiner Create(int seed = unchecked((int)2166136261))
    {
        var result = new HashCombiner();
        result._hash = unchecked((uint)seed);
        return result;
    }

    public void Add(int value)
    {
        unchecked
        {
            _len += 4;
            var k = (uint)value;
            k *= 0xcc9e2d51;
            k = RotateLeft(k, 15);
            k *= 0x1b873593;
            _hash ^= k;
            _hash = RotateLeft(_hash, 13);
            _hash *= 5;
            _hash += 0xe6546b64;
        }
    }

    public void Add<T>(T? value) =>
        Add(value?.GetHashCode() ?? 0);

    public void Add(bool value) =>
        Add(value ? 1 : 0);

    public void Add(string? value, StringComparer? comparer = null)
    {
        if (value is null)
        {
            Add(0);
            return;
        }

        comparer ??= StringComparer.Ordinal;
        Add(comparer.GetHashCode(value));
    }

    public void AddRange<T>(IEnumerable<T>? values)
    {
        if (values is null)
        {
            Add(0);
            return;
        }

        foreach (var value in values)
            Add(value);
    }

    public void AddRange<T>(ReadOnlySpan<T> values)
    {
        foreach (var value in values)
            Add(value);
    }

    public readonly int ToHashCode() => HashCode;

    private static uint RotateLeft(uint value, int bits) =>
        (value << bits) | (value >> (32 - bits));
}

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class EqualityExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) where T : IEquatable<T> =>
        source.ToImmutableArray().AsEquatableArray();

    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> source) where T : IEquatable<T> =>
        source.AsEquatableArray();

    /// <summary>
    ///     Compares two immutable arrays for sequence equality.
    ///     Treats default (uninitialized) and empty arrays as equivalent.
    /// </summary>
    public static bool SequenceEquals<T>(this ImmutableArray<T> left, ImmutableArray<T> right)
    {
        // Treat default and empty as equivalent (null = empty pattern)
        var leftEmpty = left.IsDefaultOrEmpty;
        var rightEmpty = right.IsDefaultOrEmpty;

        if (leftEmpty && rightEmpty)
            return true;
        if (leftEmpty || rightEmpty)
            return false;

        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Compares two immutable arrays for sequence equality using a custom comparer.
    ///     Treats default (uninitialized) and empty arrays as equivalent.
    /// </summary>
    public static bool SequenceEquals<T>(this ImmutableArray<T> left, ImmutableArray<T> right, IEqualityComparer<T> comparer)
    {
        // Treat default and empty as equivalent (null = empty pattern)
        var leftEmpty = left.IsDefaultOrEmpty;
        var rightEmpty = right.IsDefaultOrEmpty;

        if (leftEmpty && rightEmpty)
            return true;
        if (leftEmpty || rightEmpty)
            return false;

        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (!comparer.Equals(left[i], right[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Gets a hash code for the sequence of items.
    ///     Returns 0 for empty or default arrays (they are equivalent).
    /// </summary>
    public static int GetSequenceHashCode<T>(this ImmutableArray<T> array)
    {
        if (array.IsDefaultOrEmpty)
            return 0;

        var hash = HashCombiner.Create();
        foreach (var item in array)
            hash.Add(item);
        return hash.HashCode;
    }
}

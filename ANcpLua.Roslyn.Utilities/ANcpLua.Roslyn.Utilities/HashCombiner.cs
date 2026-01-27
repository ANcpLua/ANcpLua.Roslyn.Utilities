namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Murmur3-based hash combiner for implementing <see cref="object.GetHashCode" /> in equatable types.
/// </summary>
/// <remarks>
///     <para>
///         This struct provides a high-quality hash combining algorithm based on MurmurHash3, offering
///         excellent distribution and minimal collisions for use in hash tables and equality comparisons.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>This is a mutable struct for performance; use it within a single method scope only.</description>
///         </item>
///         <item>
///             <description>
///                 Do not store or pass instances across method boundaries as the state may be copied
///                 unexpectedly.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="Create" /> to initialize a new instance, then call <see cref="Add(int)" /> or
///                 overloads to combine values.
///             </description>
///         </item>
///         <item>
///             <description>Retrieve the final hash via <see cref="HashCode" /> or <see cref="ToHashCode" />.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EquatableArray{T}" />
/// <seealso cref="EqualityExtensions.GetSequenceHashCode{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    struct HashCombiner
{
    private uint _len;
    private uint _hash;

    /// <summary>
    ///     Gets the combined hash code computed from all added values.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The returned value incorporates finalization mixing to ensure good bit distribution,
    ///         making it suitable for use in hash tables and dictionaries.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ToHashCode" />
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

    /// <summary>
    ///     Creates a new <see cref="HashCombiner" /> instance with the specified seed value.
    /// </summary>
    /// <param name="seed">
    ///     The initial seed value for the hash computation. Defaults to the FNV-1a offset basis (2166136261).
    /// </param>
    /// <returns>A new <see cref="HashCombiner" /> initialized with the specified seed.</returns>
    /// <remarks>
    ///     <para>
    ///         Use different seed values when you need distinct hash sequences for the same input values,
    ///         such as when implementing multiple hash functions for bloom filters.
    ///     </para>
    /// </remarks>
    public static HashCombiner Create(int seed = unchecked((int)2166136261))
    {
        var result = new HashCombiner
        {
            _hash = unchecked((uint)seed)
        };
        return result;
    }

    /// <summary>
    ///     Adds an integer value to the hash computation.
    /// </summary>
    /// <param name="value">The integer value to incorporate into the hash.</param>
    /// <remarks>
    ///     <para>
    ///         This is the core hashing method that implements the MurmurHash3 mixing function.
    ///         All other <c>Add</c> overloads ultimately delegate to this method.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Add{T}(T)" />
    /// <seealso cref="AddRange{T}(IEnumerable{T})" />
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

    /// <summary>
    ///     Adds a value of any type to the hash computation using its <see cref="object.GetHashCode" /> result.
    /// </summary>
    /// <typeparam name="T">The type of the value to add.</typeparam>
    /// <param name="value">
    ///     The value to incorporate into the hash. If <c>null</c>, adds 0 to the hash.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method uses the value's <see cref="object.GetHashCode" /> method to obtain an integer
    ///         hash code, which is then incorporated using the MurmurHash3 algorithm.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Add(int)" />
    /// <seealso cref="AddRange{T}(IEnumerable{T})" />
    public void Add<T>(T? value)
    {
        Add(value?.GetHashCode() ?? 0);
    }

    /// <summary>
    ///     Adds a boolean value to the hash computation.
    /// </summary>
    /// <param name="value">The boolean value to incorporate into the hash.</param>
    /// <remarks>
    ///     <para>
    ///         Converts <c>true</c> to 1 and <c>false</c> to 0 for consistent hashing behavior.
    ///     </para>
    /// </remarks>
    public void Add(bool value)
    {
        Add(value ? 1 : 0);
    }

    /// <summary>
    ///     Adds a string value to the hash computation using the specified comparer.
    /// </summary>
    /// <param name="value">
    ///     The string value to incorporate into the hash. If <c>null</c>, adds 0 to the hash.
    /// </param>
    /// <param name="comparer">
    ///     The string comparer to use for computing the hash code. If <c>null</c>, uses <see cref="StringComparer.Ordinal" />.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Use a consistent comparer that matches your equality comparison semantics.
    ///         For example, use <see cref="StringComparer.OrdinalIgnoreCase" /> if your equality
    ///         comparison is case-insensitive.
    ///     </para>
    /// </remarks>
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

    /// <summary>
    ///     Adds all values from a sequence to the hash computation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="values">
    ///     The sequence of values to incorporate into the hash. If <c>null</c>, adds 0 to the hash.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Each element's hash code is added in order, so the hash will differ for sequences
    ///         with the same elements in different orders.
    ///     </para>
    /// </remarks>
    /// <seealso cref="AddRange{T}(ReadOnlySpan{T})" />
    /// <seealso cref="Add{T}(T)" />
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

    /// <summary>
    ///     Adds all values from a span to the hash computation.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="values">The span of values to incorporate into the hash.</param>
    /// <remarks>
    ///     <para>
    ///         This overload avoids allocation when iterating over contiguous memory regions.
    ///         Each element's hash code is added in order.
    ///     </para>
    /// </remarks>
    /// <seealso cref="AddRange{T}(IEnumerable{T})" />
    /// <seealso cref="Add{T}(T)" />
    public void AddRange<T>(ReadOnlySpan<T> values)
    {
        foreach (var value in values)
            Add(value);
    }

    /// <summary>
    ///     Returns the combined hash code computed from all added values.
    /// </summary>
    /// <returns>The finalized hash code as an integer.</returns>
    /// <remarks>
    ///     <para>
    ///         This method is equivalent to reading the <see cref="HashCode" /> property and is provided
    ///         for API compatibility with <see cref="System.HashCode.ToHashCode" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HashCode" />
    public readonly int ToHashCode() => HashCode;

    private static uint RotateLeft(uint value, int bits) => value << bits | value >> 32 - bits;
}

/// <summary>
///     Provides extension methods for equality comparisons and hash code generation on collections.
/// </summary>
/// <remarks>
///     <para>
///         This class contains helper methods for working with <see cref="EquatableArray{T}" /> and
///         <see cref="ImmutableArray" />, providing value-based equality semantics for collections.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Use <see cref="ToEquatableArray{T}(IEnumerable{T})" /> to wrap sequences for value equality.</description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="SequenceEquals{T}(ImmutableArray{T}, ImmutableArray{T})" /> for element-wise
///                 comparison.
///             </description>
///         </item>
///         <item>
///             <description>Use <see cref="GetSequenceHashCode{T}" /> to compute a hash over all elements.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EquatableArray{T}" />
/// <seealso cref="HashCombiner" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class EqualityExtensions
{
    /// <summary>
    ///     Converts an enumerable sequence to an <see cref="EquatableArray{T}" /> for value-based equality.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The source sequence to convert.</param>
    /// <returns>An <see cref="EquatableArray{T}" /> containing the elements from <paramref name="source" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This method materializes the sequence into an immutable array, then wraps it for value equality.
    ///         Use this when you need to cache or compare collections in incremental source generators.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ToEquatableArray{T}(ImmutableArray{T})" />
    /// <seealso cref="EquatableArray{T}" />
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) where T : IEquatable<T> => source.ToImmutableArray().AsEquatableArray();

    /// <summary>
    ///     Converts an immutable array to an <see cref="EquatableArray{T}" /> for value-based equality.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array. Must implement <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="source">The immutable array to wrap.</param>
    /// <returns>An <see cref="EquatableArray{T}" /> wrapping <paramref name="source" />.</returns>
    /// <remarks>
    ///     <para>
    ///         This is a zero-allocation wrapper that provides value equality semantics for the underlying
    ///         immutable array. The original array is preserved and can be retrieved via
    ///         <see cref="EquatableArray{T}.AsImmutableArray" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ToEquatableArray{T}(IEnumerable{T})" />
    /// <seealso cref="EquatableArray{T}" />
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> source) where T : IEquatable<T> => source.AsEquatableArray();

    /// <summary>
    ///     Compares two immutable arrays for sequence equality using the default equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the arrays.</typeparam>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns>
    ///     <c>true</c> if both arrays contain the same elements in the same order; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Default (uninitialized) and empty arrays are treated as equivalent. This follows the
    ///         null-equals-empty pattern commonly used in Roslyn source generators.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SequenceEquals{T}(ImmutableArray{T}, ImmutableArray{T}, IEqualityComparer{T})" />
    /// <seealso cref="GetSequenceHashCode{T}" />
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
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                return false;

        return true;
    }

    /// <summary>
    ///     Compares two immutable arrays for sequence equality using a custom equality comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the arrays.</typeparam>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <param name="comparer">The equality comparer to use for element comparisons.</param>
    /// <returns>
    ///     <c>true</c> if both arrays contain equal elements (as determined by <paramref name="comparer" />)
    ///     in the same order; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Default (uninitialized) and empty arrays are treated as equivalent. This follows the
    ///         null-equals-empty pattern commonly used in Roslyn source generators.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SequenceEquals{T}(ImmutableArray{T}, ImmutableArray{T})" />
    /// <seealso cref="GetSequenceHashCode{T}" />
    public static bool SequenceEquals<T>(this ImmutableArray<T> left, ImmutableArray<T> right,
        IEqualityComparer<T> comparer)
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
            if (!comparer.Equals(left[i], right[i]))
                return false;

        return true;
    }

    /// <summary>
    ///     Computes a hash code for the sequence of elements in an immutable array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array to compute a hash code for.</param>
    /// <returns>
    ///     A hash code computed from all elements in order, or 0 if the array is default or empty.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The hash code is order-dependent: arrays with the same elements in different orders
    ///         will produce different hash codes. Default and empty arrays both return 0, consistent
    ///         with their treatment as equivalent in <see cref="SequenceEquals{T}(ImmutableArray{T}, ImmutableArray{T})" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SequenceEquals{T}(ImmutableArray{T}, ImmutableArray{T})" />
    /// <seealso cref="HashCombiner" />
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
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extensions and factory methods for <see cref="EquatableArray{T}" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class EquatableArray
{
    /// <summary>
    ///     Creates an <see cref="EquatableArray{T}" /> from a given <see cref="ImmutableArray{T}" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T> =>
        new(array);

    /// <summary>
    ///     Creates an <see cref="EquatableArray{T}" /> from a given array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EquatableArray<T> ToEquatableArray<T>(this T[] array)
        where T : IEquatable<T> =>
        new(array);
}

/// <summary>
///     An immutable, equatable array with value equality support.
/// </summary>
/// <remarks>
///     <para>
///         Design principle: <c>null</c> backing array = empty. There is no distinction
///         between "uninitialized" and "empty" - both represent the same empty state.
///         This eliminates the "default vs empty" antipattern and ensures all operations
///         are safe without scattered defensive checks.
///     </para>
///     <para>
///         Key behaviors:
///         <list type="bullet">
///             <item><description><see cref="IsEmpty"/> returns true for both <c>default</c> and explicitly empty arrays</description></item>
///             <item><description><see cref="AsSpan"/> returns an empty span for null backing (never throws)</description></item>
///             <item><description><see cref="GetEnumerator"/> returns an empty enumerator for null backing (never throws)</description></item>
///             <item><description>Equality: two empty arrays are always equal regardless of how they were created</description></item>
///         </list>
///     </para>
/// </remarks>
/// <typeparam name="T">The type of values in the array.</typeparam>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyCollection<T>
    where T : IEquatable<T>
{
    /// <summary>
    ///     The underlying array. Null represents empty (single empty state).
    /// </summary>
    private readonly T[]? _array;

    /// <summary>
    ///     Creates a new <see cref="EquatableArray{T}" /> from an <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> to wrap.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        var backing = ImmutableCollectionsMarshal.AsArray(array);
        // Normalize: empty arrays become null (single empty state)
        _array = backing is { Length: > 0 } ? backing : null;
    }

    /// <summary>
    ///     Creates a new <see cref="EquatableArray{T}" /> from a raw array.
    /// </summary>
    /// <param name="array">The input array. Ownership is transferred - do not mutate after passing.</param>
    /// <remarks>
    ///     This constructor is internal to avoid ambiguity with collection expressions in C# 12+.
    ///     Use <see cref="EquatableArray.ToEquatableArray{T}"/> extension method instead:
    ///     <code>myArray.ToEquatableArray()</code>
    /// </remarks>
    internal EquatableArray(T[]? array)
    {
        // Normalize: empty arrays become null (single empty state)
        _array = array is { Length: > 0 } ? array : null;
    }

    /// <summary>
    ///     Gets a value indicating whether this array is empty.
    /// </summary>
    /// <remarks>
    ///     Returns <c>true</c> for both <c>default(EquatableArray)</c> and explicitly empty arrays.
    ///     There is no distinction between "uninitialized" and "empty".
    /// </remarks>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null;
    }

    /// <summary>
    ///     Gets a value indicating whether this array is default or empty.
    /// </summary>
    /// <remarks>
    ///     Provided for API compatibility with <see cref="ImmutableArray{T}.IsDefaultOrEmpty"/>.
    ///     Since this struct unifies default and empty states, this is equivalent to <see cref="IsEmpty"/>.
    /// </remarks>
    public bool IsDefaultOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null;
    }

    /// <summary>
    ///     Gets the number of elements in the array.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array?.Length ?? 0;
    }

    /// <summary>
    ///     Gets the number of elements in the array (IReadOnlyCollection implementation).
    /// </summary>
    int IReadOnlyCollection<T>.Count => Length;

    /// <summary>
    ///     Gets a reference to an item at a specified position within the array.
    /// </summary>
    /// <param name="index">The index of the item.</param>
    /// <returns>A reference to the item.</returns>
    /// <exception cref="InvalidOperationException">The array is empty.</exception>
    /// <exception cref="IndexOutOfRangeException">Index is out of range.</exception>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_array is null)
                throw new InvalidOperationException("Cannot index into an empty EquatableArray.");

            return ref _array[index];
        }
    }

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        // Both empty = equal (regardless of how they became empty)
        if (_array is null)
            return other._array is null;

        if (other._array is null)
            return false;

        return AsSpan().SequenceEqual(other.AsSpan());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_array is null)
            return 0;

        HashCode hashCode = default;
        foreach (var item in _array)
            hashCode.Add(item);

        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}" /> instance from the current <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <returns>The <see cref="ImmutableArray{T}" /> from the current <see cref="EquatableArray{T}" />.</returns>
    /// <remarks>
    ///     Returns <see cref="ImmutableArray{T}.Empty"/> for empty arrays. Never returns a default ImmutableArray.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> AsImmutableArray() =>
        _array is null ? ImmutableArray<T>.Empty : ImmutableCollectionsMarshal.AsImmutableArray(_array);

    /// <summary>
    ///     Gets the underlying items as an <see cref="ImmutableArray{T}" />.
    ///     Alias for <see cref="AsImmutableArray" /> for convenience.
    /// </summary>
    public ImmutableArray<T> Items
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AsImmutableArray();
    }

    /// <summary>
    ///     Returns a <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> wrapping the current items. Empty span if array is empty.</returns>
    /// <remarks>
    ///     This method is always safe - it returns an empty span for empty arrays, never throws.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsSpan() => new(_array);

    /// <summary>
    ///     Copies the contents of this <see cref="EquatableArray{T}" /> instance to a mutable array.
    /// </summary>
    /// <returns>The newly instantiated array, or an empty array if this is empty.</returns>
    public T[] ToArray() => _array is null ? [] : [.. _array];

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}.Enumerator" /> to traverse items in the current array.
    /// </summary>
    /// <returns>An enumerator to traverse items. Returns empty enumerator for empty arrays.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

    /// <summary>
    ///     Implicitly converts an <see cref="EquatableArray{T}" /> to <see cref="ImmutableArray{T}" />.
    /// </summary>
    public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

    /// <summary>
    ///     Checks equality between two <see cref="EquatableArray{T}" /> instances.
    /// </summary>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>
    ///     Checks inequality between two <see cref="EquatableArray{T}" /> instances.
    /// </summary>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

/// <summary>
///     Additional extension methods for <see cref="EquatableArray{T}" />.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
static class EquatableArrayExtensions
{
    /// <summary>
    ///     Appends a single item to the array.
    /// </summary>
    public static EquatableArray<T> Append<T>(this EquatableArray<T> array, T item)
        where T : IEquatable<T>
    {
        if (array.IsEmpty)
            return new EquatableArray<T>(new[] { item });

        var newArray = new T[array.Length + 1];
        array.AsSpan().CopyTo(newArray);
        newArray[array.Length] = item;
        return new EquatableArray<T>(newArray);
    }

    /// <summary>
    ///     Concatenates two arrays.
    /// </summary>
    public static EquatableArray<T> Concat<T>(this EquatableArray<T> first, EquatableArray<T> second)
        where T : IEquatable<T>
    {
        if (first.IsEmpty)
            return second;
        if (second.IsEmpty)
            return first;

        var newArray = new T[first.Length + second.Length];
        first.AsSpan().CopyTo(newArray);
        second.AsSpan().CopyTo(newArray.AsSpan(first.Length));
        return new EquatableArray<T>(newArray);
    }

    /// <summary>
    ///     Returns elements with their index.
    /// </summary>
    public static IEnumerable<(T Item, int Index)> Indexed<T>(this EquatableArray<T> array)
        where T : IEquatable<T>
    {
        for (var i = 0; i < array.Length; i++)
            yield return (array[i], i);
    }

    /// <summary>
    ///     Filters elements matching a predicate.
    /// </summary>
    public static EquatableArray<T> Where<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        if (array.IsEmpty)
            return array;

        var builder = ImmutableArray.CreateBuilder<T>();
        foreach (var item in array)
        {
            if (predicate(item))
                builder.Add(item);
        }

        return builder.Count == array.Length
            ? array  // No items filtered, return original
            : builder.ToImmutable().AsEquatableArray();
    }

    /// <summary>
    ///     Projects elements using a selector.
    /// </summary>
    public static EquatableArray<TResult> Select<T, TResult>(
        this EquatableArray<T> array,
        Func<T, TResult> selector)
        where T : IEquatable<T>
        where TResult : IEquatable<TResult>
    {
        if (array.IsEmpty)
            return default;

        var result = new TResult[array.Length];
        for (var i = 0; i < array.Length; i++)
            result[i] = selector(array[i]);

        return new EquatableArray<TResult>(result);
    }

    /// <summary>
    ///     Returns the first element, or default if empty.
    /// </summary>
    public static T? FirstOrDefault<T>(this EquatableArray<T> array)
        where T : IEquatable<T> =>
        array.IsEmpty ? default : array[0];

    /// <summary>
    ///     Returns the first element matching a predicate, or default.
    /// </summary>
    public static T? FirstOrDefault<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
        {
            if (predicate(item))
                return item;
        }

        return default;
    }

    /// <summary>
    ///     Checks if any element matches the predicate.
    /// </summary>
    public static bool Any<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
        {
            if (predicate(item))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if all elements match the predicate.
    /// </summary>
    public static bool All<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
        {
            if (!predicate(item))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks if the array contains the specified item.
    /// </summary>
    public static bool Contains<T>(this EquatableArray<T> array, T item)
        where T : IEquatable<T>
    {
        foreach (var element in array)
        {
            if (element.Equals(item))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Returns distinct elements.
    /// </summary>
    public static EquatableArray<T> Distinct<T>(this EquatableArray<T> array)
        where T : IEquatable<T>
    {
        if (array.IsEmpty)
            return array;

        var seen = new HashSet<T>();
        var builder = ImmutableArray.CreateBuilder<T>();

        foreach (var item in array)
        {
            if (seen.Add(item))
                builder.Add(item);
        }

        return builder.Count == array.Length
            ? array  // No duplicates, return original
            : builder.ToImmutable().AsEquatableArray();
    }
}

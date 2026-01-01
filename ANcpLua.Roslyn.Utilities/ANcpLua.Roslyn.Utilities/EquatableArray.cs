// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extensions and Factory methods for <see cref="EquatableArray{T}" />.
/// </summary>
public static class EquatableArray
{
    /// <summary>
    ///     Creates an <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray" />.
    /// </summary>
    /// <typeparam name="T">The type of items in the input array.</typeparam>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> instance.</param>
    /// <returns>An <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T> =>
        new(array);
}

/// <summary>
///     An immutable, equatable array. This is equivalent to <see cref="ImmutableArray{T}" /> but with value equality
///     support.
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    ///     The underlying <typeparamref name="T" /> array.
    /// </summary>
    private readonly T[]? _array;

    /// <summary>
    ///     Creates a new <see cref="EquatableArray{T}" /> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> to wrap.</param>
    public EquatableArray(ImmutableArray<T> array) => _array = ImmutableCollectionsMarshal.AsArray(array);

    /// <summary>
    ///     Gets a reference to an item at a specified position within the array.
    /// </summary>
    /// <param name="index">The index of the item.</param>
    /// <returns>A reference to the item.</returns>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_array is null)
                throw new InvalidOperationException("EquatableArray is uninitialized.");

            return ref _array[index];
        }
    }

    /// <summary>
    ///     Gets a value indicating whether this array is uninitialized (default).
    /// </summary>
    public bool IsDefault
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null;
    }

    /// <summary>
    ///     Gets a value indicating whether this array is uninitialized or empty.
    /// </summary>
    public bool IsDefaultOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null || _array.Length is 0;
    }

    /// <summary>
    ///     Gets the number of elements in the array.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array?.Length ?? 0;
    }

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_array is not { } array) return 0;

        HashCode hashCode = default;
        foreach (var item in array) hashCode.Add(item);

        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}" /> instance from the current <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <returns>The <see cref="ImmutableArray{T}" /> from the current <see cref="EquatableArray{T}" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> AsImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(_array);

    /// <summary>
    ///     Returns a <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> wrapping the current items.</returns>
    public ReadOnlySpan<T> AsSpan() => new(_array);

    /// <summary>
    ///     Copies the contents of this <see cref="EquatableArray{T}" /> instance to a mutable array.
    /// </summary>
    /// <returns>The newly instantiated array.</returns>
    public T[] ToArray() => AsImmutableArray().ToArray();

    /// <summary>
    ///     Gets an <see cref="Dictionary{TKey,TValue}.Enumerator" /> value to traverse items in the current array.
    /// </summary>
    /// <returns>An <see cref="Dictionary{TKey,TValue}.Enumerator" /> value to traverse items in the current array.</returns>
    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

    /// <summary>
    ///     Implicitly converts an <see cref="EquatableArray{T}" /> to <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <param name="array">The input <see cref="EquatableArray{T}" /> instance.</param>
    /// <returns>An <see cref="ImmutableArray{T}" /> instance from a given <see cref="EquatableArray{T}" />.</returns>
    public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

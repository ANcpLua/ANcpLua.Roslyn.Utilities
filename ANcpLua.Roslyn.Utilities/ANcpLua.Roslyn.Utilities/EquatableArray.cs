// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides factory methods and extension methods for creating <see cref="EquatableArray{T}" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This static class contains extension methods that enable fluent creation of
///         <see cref="EquatableArray{T}" /> from both <see cref="ImmutableArray{T}" /> and regular arrays.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Use <see cref="AsEquatableArray{T}" /> to wrap an existing <see cref="ImmutableArray{T}" />.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Use <see cref="ToEquatableArray{T}" /> to create from a regular array (ownership is transferred).
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EquatableArray{T}" />
/// <seealso cref="EquatableArrayExtensions" />
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
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> to wrap.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> that wraps the specified immutable array.
    /// </returns>
    /// <remarks>
    ///     This extension method provides a fluent way to convert immutable arrays to equatable arrays.
    ///     The underlying storage is shared with the original <see cref="ImmutableArray{T}" />.
    /// </remarks>
    /// <seealso cref="ToEquatableArray{T}" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(array);
    }

    /// <summary>
    ///     Creates an <see cref="EquatableArray{T}" /> from a given array.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">
    ///     The input array. Ownership is transferred to the <see cref="EquatableArray{T}" /> -
    ///     do not mutate the array after calling this method.
    /// </param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> that wraps the specified array.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method transfers ownership of the array. Mutating the original array after
    ///         calling this method will result in undefined behavior.
    ///     </para>
    ///     <para>
    ///         Use this method when you have a newly created array that you want to wrap.
    ///         For existing <see cref="ImmutableArray{T}" />, use <see cref="AsEquatableArray{T}" /> instead.
    ///     </para>
    /// </remarks>
    /// <seealso cref="AsEquatableArray{T}" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EquatableArray<T> ToEquatableArray<T>(this T[] array)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(array);
    }
}

/// <summary>
///     An immutable, equatable array with value equality support, designed for use in
///     Roslyn incremental source generators where proper caching requires value semantics.
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
///             <item>
///                 <description>
///                     <see cref="IsEmpty" /> returns <c>true</c> for both <c>default</c> and explicitly empty
///                     arrays
///                 </description>
///             </item>
///             <item>
///                 <description><see cref="AsSpan" /> returns an empty span for <c>null</c> backing (never throws)</description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="GetEnumerator" /> returns an empty enumerator for <c>null</c> backing (never
///                     throws)
///                 </description>
///             </item>
///             <item>
///                 <description>Equality: two empty arrays are always equal regardless of how they were created</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         This type is essential for incremental generator caching because it provides proper
///         value equality for array contents, unlike <see cref="ImmutableArray{T}" /> which uses
///         reference equality by default.
///     </para>
/// </remarks>
/// <typeparam name="T">
///     The type of elements in the array. Must implement <see cref="IEquatable{T}" /> to enable
///     proper value equality comparison of elements.
/// </typeparam>
/// <seealso cref="EquatableArray" />
/// <seealso cref="EquatableArrayExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyCollection<T>
    where T : IEquatable<T>
{
    /// <summary>
    ///     The underlying array. <c>null</c> represents empty (single empty state).
    /// </summary>
    private readonly T[]? _array;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EquatableArray{T}" /> struct from an <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <param name="array">
    ///     The input <see cref="ImmutableArray{T}" /> to wrap. Default or empty arrays are normalized to the empty state.
    /// </param>
    /// <remarks>
    ///     The underlying storage is shared with the original <see cref="ImmutableArray{T}" /> through
    ///     <see cref="ImmutableCollectionsMarshal.AsArray{T}" />.
    /// </remarks>
    public EquatableArray(ImmutableArray<T> array)
    {
        var backing = ImmutableCollectionsMarshal.AsArray(array);
        // Normalize: empty arrays become null (single empty state)
        _array = backing is { Length: > 0 } ? backing : null;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EquatableArray{T}" /> struct from a raw array.
    /// </summary>
    /// <param name="array">
    ///     The input array. Ownership is transferred - do not mutate after passing.
    ///     <c>null</c> or empty arrays are normalized to the empty state.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This constructor is internal to avoid ambiguity with collection expressions in C# 12+.
    ///         Use the <see cref="EquatableArray.ToEquatableArray{T}" /> extension method instead:
    ///     </para>
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
    /// <value>
    ///     <c>true</c> if this array contains no elements; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///     Returns <c>true</c> for both <c>default(EquatableArray&lt;T&gt;)</c> and explicitly empty arrays.
    ///     There is no distinction between "uninitialized" and "empty".
    /// </remarks>
    /// <seealso cref="IsDefaultOrEmpty" />
    /// <seealso cref="Length" />
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null;
    }

    /// <summary>
    ///     Gets a value indicating whether this array is default or empty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this array is default or contains no elements; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///     Provided for API compatibility with <see cref="ImmutableArray{T}.IsDefaultOrEmpty" />.
    ///     Since this struct unifies default and empty states, this property is equivalent to <see cref="IsEmpty" />.
    /// </remarks>
    /// <seealso cref="IsEmpty" />
    public bool IsDefaultOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array is null;
    }

    /// <summary>
    ///     Gets the number of elements in the array.
    /// </summary>
    /// <value>
    ///     The number of elements in the array, or 0 if the array is empty.
    /// </value>
    /// <seealso cref="IsEmpty" />
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array?.Length ?? 0;
    }

    /// <summary>
    ///     Gets the number of elements in the array.
    /// </summary>
    /// <value>
    ///     The number of elements in the array, or 0 if the array is empty.
    /// </value>
    /// <remarks>
    ///     This property implements <see cref="IReadOnlyCollection{T}.Count" /> and returns
    ///     the same value as <see cref="Length" />.
    /// </remarks>
    int IReadOnlyCollection<T>.Count => Length;

    /// <summary>
    ///     Gets a reference to an item at a specified position within the array.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>A read-only reference to the element at the specified position.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when attempting to index into an empty array.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is less than 0 or greater than or equal to <see cref="Length" />.
    /// </exception>
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

    /// <summary>
    ///     Determines whether this <see cref="EquatableArray{T}" /> is equal to another
    ///     <see cref="EquatableArray{T}" /> by comparing their elements for value equality.
    /// </summary>
    /// <param name="other">The other <see cref="EquatableArray{T}" /> to compare with.</param>
    /// <returns>
    ///     <c>true</c> if both arrays have the same length and all corresponding elements
    ///     are equal according to <typeparamref name="T" />'s <see cref="IEquatable{T}.Equals(T)" /> method;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Two empty arrays are always equal regardless of how they became empty
    ///     (default initialization, empty constructor, etc.).
    /// </remarks>
    public bool Equals(EquatableArray<T> other)
    {
        // Both empty = equal (regardless of how they became empty)
        if (_array is null)
            return other._array is null;

        if (other._array is null)
            return false;

        return AsSpan().SequenceEqual(other.AsSpan());
    }

    /// <summary>
    ///     Determines whether this <see cref="EquatableArray{T}" /> is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="obj" /> is an <see cref="EquatableArray{T}" /> and
    ///     is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    /// <summary>
    ///     Returns a hash code for this <see cref="EquatableArray{T}" /> based on its elements.
    /// </summary>
    /// <returns>
    ///     A hash code computed from all elements in the array, or 0 if the array is empty.
    /// </returns>
    /// <remarks>
    ///     The hash code is computed by combining the hash codes of all elements using
    ///     <see cref="HashCode" />. This ensures that arrays with equal elements
    ///     produce equal hash codes.
    /// </remarks>
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
    ///     Gets an <see cref="ImmutableArray{T}" /> instance from this <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <returns>
    ///     An <see cref="ImmutableArray{T}" /> containing the same elements.
    ///     Returns <see cref="ImmutableArray{T}.Empty" /> for empty arrays (never returns a default
    ///     <see cref="ImmutableArray{T}" />).
    /// </returns>
    /// <remarks>
    ///     The returned <see cref="ImmutableArray{T}" /> shares storage with this instance.
    ///     This method always returns a valid (non-default) <see cref="ImmutableArray{T}" />.
    /// </remarks>
    /// <seealso cref="Items" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> AsImmutableArray()
    {
        return _array is null ? ImmutableArray<T>.Empty : ImmutableCollectionsMarshal.AsImmutableArray(_array);
    }

    /// <summary>
    ///     Gets the underlying items as an <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <value>
    ///     An <see cref="ImmutableArray{T}" /> containing the same elements.
    ///     Returns <see cref="ImmutableArray{T}.Empty" /> for empty arrays.
    /// </value>
    /// <remarks>
    ///     This property is an alias for <see cref="AsImmutableArray" /> provided for convenience.
    /// </remarks>
    /// <seealso cref="AsImmutableArray" />
    public ImmutableArray<T> Items
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AsImmutableArray();
    }

    /// <summary>
    ///     Returns a <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    /// </summary>
    /// <returns>
    ///     A <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    ///     Returns an empty span if the array is empty.
    /// </returns>
    /// <remarks>
    ///     This method is always safe - it returns an empty span for empty arrays and never throws.
    ///     The span provides efficient, bounds-checked access to the underlying elements.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsSpan()
    {
        return new ReadOnlySpan<T>(_array);
    }

    /// <summary>
    ///     Copies the contents of this <see cref="EquatableArray{T}" /> to a new mutable array.
    /// </summary>
    /// <returns>
    ///     A new array containing copies of all elements, or an empty array if this instance is empty.
    /// </returns>
    /// <remarks>
    ///     The returned array is a copy and can be safely mutated without affecting this instance.
    /// </remarks>
    public T[] ToArray()
    {
        return _array is null ? [] : [.. _array];
    }

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}.Enumerator" /> to traverse items in the current array.
    /// </summary>
    /// <returns>
    ///     An enumerator to traverse items. Returns an empty enumerator for empty arrays.
    /// </returns>
    /// <remarks>
    ///     This method is safe to call on empty arrays and never throws.
    ///     The enumerator provides efficient, struct-based iteration.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T>.Enumerator GetEnumerator()
    {
        return AsImmutableArray().GetEnumerator();
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)AsImmutableArray()).GetEnumerator();
    }

    /// <summary>
    ///     Implicitly converts an <see cref="EquatableArray{T}" /> to an <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <param name="array">The <see cref="EquatableArray{T}" /> to convert.</param>
    /// <returns>An <see cref="ImmutableArray{T}" /> containing the same elements.</returns>
    public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
    {
        return array.AsImmutableArray();
    }

    /// <summary>
    ///     Determines whether two <see cref="EquatableArray{T}" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> to compare.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> to compare.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="left" /> and <paramref name="right" /> have equal elements;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Equals(EquatableArray{T})" />
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two <see cref="EquatableArray{T}" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> to compare.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> to compare.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="left" /> and <paramref name="right" /> do not have equal elements;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Equals(EquatableArray{T})" />
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     Provides extension methods for <see cref="EquatableArray{T}" /> that enable
///     LINQ-like operations while preserving the equatable semantics.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods provide functional-style operations on <see cref="EquatableArray{T}" />
///         instances without requiring conversion to other collection types.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Transformation: <see cref="Select{T, TResult}" />, <see cref="Where{T}" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Combination: <see cref="Append{T}" />, <see cref="Concat{T}" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Query: <see cref="Any{T}" />, <see cref="All{T}" />, <see cref="Contains{T}" />,
///                 <see cref="FirstOrDefault{T}(EquatableArray{T})" />
///             </description>
///         </item>
///         <item>
///             <description>
///                 Deduplication: <see cref="Distinct{T}" />
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EquatableArray{T}" />
/// <seealso cref="EquatableArray" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class EquatableArrayExtensions
{
    /// <summary>
    ///     Appends a single item to the end of the array.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="item">The item to append.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> containing all elements from <paramref name="array" />
    ///     followed by <paramref name="item" />.
    /// </returns>
    /// <seealso cref="Concat{T}" />
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
    ///     Concatenates two arrays into a single array.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the arrays. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="first">The first array.</param>
    /// <param name="second">The second array to concatenate.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> containing all elements from <paramref name="first" />
    ///     followed by all elements from <paramref name="second" />.
    ///     Returns <paramref name="second" /> if <paramref name="first" /> is empty.
    ///     Returns <paramref name="first" /> if <paramref name="second" /> is empty.
    /// </returns>
    /// <seealso cref="Append{T}" />
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
    ///     Returns elements paired with their zero-based index.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <returns>
    ///     An enumerable of tuples where each tuple contains an element and its index.
    /// </returns>
    /// <remarks>
    ///     This method uses deferred execution and yields elements one at a time.
    /// </remarks>
    public static IEnumerable<(T Item, int Index)> Indexed<T>(this EquatableArray<T> array)
        where T : IEquatable<T>
    {
        for (var i = 0; i < array.Length; i++)
            yield return (array[i], i);
    }

    /// <summary>
    ///     Filters elements based on a predicate.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> containing only elements that satisfy the predicate.
    ///     Returns the original array if all elements match (no allocation).
    ///     Returns an empty array if the source is empty.
    /// </returns>
    /// <seealso cref="Select{T, TResult}" />
    public static EquatableArray<T> Where<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        if (array.IsEmpty)
            return array;

        var builder = ImmutableArray.CreateBuilder<T>();
        foreach (var item in array)
            if (predicate(item))
                builder.Add(item);

        return builder.Count == array.Length
            ? array // No items filtered, return original
            : builder.ToImmutable().AsEquatableArray();
    }

    /// <summary>
    ///     Projects each element into a new form.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the source array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     The type of elements in the result array. Must implement <see cref="IEquatable{TResult}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{TResult}" /> containing the transformed elements.
    ///     Returns an empty array if the source is empty.
    /// </returns>
    /// <seealso cref="Where{T}" />
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
    ///     Returns the first element of the array, or a default value if the array is empty.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <returns>
    ///     The first element if the array is not empty; otherwise, <c>default(T)</c>.
    /// </returns>
    /// <seealso cref="FirstOrDefault{T}(EquatableArray{T}, Func{T, bool})" />
    public static T? FirstOrDefault<T>(this EquatableArray<T> array)
        where T : IEquatable<T>
    {
        return array.IsEmpty ? default : array[0];
    }

    /// <summary>
    ///     Returns the first element that matches the specified predicate, or a default value
    ///     if no such element is found.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     The first element that satisfies <paramref name="predicate" />; otherwise, <c>default(T)</c>.
    /// </returns>
    /// <seealso cref="FirstOrDefault{T}(EquatableArray{T})" />
    /// <seealso cref="Any{T}" />
    public static T? FirstOrDefault<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
            if (predicate(item))
                return item;

        return default;
    }

    /// <summary>
    ///     Determines whether any element matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     <c>true</c> if any element satisfies <paramref name="predicate" />; otherwise, <c>false</c>.
    ///     Returns <c>false</c> if the array is empty.
    /// </returns>
    /// <seealso cref="All{T}" />
    /// <seealso cref="Contains{T}" />
    public static bool Any<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
            if (predicate(item))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether all elements match the specified predicate.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///     <c>true</c> if every element satisfies <paramref name="predicate" />, or if the array is empty;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="Any{T}" />
    public static bool All<T>(this EquatableArray<T> array, Func<T, bool> predicate)
        where T : IEquatable<T>
    {
        foreach (var item in array)
            if (!predicate(item))
                return false;

        return true;
    }

    /// <summary>
    ///     Determines whether the array contains the specified item.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="item">The item to search for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="item" /> is found in the array; otherwise, <c>false</c>.
    ///     Returns <c>false</c> if the array is empty.
    /// </returns>
    /// <remarks>
    ///     Uses <see cref="IEquatable{T}.Equals(T)" /> for comparison.
    /// </remarks>
    /// <seealso cref="Any{T}" />
    public static bool Contains<T>(this EquatableArray<T> array, T item)
        where T : IEquatable<T>
    {
        foreach (var element in array)
            if (element.Equals(item))
                return true;

        return false;
    }

    /// <summary>
    ///     Returns distinct elements from the array, removing duplicates.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of elements in the array. Must implement <see cref="IEquatable{T}" />.
    /// </typeparam>
    /// <param name="array">The source array.</param>
    /// <returns>
    ///     A new <see cref="EquatableArray{T}" /> containing only distinct elements, preserving
    ///     the order of first occurrence. Returns the original array if no duplicates exist (no allocation).
    ///     Returns an empty array if the source is empty.
    /// </returns>
    /// <remarks>
    ///     Uses a <see cref="HashSet{T}" /> internally to track seen elements.
    /// </remarks>
    public static EquatableArray<T> Distinct<T>(this EquatableArray<T> array)
        where T : IEquatable<T>
    {
        if (array.IsEmpty)
            return array;

        var seen = new HashSet<T>();
        var builder = ImmutableArray.CreateBuilder<T>();

        foreach (var item in array)
            if (seen.Add(item))
                builder.Add(item);

        return builder.Count == array.Length
            ? array // No duplicates, return original
            : builder.ToImmutable().AsEquatableArray();
    }
}
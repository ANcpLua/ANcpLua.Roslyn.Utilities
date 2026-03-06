// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ANcpLua.Roslyn.Utilities;

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
            return new[] { item }.ToEquatableArray();

        var newArray = new T[array.Length + 1];
        array.AsSpan().CopyTo(newArray);
        newArray[array.Length] = item;
        return newArray.ToEquatableArray();
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
        return newArray.ToEquatableArray();
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

        return result.ToEquatableArray();
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

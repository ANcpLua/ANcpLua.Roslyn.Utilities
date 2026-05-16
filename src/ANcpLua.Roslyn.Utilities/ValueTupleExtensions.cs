namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for value tuples providing null-checking and enumeration capabilities.
/// </summary>
/// <remarks>
///     <para>
///         These extension methods simplify common patterns when working with tuples that may contain
///         nullable elements, and provide convenient enumeration of tuple elements.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Null-checking methods work with tuples of 2 or 3 elements</description>
///         </item>
///         <item>
///             <description>Enumeration methods support tuples from 2 to 5 elements of the same type</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="EnumerableExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ValueTupleExtensions
{
    /// <summary>
    ///     Determines whether any element in the 2-tuple is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if at least one element of the <paramref name="tuple" /> is not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AllNull{T1,T2}" />
    /// <seealso cref="AllNotNull{T1,T2}" />
    public static bool AnyNotNull<T1, T2>(this (T1?, T2?) tuple)
    {
        return tuple.Item1 is not null || tuple.Item2 is not null;
    }

    /// <summary>
    ///     Determines whether any element in the 3-tuple is not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if at least one element of the <paramref name="tuple" /> is not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AllNull{T1,T2,T3}" />
    /// <seealso cref="AllNotNull{T1,T2,T3}" />
    public static bool AnyNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
    {
        return tuple.Item1 is not null || tuple.Item2 is not null || tuple.Item3 is not null;
    }

    /// <summary>
    ///     Determines whether all elements in the 2-tuple are <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2}" />
    /// <seealso cref="AllNotNull{T1,T2}" />
    public static bool AllNull<T1, T2>(this (T1?, T2?) tuple)
    {
        return tuple.Item1 is null && tuple.Item2 is null;
    }

    /// <summary>
    ///     Determines whether all elements in the 3-tuple are <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2,T3}" />
    /// <seealso cref="AllNotNull{T1,T2,T3}" />
    public static bool AllNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
    {
        return tuple.Item1 is null && tuple.Item2 is null && tuple.Item3 is null;
    }

    /// <summary>
    ///     Determines whether all elements in the 2-tuple are not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2}" />
    /// <seealso cref="AllNull{T1,T2}" />
    public static bool AllNotNull<T1, T2>(this (T1?, T2?) tuple)
    {
        return tuple.Item1 is not null && tuple.Item2 is not null;
    }

    /// <summary>
    ///     Determines whether all elements in the 3-tuple are not <c>null</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first element.</typeparam>
    /// <typeparam name="T2">The type of the second element.</typeparam>
    /// <typeparam name="T3">The type of the third element.</typeparam>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>
    ///     <c>true</c> if all elements of the <paramref name="tuple" /> are not <c>null</c>;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <seealso cref="AnyNotNull{T1,T2,T3}" />
    /// <seealso cref="AllNull{T1,T2,T3}" />
    public static bool AllNotNull<T1, T2, T3>(this (T1?, T2?, T3?) tuple)
    {
        return tuple.Item1 is not null && tuple.Item2 is not null && tuple.Item3 is not null;
    }

    /// <summary>
    ///     Enumerates the elements of a 2-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <example>
    ///     <code>
    ///     var pair = ("first", "second");
    ///     foreach (var item in pair.Enumerate())
    ///         Console.WriteLine(item);
    ///     </code>
    /// </example>
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T})" />
    public static IEnumerable<T> Enumerate<T>(this (T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
    }

    /// <summary>
    ///     Enumerates the elements of a 3-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T})" />
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T})" />
    public static IEnumerable<T> Enumerate<T>(this (T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
    }

    /// <summary>
    ///     Enumerates the elements of a 4-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T})" />
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T, T})" />
    public static IEnumerable<T> Enumerate<T>(this (T, T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
        yield return tuple.Item4;
    }

    /// <summary>
    ///     Enumerates the elements of a 5-tuple as an <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tuple.</typeparam>
    /// <param name="tuple">The tuple to enumerate.</param>
    /// <returns>An enumerable containing the tuple elements in order.</returns>
    /// <seealso cref="Enumerate{T}(ValueTuple{T, T, T, T})" />
    public static IEnumerable<T> Enumerate<T>(this (T, T, T, T, T) tuple)
    {
        yield return tuple.Item1;
        yield return tuple.Item2;
        yield return tuple.Item3;
        yield return tuple.Item4;
        yield return tuple.Item5;
    }
}

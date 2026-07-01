namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Computes set differences between two collections — the building block for incremental
///     "what changed between the previous and current snapshot" logic in source generators,
///     analyzers, and caches.
/// </summary>
/// <remarks>
///     <para>
///         Elements are compared with <see cref="EqualityComparer{T}.Default" /> (i.e. the
///         <see cref="IEquatable{T}" /> implementation), so the intended element type is a value-equatable
///         model — records and <see cref="EquatableArray{T}" />-friendly types. Results are returned as
///         <see cref="EquatableArray{T}" /> so a delta can flow through an incremental generator pipeline
///         and participate in cache comparisons.
///     </para>
///     <para>
///         <see cref="Difference{T}(ImmutableArray{T}, ImmutableArray{T})" /> is one-directional. For a full
///         added/removed diff, use <see cref="Compute{T}(ImmutableArray{T}, ImmutableArray{T})" />, which
///         calls <c>Difference</c> in both directions and returns a <see cref="SetDelta{T}" />.
///     </para>
/// </remarks>
/// <seealso cref="SetDelta{T}" />
/// <seealso cref="EquatableArray{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Delta
{
    /// <summary>
    ///     Returns the elements of <paramref name="second" /> that are <b>not</b> present in
    ///     <paramref name="first" /> (the relative complement <c>second \ first</c>).
    /// </summary>
    /// <typeparam name="T">The value-equatable element type.</typeparam>
    /// <param name="first">The baseline collection. Items present here are excluded from the result.</param>
    /// <param name="second">The collection whose new-relative-to-<paramref name="first" /> items are returned.</param>
    /// <returns>
    ///     The items in <paramref name="second" /> absent from <paramref name="first" />, in the order they
    ///     appear in <paramref name="second" />.
    /// </returns>
    /// <remarks>
    ///     The operation <b>filters</b> <paramref name="second" />; it does not deduplicate. A value that
    ///     appears twice in <paramref name="second" /> and is absent from <paramref name="first" /> appears
    ///     twice in the result. Pass distinct collections when true set semantics are required. The operation
    ///     is not symmetric: <c>Difference(a, b)</c> is generally not <c>Difference(b, a)</c>.
    /// </remarks>
    public static EquatableArray<T> Difference<T>(EquatableArray<T> first, EquatableArray<T> second)
        where T : IEquatable<T>
        => Difference(first.AsImmutableArray(), second.AsImmutableArray());

    /// <inheritdoc cref="Difference{T}(EquatableArray{T}, EquatableArray{T})" />
    public static EquatableArray<T> Difference<T>(ImmutableArray<T> first, ImmutableArray<T> second)
        where T : IEquatable<T>
    {
        // Nothing in second -> nothing can be new.
        if (second.IsDefaultOrEmpty)
        {
            return default;
        }

        // Nothing in first -> everything in second is new; hand back the existing storage.
        if (first.IsDefaultOrEmpty)
        {
            return second.AsEquatableArray();
        }

        var set = new HashSet<T>(first);
        var builder = ImmutableArray.CreateBuilder<T>(second.Length);

        foreach (var item in second)
        {
            if (!set.Contains(item))
            {
                builder.Add(item);
            }
        }

        // Fast path: no item was filtered out — reuse second's storage instead of copying.
        return builder.Count == second.Length
            ? second.AsEquatableArray()
            : builder.ToImmutable().AsEquatableArray();
    }

    /// <summary>
    ///     Computes the two-directional set delta between a <paramref name="previous" /> and a
    ///     <paramref name="current" /> snapshot.
    /// </summary>
    /// <typeparam name="T">The value-equatable element type.</typeparam>
    /// <param name="previous">The earlier snapshot.</param>
    /// <param name="current">The later snapshot.</param>
    /// <returns>
    ///     A <see cref="SetDelta{T}" /> whose <see cref="SetDelta{T}.Added" /> holds items in
    ///     <paramref name="current" /> absent from <paramref name="previous" />, and whose
    ///     <see cref="SetDelta{T}.Removed" /> holds items in <paramref name="previous" /> absent from
    ///     <paramref name="current" />.
    /// </returns>
    public static SetDelta<T> Compute<T>(EquatableArray<T> previous, EquatableArray<T> current)
        where T : IEquatable<T>
        => Compute(previous.AsImmutableArray(), current.AsImmutableArray());

    /// <inheritdoc cref="Compute{T}(EquatableArray{T}, EquatableArray{T})" />
    public static SetDelta<T> Compute<T>(ImmutableArray<T> previous, ImmutableArray<T> current)
        where T : IEquatable<T>
        => new(Difference(previous, current), Difference(current, previous));
}

/// <summary>
///     The result of a two-directional set diff: the items that were added and the items that were removed
///     between two snapshots. Value-equatable, so an unchanged delta compares equal across incremental runs.
/// </summary>
/// <typeparam name="T">The value-equatable element type.</typeparam>
/// <seealso cref="Delta" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct SetDelta<T> : IEquatable<SetDelta<T>>
    where T : IEquatable<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SetDelta{T}" /> struct.
    /// </summary>
    /// <param name="added">Items present in the later snapshot but not the earlier one.</param>
    /// <param name="removed">Items present in the earlier snapshot but not the later one.</param>
    public SetDelta(EquatableArray<T> added, EquatableArray<T> removed)
    {
        Added = added;
        Removed = removed;
    }

    /// <summary>Gets the items that were added (present in <c>current</c>, absent from <c>previous</c>).</summary>
    public EquatableArray<T> Added { get; }

    /// <summary>Gets the items that were removed (present in <c>previous</c>, absent from <c>current</c>).</summary>
    public EquatableArray<T> Removed { get; }

    /// <summary>Gets a value indicating whether nothing was added or removed.</summary>
    public bool IsEmpty => Added.IsDefaultOrEmpty && Removed.IsDefaultOrEmpty;

    /// <summary>Gets a value indicating whether anything was added or removed.</summary>
    public bool HasChanges => !IsEmpty;

    /// <summary>Indicates whether the current delta equals another delta.</summary>
    /// <param name="other">The delta to compare against.</param>
    /// <returns><c>true</c> if both the added and removed sets are equal; otherwise <c>false</c>.</returns>
    public bool Equals(SetDelta<T> other)
        => Added.Equals(other.Added) && Removed.Equals(other.Removed);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is SetDelta<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = HashCombiner.Create();
        hash.Add(Added);
        hash.Add(Removed);
        return hash.ToHashCode();
    }

    /// <summary>Determines whether two deltas are equal.</summary>
    /// <param name="left">The first delta.</param>
    /// <param name="right">The second delta.</param>
    /// <returns><c>true</c> if the deltas are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(SetDelta<T> left, SetDelta<T> right) => left.Equals(right);

    /// <summary>Determines whether two deltas are not equal.</summary>
    /// <param name="left">The first delta.</param>
    /// <param name="right">The second delta.</param>
    /// <returns><c>true</c> if the deltas are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(SetDelta<T> left, SetDelta<T> right) => !left.Equals(right);
}

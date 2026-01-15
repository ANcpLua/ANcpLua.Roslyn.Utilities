namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Equatable wrapper for diagnostic message arguments, enabling value-based equality
///     comparisons for use in Roslyn incremental source generator caching.
/// </summary>
/// <remarks>
///     <para>
///         This type wraps an <see cref="ImmutableArray{T}" /> of message arguments and provides
///         proper value-based equality semantics required for incremental generator caching.
///         Without proper equality, generators may produce redundant outputs or miss necessary updates.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Treats default (uninitialized) and empty arrays as equivalent, following the "null = empty"
///                 pattern.
///             </description>
///         </item>
///         <item>
///             <description>Compares array elements using <see cref="object.Equals(object, object)" /> for deep equality.</description>
///         </item>
///         <item>
///             <description>Produces consistent hash codes where empty and default arrays both return 0.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="Args">The immutable array of message arguments to wrap.</param>
/// <seealso cref="DiagnosticInfo" />
/// <seealso cref="EquatableArray{T}" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct EquatableMessageArgs(ImmutableArray<object?> Args)
{
    /// <summary>
    ///     An empty args array instance for reuse.
    /// </summary>
    /// <remarks>
    ///     Use this static instance instead of creating new empty instances
    ///     to improve memory efficiency and ensure consistent behavior.
    /// </remarks>
    public static readonly EquatableMessageArgs Empty = new(ImmutableArray<object?>.Empty);

    /// <summary>
    ///     Gets a value indicating whether this instance has no arguments.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the <see cref="Args" /> array is default (uninitialized) or empty;
    ///     otherwise, <c>false</c>.
    /// </value>
    public bool IsEmpty => Args.IsDefaultOrEmpty;

    /// <summary>
    ///     Determines whether the specified <see cref="EquatableMessageArgs" /> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="EquatableMessageArgs" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if both instances are empty, or if they contain the same number of elements
    ///     and all corresponding elements are equal; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method treats default (uninitialized) and empty arrays as equivalent,
    ///     ensuring consistent behavior regardless of how empty instances are created.
    /// </remarks>
    public bool Equals(EquatableMessageArgs other)
    {
        // Treat default and empty as equivalent (null = empty pattern)
        var thisEmpty = Args.IsDefaultOrEmpty;
        var otherEmpty = other.Args.IsDefaultOrEmpty;

        if (thisEmpty && otherEmpty)
            return true;
        if (thisEmpty || otherEmpty)
            return false;

        if (Args.Length != other.Args.Length)
            return false;

        for (var i = 0; i < Args.Length; i++)
            if (!Equals(Args[i], other.Args[i]))
                return false;

        return true;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code computed from all elements in the <see cref="Args" /> array,
    ///     or 0 if the array is empty or default.
    /// </returns>
    /// <remarks>
    ///     Empty and default arrays both return 0 to ensure they are treated as equivalent
    ///     in hash-based collections, consistent with the <see cref="Equals(EquatableMessageArgs)" /> behavior.
    /// </remarks>
    public override int GetHashCode()
    {
        // Empty and default both return 0 (they are equivalent)
        if (Args.IsDefaultOrEmpty)
            return 0;

        unchecked
        {
            var hash = 17;
            foreach (var arg in Args)
                hash = hash * 31 + (arg?.GetHashCode() ?? 0);

            return hash;
        }
    }
}
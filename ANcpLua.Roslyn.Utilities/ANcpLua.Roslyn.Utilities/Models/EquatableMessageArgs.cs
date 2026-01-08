using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Equatable wrapper for diagnostic message arguments.
/// </summary>
/// <remarks>
///     Treats default (uninitialized) and empty arrays as equivalent.
///     This follows the "null = empty" pattern for consistency.
/// </remarks>
public readonly record struct EquatableMessageArgs(ImmutableArray<object?> Args)
{
    /// <summary>An empty args array.</summary>
    public static readonly EquatableMessageArgs Empty = new(ImmutableArray<object?>.Empty);

    /// <summary>
    ///     Gets a value indicating whether this instance has no arguments.
    /// </summary>
    public bool IsEmpty => Args.IsDefaultOrEmpty;

    /// <inheritdoc />
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
        {
            if (!Equals(Args[i], other.Args[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Empty and default both return 0 (they are equivalent)
        if (Args.IsDefaultOrEmpty)
            return 0;

        unchecked
        {
            var hash = 17;
            foreach (var arg in Args)
                hash = (hash * 31) + (arg?.GetHashCode() ?? 0);

            return hash;
        }
    }
}

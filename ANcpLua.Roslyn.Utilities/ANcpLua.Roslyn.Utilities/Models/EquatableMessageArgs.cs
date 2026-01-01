using System.Collections.Immutable;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     Equatable wrapper for diagnostic message arguments.
/// </summary>
public readonly record struct EquatableMessageArgs(ImmutableArray<object?> Args)
{
    /// <summary>An empty args array.</summary>
    public static readonly EquatableMessageArgs Empty = new(ImmutableArray<object?>.Empty);

    /// <inheritdoc />
    public bool Equals(EquatableMessageArgs other)
    {
        if (Args.IsDefault && other.Args.IsDefault) return true;

        if (Args.IsDefault || other.Args.IsDefault) return false;

        if (Args.Length != other.Args.Length) return false;

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
        if (Args.IsDefault) return 0;

        unchecked
        {
            var hash = 17;
            foreach (var arg in Args) hash = (hash * 31) + (arg?.GetHashCode() ?? 0);

            return hash;
        }
    }
}

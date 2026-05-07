namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides cached empty <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> instances.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ReadOnlyCollection
{
    /// <summary>
    ///     Gets an empty read-only collection for <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>A cached empty read-only collection.</returns>
    public static global::System.Collections.ObjectModel.ReadOnlyCollection<T> Empty<T>()
    {
        return EmptyCache<T>.Value;
    }

    private static class EmptyCache<T>
    {
        public static readonly global::System.Collections.ObjectModel.ReadOnlyCollection<T> Value =
            new global::System.Collections.ObjectModel.ReadOnlyCollection<T>(global::System.Array.Empty<T>());
    }
}

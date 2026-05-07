using System.Collections.Generic;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Provides cached empty <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey,TValue}" /> instances.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ReadOnlyDictionary
{
    /// <summary>
    ///     Gets an empty read-only dictionary for the specified key and value types.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A cached empty read-only dictionary.</returns>
    public static global::System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>()
        where TKey : notnull
    {
        return EmptyCache<TKey, TValue>.Value;
    }

    private static class EmptyCache<TKey, TValue>
        where TKey : notnull
    {
        public static readonly global::System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue> Value =
            new global::System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(
                new Dictionary<TKey, TValue>());
    }
}

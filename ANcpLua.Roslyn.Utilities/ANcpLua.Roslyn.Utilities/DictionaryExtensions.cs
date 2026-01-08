namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="Dictionary{TKey, TValue}" /> with allocation-free patterns.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    ///     Gets the value for the specified key, or inserts a new value using the factory if the key doesn't exist.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the factory.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <param name="context">Context data passed to the factory function.</param>
    /// <param name="factory">Factory function that creates a new value from the context.</param>
    /// <returns>The existing value if found, otherwise the newly created and inserted value.</returns>
    /// <example>
    ///     <code>
    /// // Without context (causes closure allocation):
    /// var value = dict.GetValueOrDefault(key) ?? (dict[key] = CreateValue(expensiveData));
    ///
    /// // With context (allocation-free):
    /// var value = dict.GetOrInsert(key, expensiveData, static ctx => CreateValue(ctx));
    /// </code>
    /// </example>
    public static TValue GetOrInsert<TKey, TValue, TContext>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TContext context,
        Func<TContext, TValue> factory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = factory(context);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    ///     Gets the value for the specified key, or inserts a new value using the factory if the key doesn't exist.
    ///     Overload that includes the key in the factory context.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the factory.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <param name="context">Context data passed to the factory function.</param>
    /// <param name="factory">Factory function that creates a new value from the key and context.</param>
    /// <returns>The existing value if found, otherwise the newly created and inserted value.</returns>
    public static TValue GetOrInsert<TKey, TValue, TContext>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TContext context,
        Func<TKey, TContext, TValue> factory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = factory(key, context);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    ///     Gets the value for the specified key, or inserts a default value if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <returns>The existing value if found, otherwise the default value.</returns>
    public static TValue? GetOrInsertDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = default;
        dictionary.Add(key, value!);
        return value;
    }
}

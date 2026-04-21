namespace ANcpLua.Roslyn.Utilities;

/// <summary>Get-or-insert dictionary helpers that thread a context parameter to the factory so callers can pass static lambdas and avoid closure allocations.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DictionaryExtensions
{
    /// <summary>
    ///     Gets the value for the specified key, or inserts a new value using the factory if the key doesn't exist.
    ///     Uses a context parameter to avoid closure allocations in hot paths.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the factory function.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <param name="context">Context data passed to the <paramref name="factory" /> function, avoiding closure allocations.</param>
    /// <param name="factory">
    ///     A factory function that creates a new value from the <paramref name="context" />.
    ///     Only invoked if <paramref name="key" /> is not found in the dictionary.
    ///     Should be a <c>static</c> lambda to ensure no closures are captured.
    /// </param>
    /// <returns>
    ///     The existing value if <paramref name="key" /> is found in the dictionary;
    ///     otherwise, the newly created value returned by <paramref name="factory" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is designed for use in performance-critical paths where avoiding allocations is important.
    ///         By passing context data explicitly rather than capturing it in a closure, this method ensures
    ///         zero heap allocations when the factory is a static lambda.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Without context (causes closure allocation):
    /// var value = dict.GetValueOrDefault(key) ?? (dict[key] = CreateValue(expensiveData));
    /// 
    /// // With context (allocation-free):
    /// var value = dict.GetOrInsert(key, expensiveData, static ctx => CreateValue(ctx));
    /// </code>
    /// </example>
    /// <seealso
    ///     cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TKey, TContext, TValue})" />
    /// <seealso cref="GetOrInsertDefault{TKey, TValue}" />
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
    ///     This overload includes the key in the factory parameters for cases where the value depends on the key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TContext">The type of context passed to the factory function.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <param name="context">Context data passed to the <paramref name="factory" /> function, avoiding closure allocations.</param>
    /// <param name="factory">
    ///     A factory function that creates a new value from both the <paramref name="key" /> and <paramref name="context" />.
    ///     Only invoked if <paramref name="key" /> is not found in the dictionary.
    ///     Should be a <c>static</c> lambda to ensure no closures are captured.
    /// </param>
    /// <returns>
    ///     The existing value if <paramref name="key" /> is found in the dictionary;
    ///     otherwise, the newly created value returned by <paramref name="factory" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this overload when the factory needs access to the key to create the value.
    ///         This avoids the need to capture the key in a closure.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TContext, TValue})" />
    /// <seealso cref="GetOrInsertDefault{TKey, TValue}" />
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
    ///     Gets the value for the specified key, or inserts <c>default</c> if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. Must be a value type.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <returns>
    ///     The existing value if <paramref name="key" /> is found in the dictionary;
    ///     otherwise, <c>default</c>(<typeparamref name="TValue" />), which is also inserted into the dictionary.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For value types, <c>default</c> is always a valid non-null value
    ///         (e.g., <c>0</c> for numeric types, <c>false</c> for <see cref="bool" />).
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetOrInsertNull{TKey, TValue}" />
    /// <seealso cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TContext, TValue})" />
    public static TValue GetOrInsertDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key)
        where TKey : notnull
        where TValue : struct
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        dictionary[key] = default;
        return default;
    }

    /// <summary>
    ///     Gets the value for the specified key, or inserts <c>null</c> if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. Must be a reference type.</typeparam>
    /// <param name="dictionary">The dictionary to operate on. Must be typed to allow null values.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <returns>
    ///     The existing value if <paramref name="key" /> is found in the dictionary;
    ///     otherwise, <c>null</c>, which is also inserted into the dictionary.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method requires the dictionary to be declared as <c>Dictionary&lt;TKey, TValue?&gt;</c>
    ///         to explicitly indicate that null values are allowed.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetOrInsertDefault{TKey, TValue}(Dictionary{TKey, TValue}, TKey)" />
    /// <seealso cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TContext, TValue})" />
    public static TValue? GetOrInsertNull<TKey, TValue>(
        this Dictionary<TKey, TValue?> dictionary,
        TKey key)
        where TKey : notnull
        where TValue : class
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        dictionary[key] = null;
        return null;
    }

    /// <summary>
    ///     Gets the value for the specified key, or inserts a new default-constructed value if the key doesn't exist.
    /// </summary>
    /// <remarks>
    ///     Ergonomic overload for the common case of bucket-style aggregation (dictionary of lists, sets, counters).
    ///     Matches <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,Func{TKey,TValue})" />
    ///     shape. For closure-free factories use
    ///     <see cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TContext, TValue})" />.
    /// </remarks>
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new()
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = new TValue();
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    ///     Gets the value for the specified key, or inserts the result of <paramref name="factory" /> if the key doesn't exist.
    /// </summary>
    /// <remarks>
    ///     Use a <c>static</c> lambda to avoid closure allocation. For lambdas that need external state, prefer the
    ///     closure-free <see cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TKey, TContext, TValue})" /> overload.
    /// </remarks>
    public static TValue GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = factory(key);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    ///     Copies <paramref name="sourceKey" /> into <paramref name="destinationKey" /> when the source is present
    ///     and the destination is absent. Returns <c>true</c> if the dictionary was mutated.
    /// </summary>
    /// <remarks>
    ///     Extracts the "copy-if-absent under new name" pattern that shows up repeatedly in vendor-telemetry adapters
    ///     (e.g. mapping Codex/Cursor GenAI attribute keys onto the OTel semconv names). The optional
    ///     <paramref name="transform" /> lets callers reshape the value during the copy.
    /// </remarks>
    public static bool MapIfAbsent<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey sourceKey,
        TKey destinationKey,
        Func<TValue, TValue>? transform = null)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(destinationKey))
            return false;

        if (!dictionary.TryGetValue(sourceKey, out var value))
            return false;

        dictionary[destinationKey] = transform is null ? value : transform(value);
        return true;
    }
}
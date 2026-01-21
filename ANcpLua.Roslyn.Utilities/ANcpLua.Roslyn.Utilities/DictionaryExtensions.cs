using System;
using System.Collections.Generic;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="Dictionary{TKey,TValue}" /> with allocation-free patterns.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide get-or-insert semantics optimized for performance-critical code paths,
///         such as Roslyn analyzers and source generators.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Uses context parameters to avoid closure allocations in hot paths.</description>
///         </item>
///         <item>
///             <description>Factory functions are only invoked when the key is not found.</description>
///         </item>
///         <item>
///             <description>Newly created values are automatically added to the dictionary.</description>
///         </item>
///     </list>
/// </remarks>
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
    ///     cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TResult})" />
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
    ///     Gets the value for the specified key, or inserts a default value if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up or insert.</param>
    /// <returns>
    ///     The existing value if <paramref name="key" /> is found in the dictionary;
    ///     otherwise, <c>default</c>(<typeparamref name="TValue" />), which is also inserted into the dictionary.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is a convenience method for cases where the default value of <typeparamref name="TValue" />
    ///         is an appropriate initial value (e.g., <c>0</c> for numeric types, <c>null</c> for reference types,
    ///         or a new instance for value types with parameterless constructors).
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TContext, TValue})" />
    /// <seealso
    ///     cref="GetOrInsert{TKey, TValue, TContext}(Dictionary{TKey, TValue}, TKey, TContext, Func{TKey, TContext, TValue})" />
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
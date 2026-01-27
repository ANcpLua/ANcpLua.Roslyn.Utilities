using System.Reflection;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Extension methods for <see cref="object" /> providing safe casting, type checking, and reflection helpers.
/// </summary>
/// <remarks>
///     <para>
///         This class provides fluent alternatives to common casting patterns, reducing verbosity
///         and improving readability. All methods are designed to be safe and predictable.
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Safe casting:</b> <see cref="As{T}(object?)" /> and <see cref="AsValue{T}(object?)" />
///                 provide clean alternatives to <c>as</c> and <c>is</c> patterns.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Type checking:</b> <c>Is&lt;T&gt;</c> combines type checking and casting.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Reflection:</b> <see cref="HasProperty" /> and <c>TryGetPropertyValue&lt;T&gt;</c>
///                 provide safe property access without throwing.
///             </description>
///         </item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ObjectExtensions
{
    // ========== Safe Casting ==========

    /// <summary>
    ///     Safely casts an object to the specified reference type.
    /// </summary>
    /// <typeparam name="T">The target reference type.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>The object cast to <typeparamref name="T" />, or <c>null</c> if the cast is not valid.</returns>
    /// <remarks>
    ///     <para>
    ///         This is equivalent to the <c>as</c> operator but provides a more fluent syntax
    ///         that chains well with null-conditional operators.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Before
    /// var method = (symbol as IMethodSymbol)?.Parameters;
    ///
    /// // After - more readable chaining
    /// var method = symbol.As&lt;IMethodSymbol&gt;()?.Parameters;
    /// </code>
    /// </example>
    /// <seealso cref="AsValue{T}(object?)" />
    public static T? As<T>(this object? obj) where T : class
        => obj as T;

    /// <summary>
    ///     Safely casts an object to the specified value type.
    /// </summary>
    /// <typeparam name="T">The target value type.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>
    ///     The object unboxed to <typeparamref name="T" /> if the cast is valid;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this for unboxing value types from <see cref="object" />.
    ///         Returns <c>null</c> instead of throwing <see cref="InvalidCastException" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// object boxedInt = 42;
    /// int? value = boxedInt.AsValue&lt;int&gt;(); // 42
    ///
    /// object boxedString = "hello";
    /// int? value2 = boxedString.AsValue&lt;int&gt;(); // null
    /// </code>
    /// </example>
    /// <seealso cref="As{T}(object?)" />
    public static T? AsValue<T>(this object? obj) where T : struct
        => obj is T value ? value : null;

    /// <summary>
    ///     Checks if an object is of a specific type and returns the cast result.
    /// </summary>
    /// <typeparam name="T">The type to check for.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="result">
    ///     When this method returns <c>true</c>, contains the object cast to <typeparamref name="T" />;
    ///     otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if <paramref name="obj" /> is of type <typeparamref name="T" />; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         This method combines the type check and cast into a single operation,
    ///         useful in conditions where you need both the check and the cast result.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (symbol.Is&lt;IMethodSymbol&gt;(out var method))
    /// {
    ///     // method is guaranteed non-null here
    ///     ProcessMethod(method);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="As{T}(object?)" />
    /// <seealso cref="IsValue{T}(object?, out T)" />
    public static bool Is<T>(this object? obj, [NotNullWhen(true)] out T? result) where T : class
    {
        result = obj as T;
        return result is not null;
    }

    /// <summary>
    ///     Checks if an object is of a specific value type and returns the unboxed result.
    /// </summary>
    /// <typeparam name="T">The value type to check for.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="result">
    ///     When this method returns <c>true</c>, contains the unboxed value;
    ///     otherwise, <c>default</c>.
    /// </param>
    /// <returns><c>true</c> if <paramref name="obj" /> is of type <typeparamref name="T" />; otherwise, <c>false</c>.</returns>
    /// <example>
    ///     <code>
    /// if (constantValue.IsValue&lt;int&gt;(out var intValue))
    /// {
    ///     ProcessInt(intValue);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AsValue{T}(object?)" />
    public static bool IsValue<T>(this object? obj, out T result) where T : struct
    {
        if (obj is T value)
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Casts an object to the specified type, throwing if the cast fails.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <returns>The object cast to <typeparamref name="T" />.</returns>
    /// <exception cref="InvalidCastException">
    ///     Thrown when <paramref name="obj" /> is <c>null</c> or cannot be cast to <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Use this when you are certain the cast will succeed and want a clear exception if it doesn't.
    ///         For safe casts that return <c>null</c> on failure, use <see cref="As{T}(object?)" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // When you know the type and want a clear exception on failure
    /// var method = symbol.CastTo&lt;IMethodSymbol&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="As{T}(object?)" />
    [return: NotNull]
    public static T CastTo<T>(this object? obj)
        => obj is T result
            ? result
            : throw new InvalidCastException($"Cannot cast {obj?.GetType().Name ?? "null"} to {typeof(T).Name}.");

    // ========== Reflection Helpers ==========

    /// <summary>
    ///     Determines whether the object has a property with the specified name.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="propertyName">The name of the property to look for.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="obj" /> has a public instance property named <paramref name="propertyName" />;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method checks for public instance properties only.
    ///         Returns <c>false</c> if <paramref name="obj" /> is <c>null</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (obj.HasProperty("Name"))
    /// {
    ///     var name = obj.TryGetPropertyValue&lt;string&gt;("Name");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="TryGetPropertyValue{T}(object?, string, T)" />
    public static bool HasProperty(this object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) != null;
    }

    /// <summary>
    ///     Attempts to get the value of a property by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="obj">The object to get the property value from.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="defaultValue">The default value to return if the property doesn't exist or has the wrong type.</param>
    /// <returns>
    ///     The property value if found and of the correct type; otherwise, <paramref name="defaultValue" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method provides safe, exception-free property access via reflection.
    ///         Useful for duck-typing scenarios or when working with dynamic objects.
    ///     </para>
    ///     <para>
    ///         Returns <paramref name="defaultValue" /> in any of these cases:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><paramref name="obj" /> is <c>null</c></description>
    ///         </item>
    ///         <item>
    ///             <description>The property doesn't exist</description>
    ///         </item>
    ///         <item>
    ///             <description>The property value is <c>null</c></description>
    ///         </item>
    ///         <item>
    ///             <description>The property value cannot be cast to <typeparamref name="T" /></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Get a property value with a fallback
    /// var name = obj.TryGetPropertyValue&lt;string&gt;("Name", "Unknown");
    ///
    /// // Check multiple properties
    /// var id = obj.TryGetPropertyValue&lt;int&gt;("Id", -1);
    /// var isActive = obj.TryGetPropertyValue&lt;bool&gt;("IsActive", false);
    /// </code>
    /// </example>
    /// <seealso cref="HasProperty" />
    public static T? TryGetPropertyValue<T>(this object? obj, string propertyName, T? defaultValue = default)
    {
        if (obj is null)
            return defaultValue;

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            return defaultValue;

        try
        {
            var value = property.GetValue(obj);
            return value is T typedValue ? typedValue : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    ///     Attempts to get the value of a property by name using try-pattern.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="obj">The object to get the property value from.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">
    ///     When this method returns <c>true</c>, contains the property value;
    ///     otherwise, <c>default</c>.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the property exists and its value was successfully retrieved and cast;
    ///     otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (obj.TryGetPropertyValue&lt;string&gt;("Name", out var name))
    /// {
    ///     Console.WriteLine(name);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="TryGetPropertyValue{T}(object?, string, T)" />
    public static bool TryGetPropertyValue<T>(this object? obj, string propertyName, out T? value)
    {
        value = default;

        var property = obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            return false;

        try
        {
            var rawValue = property.GetValue(obj);
            if (rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    // ========== Equality and Comparison ==========

    /// <summary>
    ///     Determines whether an object is one of the specified values.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="values">The values to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj" /> equals any of <paramref name="values" />; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         Uses <see cref="EqualityComparer{T}.Default" /> for comparison.
    ///         Short-circuits on the first match.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (status.IsOneOf(Status.Active, Status.Pending))
    /// {
    ///     // Process active or pending status
    /// }
    ///
    /// // Works with strings too
    /// if (extension.IsOneOf(".cs", ".vb", ".fs"))
    /// {
    ///     // It's a .NET source file
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsNotOneOf{T}(T, T[])" />
    public static bool IsOneOf<T>(this T obj, params T[] values)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var value in values)
            if (comparer.Equals(obj, value))
                return true;

        return false;
    }

    /// <summary>
    ///     Determines whether an object is not one of the specified values.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <param name="values">The values to compare against.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="obj" /> does not equal any of <paramref name="values" />; otherwise,
    ///     <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// if (status.IsNotOneOf(Status.Deleted, Status.Archived))
    /// {
    ///     // Process non-deleted, non-archived items
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IsOneOf{T}(T, T[])" />
    public static bool IsNotOneOf<T>(this T obj, params T[] values)
        => !obj.IsOneOf(values);
}

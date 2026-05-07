using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Author-side helpers for <c>bool TryX(out T)</c> patterns: compact one-liners
///     that initialize <c>out</c> parameters on the failure or success return path.
/// </summary>
/// <remarks>
///     <para>
///         Pairs with <see cref="TryExtensions" />, the consumer-side surface that turns
///         <c>bool + out</c> into <c>T?</c>. <see cref="TryResult" /> is the opposite direction:
///         it exists for code that <em>writes</em> a <c>bool TryFoo(out T)</c> method and wants to
///         collapse the failure or success branch from three or four lines to one.
///     </para>
///     <para>
///         All overloads request <see cref="MethodImplOptions.AggressiveInlining" />, keeping them close to a hand-written
///         <c>result = default; return false;</c> on the failure path or
///         <c>result = value; return true;</c> on the success path.
///     </para>
///     <para>
///         On .NET 9+, the type parameters carry <c>allows ref struct</c>, so the helpers also work when
///         an <c>out</c> parameter is a <see cref="System.Span{T}" />, <see cref="System.ReadOnlySpan{T}" />,
///         or any other ref struct — something the BCL's <c>Try*</c> shape can't express generically.
///         On <c>netstandard2.0</c> the relaxation is conditionally compiled out.
///     </para>
///     <para>
///         The <see cref="Fail{TReturn, T}(TReturn, out T)" /> family generalizes past <c>bool</c>-returning
///         try-methods: use it when the enclosing method returns an enum status, an int error code, or any
///         non-<c>bool</c> sentinel.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Multi-out failure path: from four lines to one.
/// public bool TryParse(string input, out Foo foo, out Bar bar)
/// {
///     if (!IsValid(input))
///         return TryResult.Fail(out foo, out bar);
///
///     foo = ParseFoo(input);
///     bar = ParseBar(input);
///     return true;
/// }
///
/// // Single-out success and failure on one expression each.
/// public bool TryGetCached(string key, out Value result)
///     =&gt; _cache.TryGetValue(key, out var hit)
///         ? TryResult.Ok(hit, out result)
///         : TryResult.Fail(out result);
///
/// // Non-bool return type.
/// public ParseStatus TryParse(string input, out Foo foo)
/// {
///     if (string.IsNullOrEmpty(input))
///         return TryResult.Fail(ParseStatus.EmptyInput, out foo);
///     // ...
/// }
///     </code>
/// </example>
/// <seealso cref="TryExtensions" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class TryResult
{
    /// <summary>
    ///     Sets <paramref name="result" /> to <c>default</c> and returns <c>false</c>.
    /// </summary>
    /// <typeparam name="T">The type of the <c>out</c> parameter.</typeparam>
    /// <param name="result">When this method returns, contains <c>default(T)</c>.</param>
    /// <returns><c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Fail<T>(out T? result)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    {
        result = default;
        return false;
    }

    /// <summary>
    ///     Sets two <c>out</c> parameters to <c>default</c> and returns <c>false</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <param name="result1">When this method returns, contains <c>default(T1)</c>.</param>
    /// <param name="result2">When this method returns, contains <c>default(T2)</c>.</param>
    /// <returns><c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Fail<T1, T2>(out T1? result1, out T2? result2)
#if NET9_0_OR_GREATER
        where T1 : allows ref struct
        where T2 : allows ref struct
#endif
    {
        result1 = default;
        result2 = default;
        return false;
    }

    /// <summary>
    ///     Sets three <c>out</c> parameters to <c>default</c> and returns <c>false</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <typeparam name="T3">The type of the third <c>out</c> parameter.</typeparam>
    /// <param name="result1">When this method returns, contains <c>default(T1)</c>.</param>
    /// <param name="result2">When this method returns, contains <c>default(T2)</c>.</param>
    /// <param name="result3">When this method returns, contains <c>default(T3)</c>.</param>
    /// <returns><c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Fail<T1, T2, T3>(out T1? result1, out T2? result2, out T3? result3)
#if NET9_0_OR_GREATER
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
#endif
    {
        result1 = default;
        result2 = default;
        result3 = default;
        return false;
    }

    /// <summary>
    ///     Sets <paramref name="result" /> to <c>default</c> and returns <paramref name="returnValue" />.
    /// </summary>
    /// <remarks>
    ///     Use when the enclosing <c>TryX</c> method returns something other than <c>bool</c>
    ///     (e.g. an enum status code, a parsed primary value, or a non-trivial sentinel).
    /// </remarks>
    /// <typeparam name="TReturn">The return type of the enclosing method.</typeparam>
    /// <typeparam name="T">The type of the <c>out</c> parameter.</typeparam>
    /// <param name="returnValue">The value to return.</param>
    /// <param name="result">When this method returns, contains <c>default(T)</c>.</param>
    /// <returns><paramref name="returnValue" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TReturn Fail<TReturn, T>(TReturn returnValue, out T? result)
#if NET9_0_OR_GREATER
        where TReturn : allows ref struct
        where T : allows ref struct
#endif
    {
        result = default;
        return returnValue;
    }

    /// <summary>
    ///     Sets two <c>out</c> parameters to <c>default</c> and returns <paramref name="returnValue" />.
    /// </summary>
    /// <typeparam name="TReturn">The return type of the enclosing method.</typeparam>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <param name="returnValue">The value to return.</param>
    /// <param name="result1">When this method returns, contains <c>default(T1)</c>.</param>
    /// <param name="result2">When this method returns, contains <c>default(T2)</c>.</param>
    /// <returns><paramref name="returnValue" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TReturn Fail<TReturn, T1, T2>(TReturn returnValue, out T1? result1, out T2? result2)
#if NET9_0_OR_GREATER
        where TReturn : allows ref struct
        where T1 : allows ref struct
        where T2 : allows ref struct
#endif
    {
        result1 = default;
        result2 = default;
        return returnValue;
    }

    /// <summary>
    ///     Sets three <c>out</c> parameters to <c>default</c> and returns <paramref name="returnValue" />.
    /// </summary>
    /// <typeparam name="TReturn">The return type of the enclosing method.</typeparam>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <typeparam name="T3">The type of the third <c>out</c> parameter.</typeparam>
    /// <param name="returnValue">The value to return.</param>
    /// <param name="result1">When this method returns, contains <c>default(T1)</c>.</param>
    /// <param name="result2">When this method returns, contains <c>default(T2)</c>.</param>
    /// <param name="result3">When this method returns, contains <c>default(T3)</c>.</param>
    /// <returns><paramref name="returnValue" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TReturn Fail<TReturn, T1, T2, T3>(TReturn returnValue,
        out T1? result1, out T2? result2, out T3? result3)
#if NET9_0_OR_GREATER
        where TReturn : allows ref struct
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
#endif
    {
        result1 = default;
        result2 = default;
        result3 = default;
        return returnValue;
    }

    /// <summary>
    ///     Assigns <paramref name="value" /> to <paramref name="result" /> and returns <c>true</c>.
    /// </summary>
    /// <remarks>
    ///     The success-path counterpart to <see cref="Fail{T}(out T)" />. Lets the success and failure
    ///     branches of a <c>TryX</c> method be expressed as conditional expressions rather than
    ///     <c>if</c>/<c>else</c> blocks.
    /// </remarks>
    /// <typeparam name="T">The type of the <c>out</c> parameter.</typeparam>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">When this method returns, contains <paramref name="value" />.</param>
    /// <returns><c>true</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Ok<T>(T value, out T result)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    {
        result = value;
        return true;
    }

    /// <summary>
    ///     Assigns the supplied values to two <c>out</c> parameters and returns <c>true</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <param name="value1">The value assigned to <paramref name="result1" />.</param>
    /// <param name="value2">The value assigned to <paramref name="result2" />.</param>
    /// <param name="result1">When this method returns, contains <paramref name="value1" />.</param>
    /// <param name="result2">When this method returns, contains <paramref name="value2" />.</param>
    /// <returns><c>true</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Ok<T1, T2>(T1 value1, T2 value2, out T1 result1, out T2 result2)
#if NET9_0_OR_GREATER
        where T1 : allows ref struct
        where T2 : allows ref struct
#endif
    {
        result1 = value1;
        result2 = value2;
        return true;
    }

    /// <summary>
    ///     Assigns the supplied values to three <c>out</c> parameters and returns <c>true</c>.
    /// </summary>
    /// <typeparam name="T1">The type of the first <c>out</c> parameter.</typeparam>
    /// <typeparam name="T2">The type of the second <c>out</c> parameter.</typeparam>
    /// <typeparam name="T3">The type of the third <c>out</c> parameter.</typeparam>
    /// <param name="value1">The value assigned to <paramref name="result1" />.</param>
    /// <param name="value2">The value assigned to <paramref name="result2" />.</param>
    /// <param name="value3">The value assigned to <paramref name="result3" />.</param>
    /// <param name="result1">When this method returns, contains <paramref name="value1" />.</param>
    /// <param name="result2">When this method returns, contains <paramref name="value2" />.</param>
    /// <param name="result3">When this method returns, contains <paramref name="value3" />.</param>
    /// <returns><c>true</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Ok<T1, T2, T3>(T1 value1, T2 value2, T3 value3,
        out T1 result1, out T2 result2, out T3 result3)
#if NET9_0_OR_GREATER
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
#endif
    {
        result1 = value1;
        result2 = value2;
        result3 = value3;
        return true;
    }
}

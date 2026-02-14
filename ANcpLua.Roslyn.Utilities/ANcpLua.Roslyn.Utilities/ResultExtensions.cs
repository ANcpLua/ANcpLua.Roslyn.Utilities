using System.Threading.Tasks;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Functional composition extensions for <see cref="Result{T}" />.
/// </summary>
/// <remarks>
///     <para>
///         Method naming mirrors <see cref="DiagnosticFlow{T}" /> for consistency:
///     </para>
///     <list type="bullet">
///         <item><description><see cref="Select{T,TNext}" /> — transforms the value (map).</description></item>
///         <item><description><see cref="Then{T,TNext}" /> — chains a result-producing function (bind).</description></item>
///         <item><description><see cref="Where{T}" /> — filters with a predicate.</description></item>
///         <item><description><see cref="Tap{T}(Result{T},Action{T})" /> — executes a side effect.</description></item>
///     </list>
/// </remarks>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class ResultExtensions
{
    /// <summary>Transforms the value if successful (map).</summary>
    public static Result<TNext> Select<T, TNext>(this Result<T> result, Func<T, TNext> selector) =>
        result.IsOk ? Result<TNext>.Ok(selector(result.Value)) : Result<TNext>.Fail(result.Error);

    /// <summary>Chains a result-producing function if successful (bind/flatMap).</summary>
    public static Result<TNext> Then<T, TNext>(this Result<T> result, Func<T, Result<TNext>> next) =>
        result.IsOk ? next(result.Value) : Result<TNext>.Fail(result.Error);

    /// <summary>Executes a side effect if successful, returning the original result.</summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsOk) action(result.Value);
        return result;
    }

    /// <summary>Filters the result, failing if the predicate returns <c>false</c>.</summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, Error errorIfFalse) =>
        result.IsOk && !predicate(result.Value) ? Result<T>.Fail(errorIfFalse) : result;

    /// <summary>Gets the value or a fallback if failed.</summary>
    public static T ValueOr<T>(this Result<T> result, T fallback) =>
        result.IsOk ? result.Value : fallback;

    /// <summary>Gets the value or computes a fallback from the error.</summary>
    public static T ValueOr<T>(this Result<T> result, Func<Error, T> fallback) =>
        result.IsOk ? result.Value : fallback(result.Error);

    // ── Async extensions ─────────────────────────────────────────────

    /// <summary>Creates a result from a nullable task, failing if the value is <c>null</c>.</summary>
    public static async Task<Result<T>> ToResult<T>(this Task<T?> task, Error errorIfNull) where T : class
    {
        var value = await task.ConfigureAwait(false);
        return value is null ? Result<T>.Fail(errorIfNull) : Result<T>.Ok(value);
    }

    /// <summary>Transforms the value of an async result (async map).</summary>
    public static async Task<Result<TNext>> SelectAsync<T, TNext>(
        this Task<Result<T>> task, Func<T, TNext> selector)
    {
        var result = await task.ConfigureAwait(false);
        return result.Select(selector);
    }

    /// <summary>Chains an async result-producing function (async bind).</summary>
    public static async Task<Result<TNext>> ThenAsync<T, TNext>(
        this Task<Result<T>> task, Func<T, Task<Result<TNext>>> next)
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? await next(result.Value).ConfigureAwait(false)
            : Result<TNext>.Fail(result.Error);
    }

    /// <summary>Chains a synchronous result-producing function on an async result.</summary>
    public static async Task<Result<TNext>> ThenAsync<T, TNext>(
        this Task<Result<T>> task, Func<T, Result<TNext>> next)
    {
        var result = await task.ConfigureAwait(false);
        return result.Then(next);
    }

    /// <summary>Executes an async side effect if successful.</summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> task, Func<T, Task> action)
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsOk) await action(result.Value).ConfigureAwait(false);
        return result;
    }

    /// <summary>Executes a synchronous side effect on an async result.</summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> task, Action<T> action)
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsOk) action(result.Value);
        return result;
    }
}

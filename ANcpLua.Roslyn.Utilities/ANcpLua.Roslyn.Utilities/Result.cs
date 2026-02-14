namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Represents an error with a code and human-readable message.
/// </summary>
/// <param name="Code">A machine-readable error code (e.g., <c>"order.not_found"</c>).</param>
/// <param name="Message">A human-readable error description.</param>
/// <remarks>
///     <para>
///         Use as the failure type in <see cref="Result{T}" /> for domain error handling.
///         For Roslyn diagnostic pipelines, use <see cref="DiagnosticFlow{T}" /> instead.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var error = new Error("order.not_found", "Order 42 not found.");
/// Result&lt;Order&gt; result = error; // implicit conversion
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed record Error(string Code, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>
///     A general-purpose result type representing success or failure.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description><b>Value type.</b> Zero-allocation on success paths.</description>
///         </item>
///         <item>
///             <description>
///                 <b>Equatable.</b> Implements value equality for caching and assertions.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Composable.</b> Use <see cref="ResultExtensions" /> for
///                 <c>Select</c>, <c>Then</c>, <c>Tap</c>, and <c>Where</c> chains.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Implicit conversions.</b> Both <typeparamref name="T" /> and <see cref="Error" />
///                 convert implicitly to <see cref="Result{T}" />.
///             </description>
///         </item>
///     </list>
///     <para>
///         For Roslyn diagnostic pipelines, use <see cref="DiagnosticFlow{T}" /> instead —
///         it accumulates multiple diagnostics and integrates with incremental generators.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Success
/// Result&lt;int&gt; ok = Result&lt;int&gt;.Ok(42);
/// Result&lt;int&gt; okImplicit = 42;
///
/// // Failure
/// Result&lt;int&gt; fail = Result&lt;int&gt;.Fail(new Error("invalid", "Bad input"));
/// Result&lt;int&gt; failImplicit = new Error("invalid", "Bad input");
///
/// // Pattern match
/// string msg = ok.Match(v =&gt; $"Got {v}", e =&gt; e.Message);
///
/// // Railway composition
/// Result&lt;string&gt; pipeline = ok
///     .Where(x =&gt; x &gt; 0, new Error("negative", "Must be positive"))
///     .Select(x =&gt; x.ToString())
///     .Tap(Console.WriteLine);
/// </code>
/// </example>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct Result<T> : IEquatable<Result<T>>
{
    readonly T? _value;
    readonly Error? _error;

    Result(T value)
    {
        _value = value;
        _error = null;
    }

    Result(Error error)
    {
        _value = default;
        _error = Guard.NotNull(error);
    }

    /// <summary>Gets a value indicating whether this result is successful.</summary>
    public bool IsOk => _error is null;

    /// <summary>Gets a value indicating whether this result is a failure.</summary>
    public bool IsFailed => _error is not null;

    /// <summary>Gets the success value.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsOk
        ? _value!
        : throw new InvalidOperationException($"Result is failed: {_error}");

    /// <summary>Gets the error.</summary>
    /// <exception cref="InvalidOperationException">The result is successful.</exception>
    public Error Error => _error
        ?? throw new InvalidOperationException("Result is successful.");

    /// <summary>Creates a successful result.</summary>
    public static Result<T> Ok(T value) => new(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<T> Fail(Error error) => new(error);

    /// <summary>Implicitly converts a value to a successful result.</summary>
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>Implicitly converts an error to a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Fail(error);

    /// <summary>Pattern-matches on the result, returning a value of <typeparamref name="TResult" />.</summary>
    public TResult Match<TResult>(Func<T, TResult> ok, Func<Error, TResult> fail) =>
        IsOk ? ok(_value!) : fail(_error!);

    /// <summary>Pattern-matches on the result with side effects.</summary>
    public void Match(Action<T> ok, Action<Error> fail)
    {
        if (IsOk) ok(_value!);
        else fail(_error!);
    }

    /// <inheritdoc />
    public bool Equals(Result<T> other) =>
        IsOk == other.IsOk &&
        (IsOk
            ? EqualityComparer<T>.Default.Equals(_value!, other._value!)
            : _error!.Equals(other._error!));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsOk ? EqualityComparer<T>.Default.GetHashCode(_value!) : _error!.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => IsOk ? $"Ok({_value})" : $"Fail({_error})";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}

/// <summary>
///     Factory methods for creating <see cref="Result{T}" /> instances.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class Result
{
    /// <summary>Creates a successful result.</summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<T> Fail<T>(Error error) => Result<T>.Fail(error);

    /// <summary>Creates a result from a nullable reference, failing if <c>null</c>.</summary>
    public static Result<T> FromNullable<T>(T? value, Error errorIfNull) where T : class =>
        value is not null ? Result<T>.Ok(value) : Result<T>.Fail(errorIfNull);

    /// <summary>Creates a result from a nullable value type, failing if <c>null</c>.</summary>
    public static Result<T> FromNullable<T>(T? value, Error errorIfNull) where T : struct =>
        value.HasValue ? Result<T>.Ok(value.Value) : Result<T>.Fail(errorIfNull);

    /// <summary>Wraps a function that may throw, catching exceptions as failures.</summary>
    public static Result<T> Try<T>(Func<T> action, Func<Exception, Error> onError)
    {
        try
        {
            return Result<T>.Ok(action());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(onError(ex));
        }
    }
}

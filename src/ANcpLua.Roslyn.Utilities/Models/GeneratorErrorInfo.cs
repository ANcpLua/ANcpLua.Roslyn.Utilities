// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     A value-equatable snapshot of an <see cref="Exception" /> safe to flow through the
///     incremental generator cache.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Exception" /> instances do not implement value equality, so storing one in
///         pipeline state forces every cache comparison to reference-equality and breaks
///         incremental caching across compilations. <see cref="GeneratorErrorInfo" /> captures the
///         minimum information needed to surface the failure as a diagnostic — type name, message,
///         and stack trace — as immutable strings, which the record-struct equality contract
///         compares by value.
///     </para>
/// </remarks>
/// <param name="TypeName">The exception's runtime type name (e.g. <c>System.InvalidOperationException</c>).</param>
/// <param name="Message">The exception's message, or empty if absent.</param>
/// <param name="StackTrace">The exception's stack trace, or empty if absent.</param>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct GeneratorErrorInfo(string TypeName, string Message, string StackTrace)
{
    /// <summary>
    ///     Captures an <see cref="Exception" /> as a value-equatable <see cref="GeneratorErrorInfo" />.
    /// </summary>
    public static GeneratorErrorInfo From(Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        return new GeneratorErrorInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message ?? string.Empty,
            exception.StackTrace ?? string.Empty);
    }

    /// <summary>
    ///     Renders the captured error in <c>Type: Message\n   StackTrace</c> form, mirroring
    ///     <see cref="Exception.ToString" />.
    /// </summary>
    public override string ToString()
    {
        return string.IsNullOrEmpty(StackTrace)
            ? $"{TypeName}: {Message}"
            : $"{TypeName}: {Message}{Environment.NewLine}{StackTrace}";
    }
}

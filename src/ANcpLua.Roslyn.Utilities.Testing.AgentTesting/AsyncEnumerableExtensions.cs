// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
///     Extensions for converting synchronous enumerables to async enumerables in test scenarios.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    ///     Converts a synchronous <see cref="IEnumerable{T}" /> to an <see cref="IAsyncEnumerable{T}" />
    ///     for testing async streaming pipelines.
    /// </summary>
    public static async IAsyncEnumerable<T> ToAsyncEnumerableAsync<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
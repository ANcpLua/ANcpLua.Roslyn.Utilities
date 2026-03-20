#if !NETSTANDARD
namespace ANcpLua.Roslyn.Utilities.Streaming;

/// <summary>
///     A parsed Server-Sent Event (SSE) containing an event type and data payload.
/// </summary>
/// <param name="Type">The event type (defaults to <c>"message"</c> per SSE specification).</param>
/// <param name="Data">The event data payload.</param>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed record SseEvent(string Type, string Data);
#endif

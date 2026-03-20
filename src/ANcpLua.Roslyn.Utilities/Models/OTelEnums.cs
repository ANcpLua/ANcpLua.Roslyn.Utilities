namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     OTel span kind describing the relationship between spans.
///     Values match the OTel protobuf specification.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum SpanKind : byte
{
    /// <summary>Span kind is unspecified.</summary>
    Unspecified = 0,

    /// <summary>Internal operation within an application.</summary>
    Internal = 1,

    /// <summary>Server-side handling of an RPC or HTTP request.</summary>
    Server = 2,

    /// <summary>Client-side of an RPC or HTTP request.</summary>
    Client = 3,

    /// <summary>Producer of an asynchronous message.</summary>
    Producer = 4,

    /// <summary>Consumer of an asynchronous message.</summary>
    Consumer = 5
}

/// <summary>
///     OTel span status code.
///     Values match the OTel protobuf specification.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    enum SpanStatusCode : byte
{
    /// <summary>Status is not set.</summary>
    Unset = 0,

    /// <summary>The operation completed successfully.</summary>
    Ok = 1,

    /// <summary>The operation contained an error.</summary>
    Error = 2
}

// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.Logging;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// A captured log entry produced by <see cref="TestOutputAdapter"/>.
/// </summary>
/// <param name="Level">The log level.</param>
/// <param name="EventId">The event ID.</param>
/// <param name="Message">The formatted log message.</param>
/// <param name="Exception">The exception, if any.</param>
public readonly record struct LogRecord(LogLevel Level, EventId EventId, string Message, Exception? Exception);

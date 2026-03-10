// Licensed to the .NET Foundation under one or more agreements.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Bridges xUnit's <see cref="ITestOutputHelper"/> to both <see cref="ILogger"/>
/// and <see cref="TextWriter"/>, capturing all output into test results.
/// Also captures structured <see cref="LogRecord"/> entries for assertion.
/// </summary>
public sealed class TestOutputAdapter(ITestOutputHelper output) : TextWriter, ILogger, ILoggerFactory
{
    private readonly ITestOutputHelper _output = output;
    private readonly Stack<string> _scopes = [];
    private readonly ConcurrentQueue<LogRecord> _capturedLogs = new();

    /// <inheritdoc/>
    public override Encoding Encoding { get; } = Encoding.UTF8;

    /// <summary>
    /// Returns a snapshot of all captured log entries.
    /// </summary>
    public IReadOnlyList<LogRecord> GetCapturedLogs() => [.. _capturedLogs];

    /// <summary>
    /// Returns a snapshot of captured log entries at the specified level.
    /// </summary>
    public IReadOnlyList<LogRecord> GetCapturedLogs(LogLevel level) =>
        [.. _capturedLogs.Where(r => r.Level == level)];

    /// <summary>
    /// Clears all captured log entries.
    /// </summary>
    public void ClearCapturedLogs()
    {
        while (_capturedLogs.TryDequeue(out _))
        {
        }
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => this;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public override void WriteLine(object? value) => SafeWrite($"{value}");

    /// <inheritdoc/>
    public override void WriteLine(string? format, params object?[] arg) =>
        SafeWrite(string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, arg));

    /// <inheritdoc/>
    public override void WriteLine(string? value) => SafeWrite(value ?? string.Empty);

    /// <inheritdoc/>
    public override void Write(object? value) => SafeWrite($"{value}");

    /// <inheritdoc/>
    public override void Write(char[]? buffer) => SafeWrite(new string(buffer));

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        _scopes.Push($"{state}");
        return new LoggerScope(() => _scopes.Pop());
    }

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        _capturedLogs.Enqueue(new LogRecord(logLevel, eventId, message, exception));
        string scope = _scopes.Count > 0 ? $"[{_scopes.Peek()}] " : string.Empty;
        SafeWrite($"{scope}{message}");
    }

    void ILoggerFactory.AddProvider(ILoggerProvider provider)
    {
    }

    private void SafeWrite(string value)
    {
        try
        {
            _output.WriteLine(value);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no currently active test", StringComparison.Ordinal))
        {
            // Swallow — test context no longer active (e.g. async continuation after test completes)
        }
    }

    private sealed class LoggerScope(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                onDispose();
                _disposed = true;
            }
        }
    }
}

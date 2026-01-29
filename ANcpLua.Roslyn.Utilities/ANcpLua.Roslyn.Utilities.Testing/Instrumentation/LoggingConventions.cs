// =============================================================================
// Logging Conventions - Event IDs, Tag Names, and Message Templates
// Production utilities for [LoggerMessage] source generation
// =============================================================================

using Microsoft.Extensions.Logging;

namespace ANcpLua.Roslyn.Utilities.Instrumentation;

/// <summary>
///     Standardized event ID ranges for structured logging.
///     Use with <see cref="LoggerMessageAttribute"/> EventId parameter.
/// </summary>
/// <remarks>
///     Event IDs provide stable identifiers for log correlation and alerting.
///     Organize by functional domain with 1000-event ranges.
/// </remarks>
public static class EventIds
{
    // Startup/Shutdown: 1000-1999
    public const int StartupBegin = 1000;
    public const int StartupComplete = 1001;
    public const int ShutdownBegin = 1002;
    public const int ShutdownComplete = 1003;
    public const int ConfigurationLoaded = 1010;
    public const int ConfigurationError = 1011;
    public const int DependencyResolved = 1020;
    public const int DependencyFailed = 1021;

    // HTTP/API: 2000-2999
    public const int RequestReceived = 2000;
    public const int RequestCompleted = 2001;
    public const int RequestFailed = 2002;
    public const int RequestValidationFailed = 2003;
    public const int ResponseSent = 2010;
    public const int ResponseError = 2011;
    public const int AuthenticationSucceeded = 2100;
    public const int AuthenticationFailed = 2101;
    public const int AuthorizationSucceeded = 2110;
    public const int AuthorizationFailed = 2111;

    // Data/Storage: 3000-3999
    public const int QueryExecuted = 3000;
    public const int QueryFailed = 3001;
    public const int RecordCreated = 3010;
    public const int RecordUpdated = 3011;
    public const int RecordDeleted = 3012;
    public const int ConnectionOpened = 3100;
    public const int ConnectionClosed = 3101;
    public const int ConnectionFailed = 3102;
    public const int TransactionStarted = 3200;
    public const int TransactionCommitted = 3201;
    public const int TransactionRolledBack = 3202;

    // GenAI/LLM: 4000-4999
    public const int GenAiRequestStarted = 4000;
    public const int GenAiRequestCompleted = 4001;
    public const int GenAiRequestFailed = 4002;
    public const int GenAiStreamStarted = 4010;
    public const int GenAiStreamChunk = 4011;
    public const int GenAiStreamCompleted = 4012;
    public const int GenAiToolCall = 4100;
    public const int GenAiToolResult = 4101;
    public const int GenAiRateLimited = 4200;
    public const int GenAiTokenLimitExceeded = 4201;

    // Background/Jobs: 5000-5999
    public const int JobStarted = 5000;
    public const int JobCompleted = 5001;
    public const int JobFailed = 5002;
    public const int JobCancelled = 5003;
    public const int JobRetrying = 5010;
    public const int JobRetryExhausted = 5011;
    public const int ScheduledTaskTriggered = 5100;
    public const int QueueItemProcessed = 5200;

    // External Services: 6000-6999
    public const int ExternalCallStarted = 6000;
    public const int ExternalCallCompleted = 6001;
    public const int ExternalCallFailed = 6002;
    public const int ExternalCallRetrying = 6003;
    public const int CircuitBreakerOpened = 6100;
    public const int CircuitBreakerClosed = 6101;

    // Cache: 7000-7999
    public const int CacheHit = 7000;
    public const int CacheMiss = 7001;
    public const int CacheSet = 7002;
    public const int CacheEvicted = 7003;
    public const int CacheExpired = 7004;

    // Health/Diagnostics: 8000-8999
    public const int HealthCheckPassed = 8000;
    public const int HealthCheckFailed = 8001;
    public const int MetricRecorded = 8100;
    public const int DiagnosticEvent = 8200;
}

/// <summary>
///     OTel semantic convention tag names for structured logging.
///     Use with <c>[TagName]</c> attribute on log parameters.
/// </summary>
public static class LogTags
{
    // Trace context
    public const string TraceId = "trace.id";
    public const string SpanId = "span.id";
    public const string ParentSpanId = "span.parent_id";

    // Service identity
    public const string ServiceName = "service.name";
    public const string ServiceVersion = "service.version";
    public const string ServiceInstanceId = "service.instance.id";

    // HTTP
    public const string HttpMethod = "http.request.method";
    public const string HttpUrl = "url.full";
    public const string HttpPath = "url.path";
    public const string HttpStatusCode = "http.response.status_code";
    public const string HttpRoute = "http.route";
    public const string ClientAddress = "client.address";
    public const string UserAgent = "user_agent.original";

    // Database
    public const string DbSystem = "db.system.name";
    public const string DbName = "db.namespace";
    public const string DbOperation = "db.operation.name";
    public const string DbStatement = "db.query.text";

    // GenAI (OTel semconv)
    public const string GenAiProvider = "gen_ai.provider.name";
    public const string GenAiModel = "gen_ai.request.model";
    public const string GenAiOperation = "gen_ai.operation.name";
    public const string GenAiInputTokens = "gen_ai.usage.input_tokens";
    public const string GenAiOutputTokens = "gen_ai.usage.output_tokens";
    public const string GenAiFinishReason = "gen_ai.response.finish_reasons";

    // Error
    public const string ErrorType = "error.type";
    public const string ErrorMessage = "error.message";
    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";
    public const string ExceptionStacktrace = "exception.stacktrace";

    // User/Session
    public const string UserId = "user.id";
    public const string SessionId = "session.id";
    public const string TenantId = "tenant.id";

    // Messaging
    public const string MessagingSystem = "messaging.system";
    public const string MessagingDestination = "messaging.destination.name";
    public const string MessagingOperation = "messaging.operation.name";
    public const string MessagingMessageId = "messaging.message.id";

    // Custom/Domain
    public const string OperationName = "operation.name";
    public const string DurationMs = "duration.ms";
    public const string ItemCount = "item.count";
    public const string RequestId = "request.id";
}

/// <summary>
///     Pre-built log scopes for common scenarios.
/// </summary>
public static class LogScopes
{
    /// <summary>Creates a scope with trace context from the current Activity.</summary>
    public static IDisposable? BeginTraceScope(this ILogger logger)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
            return null;

        return logger.BeginScope(new Dictionary<string, object?>
        {
            [LogTags.TraceId] = activity.TraceId.ToString(),
            [LogTags.SpanId] = activity.SpanId.ToString()
        });
    }

    /// <summary>Creates a scope with operation context.</summary>
    /// <returns>A scope disposable, or null if logging is disabled.</returns>
    public static IDisposable? BeginOperationScope(
        this ILogger logger,
        string operationName,
        string? requestId = null)
    {
        var state = new Dictionary<string, object?> { [LogTags.OperationName] = operationName };

        if (requestId is not null)
            state[LogTags.RequestId] = requestId;

        return logger.BeginScope(state);
    }

    /// <summary>Creates a scope with GenAI context.</summary>
    /// <returns>A scope disposable, or null if logging is disabled.</returns>
    public static IDisposable? BeginGenAiScope(
        this ILogger logger,
        string provider,
        string model,
        string operation = "chat")
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            [LogTags.GenAiProvider] = provider,
            [LogTags.GenAiModel] = model,
            [LogTags.GenAiOperation] = operation
        });
    }
}

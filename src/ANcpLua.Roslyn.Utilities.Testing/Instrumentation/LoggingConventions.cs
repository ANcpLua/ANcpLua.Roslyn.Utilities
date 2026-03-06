// =============================================================================
// Logging Conventions - Event IDs, Tag Names, and Message Templates
// Production utilities for [LoggerMessage] source generation
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Standardized event ID ranges for structured logging.
///     Use with <see cref="LoggerMessageAttribute" /> EventId parameter.
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

    // GenAI — Core
    public const string GenAiProvider = "gen_ai.provider.name";
    public const string GenAiOperation = "gen_ai.operation.name";
    public const string GenAiConversationId = "gen_ai.conversation.id";
    public const string GenAiSystemInstructions = "gen_ai.system_instructions";

    // GenAI — Agent
    public const string GenAiAgentDescription = "gen_ai.agent.description";
    public const string GenAiAgentId = "gen_ai.agent.id";
    public const string GenAiAgentName = "gen_ai.agent.name";
    public const string GenAiAgentVersion = "gen_ai.agent.version";

    // GenAI — Request
    public const string GenAiModel = "gen_ai.request.model";
    public const string GenAiRequestMaxTokens = "gen_ai.request.max_tokens";
    public const string GenAiRequestTemperature = "gen_ai.request.temperature";
    public const string GenAiRequestTopP = "gen_ai.request.top_p";
    public const string GenAiRequestTopK = "gen_ai.request.top_k";
    public const string GenAiRequestFrequencyPenalty = "gen_ai.request.frequency_penalty";
    public const string GenAiRequestPresencePenalty = "gen_ai.request.presence_penalty";
    public const string GenAiRequestSeed = "gen_ai.request.seed";
    public const string GenAiRequestStopSequences = "gen_ai.request.stop_sequences";
    public const string GenAiRequestChoiceCount = "gen_ai.request.choice.count";
    public const string GenAiRequestEncodingFormats = "gen_ai.request.encoding_formats";

    // GenAI — Response
    public const string GenAiResponseModel = "gen_ai.response.model";
    public const string GenAiResponseId = "gen_ai.response.id";
    public const string GenAiFinishReason = "gen_ai.response.finish_reasons";

    // GenAI — Usage
    public const string GenAiInputTokens = "gen_ai.usage.input_tokens";
    public const string GenAiOutputTokens = "gen_ai.usage.output_tokens";
    public const string GenAiCacheReadInputTokens = "gen_ai.usage.cache_read.input_tokens";
    public const string GenAiCacheCreationInputTokens = "gen_ai.usage.cache_creation.input_tokens";
    public const string GenAiTokenType = "gen_ai.token.type";

    // GenAI — Input/Output
    public const string GenAiInputMessages = "gen_ai.input.messages";
    public const string GenAiOutputMessages = "gen_ai.output.messages";
    public const string GenAiOutputType = "gen_ai.output.type";

    // GenAI — Tool
    public const string GenAiToolCallArguments = "gen_ai.tool.call.arguments";
    public const string GenAiToolCallId = "gen_ai.tool.call.id";
    public const string GenAiToolCallResult = "gen_ai.tool.call.result";
    public const string GenAiToolDefinitions = "gen_ai.tool.definitions";
    public const string GenAiToolDescription = "gen_ai.tool.description";
    public const string GenAiToolName = "gen_ai.tool.name";
    public const string GenAiToolType = "gen_ai.tool.type";

    // GenAI — Prompt
    public const string GenAiPromptName = "gen_ai.prompt.name";

    // GenAI — Retrieval
    public const string GenAiRetrievalDocuments = "gen_ai.retrieval.documents";
    public const string GenAiRetrievalQueryText = "gen_ai.retrieval.query.text";

    // GenAI — Embeddings
    public const string GenAiEmbeddingsDimensionCount = "gen_ai.embeddings.dimension.count";

    // GenAI — Evaluation
    public const string GenAiEvaluationExplanation = "gen_ai.evaluation.explanation";
    public const string GenAiEvaluationName = "gen_ai.evaluation.name";
    public const string GenAiEvaluationScoreLabel = "gen_ai.evaluation.score.label";
    public const string GenAiEvaluationScoreValue = "gen_ai.evaluation.score.value";

    // GenAI — Data Source
    public const string GenAiDataSourceId = "gen_ai.data_source.id";

    // GenAI — OpenAI vendor-specific
    public const string GenAiOpenaiRequestResponseFormat = "gen_ai.openai.request.response_format";
    public const string GenAiOpenaiRequestSeed = "gen_ai.openai.request.seed";
    public const string GenAiOpenaiRequestServiceTier = "gen_ai.openai.request.service_tier";
    public const string GenAiOpenaiResponseServiceTier = "gen_ai.openai.response.service_tier";
    public const string GenAiOpenaiResponseSystemFingerprint = "gen_ai.openai.response.system_fingerprint";

    // Error
    public const string ErrorType = "error.type";

    /// <summary>Deprecated in semconv v1.40.0. Use domain-specific error attributes instead.</summary>
    [Obsolete("Deprecated in OTel semconv v1.40.0. Use domain-specific codes (e.g. feature_flag.error.message).")]
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
        var activity = Activity.Current;
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
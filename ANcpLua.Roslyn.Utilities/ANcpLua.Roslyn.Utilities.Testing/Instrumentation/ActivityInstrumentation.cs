// =============================================================================
// Activity Instrumentation - Production utilities for distributed tracing
// .NET 10 ActivitySourceOptions with OTel schema URL support
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ANcpLua.Roslyn.Utilities.Instrumentation;

/// <summary>
///     OTel schema versions for telemetry evolution.
/// </summary>
public static class OTelSchema
{
    public const string V1_29_0 = "https://opentelemetry.io/schemas/1.29.0";
    public const string V1_30_0 = "https://opentelemetry.io/schemas/1.30.0";
    public const string V1_39_0 = "https://opentelemetry.io/schemas/1.39.0";

    /// <summary>Current recommended schema version.</summary>
    public const string Current = V1_39_0;
}

/// <summary>
///     Factory for creating <see cref="ActivitySource"/> with OTel conventions.
/// </summary>
public static class ActivitySourceFactory
{
    /// <summary>
    ///     Creates an ActivitySource with .NET 10 options and OTel schema URL.
    /// </summary>
    public static ActivitySource Create(
        string name,
        string? version = null,
        string? schemaUrl = null)
    {
        return new ActivitySource(new ActivitySourceOptions(name)
        {
            Version = version,
            TelemetrySchemaUrl = schemaUrl ?? OTelSchema.Current
        });
    }

    /// <summary>
    ///     Creates an ActivitySource from assembly metadata.
    /// </summary>
    public static ActivitySource CreateFromAssembly<T>(string? schemaUrl = null)
    {
        var assembly = typeof(T).Assembly;
        var name = assembly.GetName().Name ?? typeof(T).Namespace ?? "Unknown";
        var version = assembly.GetName().Version?.ToString();

        return Create(name, version, schemaUrl);
    }
}

/// <summary>
///     OTel semantic convention attribute names for spans.
/// </summary>
public static class SpanAttributes
{
    // General
    public const string OperationName = "operation.name";

    // HTTP Client
    public const string HttpRequestMethod = "http.request.method";
    public const string HttpResponseStatusCode = "http.response.status_code";
    public const string UrlFull = "url.full";
    public const string UrlPath = "url.path";
    public const string UrlScheme = "url.scheme";
    public const string ServerAddress = "server.address";
    public const string ServerPort = "server.port";

    // HTTP Server
    public const string HttpRoute = "http.route";
    public const string ClientAddress = "client.address";
    public const string UserAgentOriginal = "user_agent.original";

    // Database
    public const string DbSystemName = "db.system.name";
    public const string DbNamespace = "db.namespace";
    public const string DbOperationName = "db.operation.name";
    public const string DbQueryText = "db.query.text";

    // Messaging
    public const string MessagingSystem = "messaging.system";
    public const string MessagingDestinationName = "messaging.destination.name";
    public const string MessagingOperationName = "messaging.operation.name";
    public const string MessagingMessageId = "messaging.message.id";

    // GenAI (OTel semconv)
    public const string GenAiProviderName = "gen_ai.provider.name";
    public const string GenAiRequestModel = "gen_ai.request.model";
    public const string GenAiResponseModel = "gen_ai.response.model";
    public const string GenAiOperationName = "gen_ai.operation.name";
    public const string GenAiUsageInputTokens = "gen_ai.usage.input_tokens";
    public const string GenAiUsageOutputTokens = "gen_ai.usage.output_tokens";
    public const string GenAiResponseFinishReasons = "gen_ai.response.finish_reasons";
    public const string GenAiRequestMaxTokens = "gen_ai.request.max_tokens";
    public const string GenAiRequestTemperature = "gen_ai.request.temperature";

    // Error
    public const string ErrorType = "error.type";

    // Exception event attributes
    public const string ExceptionType = "exception.type";
    public const string ExceptionMessage = "exception.message";
    public const string ExceptionStacktrace = "exception.stacktrace";
}

/// <summary>
///     Extension methods for <see cref="Activity"/> with OTel conventions.
///     All methods accept nullable Activity since ActivitySource.StartActivity() can return null.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>Sets the status to OK.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? SetOk(this Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }

    /// <summary>Sets the status to Error with optional description.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? SetError(this Activity? activity, string? description = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, description);
        return activity;
    }

    /// <summary>Records an exception following OTel conventions.</summary>
    public static Activity? RecordException(this Activity? activity, Exception exception, bool escaped = true)
    {
        if (activity is null)
            return null;

        var tags = new ActivityTagsCollection
        {
            [SpanAttributes.ExceptionType] = exception.GetType().FullName ?? exception.GetType().Name,
            [SpanAttributes.ExceptionMessage] = exception.Message
        };

        if (exception.StackTrace is { } stackTrace)
            tags[SpanAttributes.ExceptionStacktrace] = stackTrace;

        activity.AddEvent(new ActivityEvent("exception", tags: tags));

        if (escaped)
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        return activity;
    }

    /// <summary>Sets GenAI request attributes.</summary>
    public static Activity? SetGenAiRequest(
        this Activity? activity,
        string provider,
        string model,
        string operation = "chat",
        int? maxTokens = null,
        double? temperature = null)
    {
        if (activity is null)
            return null;

        activity.SetTag(SpanAttributes.GenAiProviderName, provider);
        activity.SetTag(SpanAttributes.GenAiRequestModel, model);
        activity.SetTag(SpanAttributes.GenAiOperationName, operation);

        if (maxTokens.HasValue)
            activity.SetTag(SpanAttributes.GenAiRequestMaxTokens, maxTokens.Value);

        if (temperature.HasValue)
            activity.SetTag(SpanAttributes.GenAiRequestTemperature, temperature.Value);

        return activity;
    }

    /// <summary>Sets GenAI response attributes.</summary>
    public static Activity? SetGenAiResponse(
        this Activity? activity,
        long? inputTokens = null,
        long? outputTokens = null,
        string? finishReason = null,
        string? responseModel = null)
    {
        if (activity is null)
            return null;

        if (inputTokens.HasValue)
            activity.SetTag(SpanAttributes.GenAiUsageInputTokens, inputTokens.Value);

        if (outputTokens.HasValue)
            activity.SetTag(SpanAttributes.GenAiUsageOutputTokens, outputTokens.Value);

        if (finishReason is not null)
            activity.SetTag(SpanAttributes.GenAiResponseFinishReasons, finishReason);

        if (responseModel is not null)
            activity.SetTag(SpanAttributes.GenAiResponseModel, responseModel);

        return activity;
    }

    /// <summary>Sets HTTP client span attributes.</summary>
    public static Activity? SetHttpClient(
        this Activity? activity,
        string method,
        string url,
        int? statusCode = null)
    {
        if (activity is null)
            return null;

        activity.SetTag(SpanAttributes.HttpRequestMethod, method);
        activity.SetTag(SpanAttributes.UrlFull, url);

        if (statusCode.HasValue)
            activity.SetTag(SpanAttributes.HttpResponseStatusCode, statusCode.Value);

        return activity;
    }

    /// <summary>Sets database span attributes.</summary>
    public static Activity? SetDatabase(
        this Activity? activity,
        string system,
        string? database = null,
        string? operation = null,
        string? statement = null)
    {
        if (activity is null)
            return null;

        activity.SetTag(SpanAttributes.DbSystemName, system);

        if (database is not null)
            activity.SetTag(SpanAttributes.DbNamespace, database);

        if (operation is not null)
            activity.SetTag(SpanAttributes.DbOperationName, operation);

        if (statement is not null)
            activity.SetTag(SpanAttributes.DbQueryText, statement);

        return activity;
    }

    /// <summary>Gets a tag value with type conversion.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetTagValue<T>(this Activity? activity, string key)
    {
        if (activity is null)
            return default;

        var value = activity.GetTagItem(key);
        return value is T typed ? typed : default;
    }

    /// <summary>Checks if activity has GenAI attributes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGenAiAttributes(this Activity? activity) =>
        activity?.GetTagItem(SpanAttributes.GenAiProviderName) is not null;

    /// <summary>Gets total token count from GenAI attributes.</summary>
    public static long GetTotalTokens(this Activity? activity)
    {
        if (activity is null)
            return 0;

        var input = activity.GetTagItem(SpanAttributes.GenAiUsageInputTokens);
        var output = activity.GetTagItem(SpanAttributes.GenAiUsageOutputTokens);

        return (input is long inputL ? inputL : 0) + (output is long outputL ? outputL : 0);
    }
}

/// <summary>
///     Scoped activity wrapper for automatic status and exception handling.
/// </summary>
public readonly struct ScopedActivity : IDisposable
{
    private readonly Activity? _activity;

    public ScopedActivity(Activity? activity) => _activity = activity;

    public Activity? Activity => _activity;

    /// <summary>Marks the activity as successful.</summary>
    public void SetSuccess() => _activity?.SetOk();

    /// <summary>Records an exception and marks the activity as failed.</summary>
    public void SetException(Exception exception) => _activity?.RecordException(exception);

    public void Dispose() => _activity?.Dispose();
}

/// <summary>
///     Extension methods for <see cref="ActivitySource"/>.
/// </summary>
public static class ActivitySourceExtensions
{
    /// <summary>
    ///     Starts an activity with automatic status handling.
    /// </summary>
    public static ScopedActivity StartScopedActivity(
        this ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Internal)
    {
        return new ScopedActivity(source.StartActivity(name, kind));
    }
}

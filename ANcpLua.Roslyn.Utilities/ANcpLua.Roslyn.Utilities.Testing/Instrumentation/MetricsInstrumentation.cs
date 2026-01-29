// =============================================================================
// Metrics Instrumentation - Production utilities for metrics collection
// .NET 10 MeterOptions with OTel schema URL support
// Full OTel instrument type coverage
// =============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ANcpLua.Roslyn.Utilities.Instrumentation;

/// <summary>
///     OpenTelemetry metric instrument kinds.
///     Maps 1:1 to System.Diagnostics.Metrics instrument types.
/// </summary>
/// <remarks>
///     <list type="table">
///         <listheader>
///             <term>Kind</term>
///             <description>Use Case</description>
///         </listheader>
///         <item>
///             <term><see cref="Counter"/></term>
///             <description>Monotonic sum: requests, errors, bytes sent</description>
///         </item>
///         <item>
///             <term><see cref="UpDownCounter"/></term>
///             <description>Non-monotonic sum: active connections, queue depth</description>
///         </item>
///         <item>
///             <term><see cref="Histogram"/></term>
///             <description>Distribution: latency, request size</description>
///         </item>
///         <item>
///             <term><see cref="Gauge"/></term>
///             <description>Current value (pull): CPU, memory, pool size</description>
///         </item>
///         <item>
///             <term><see cref="ObservableCounter"/></term>
///             <description>Monotonic sum (pull): total GC collections</description>
///         </item>
///         <item>
///             <term><see cref="ObservableUpDownCounter"/></term>
///             <description>Non-monotonic sum (pull): thread count</description>
///         </item>
///     </list>
/// </remarks>
public enum MetricKind
{
    /// <summary>Monotonic increment-only counter (push). Use: requests, errors, bytes.</summary>
    Counter,

    /// <summary>Counter that can increase or decrease (push). Use: active connections, in-flight requests.</summary>
    UpDownCounter,

    /// <summary>Distribution of values (push). Use: latency, request size.</summary>
    Histogram,

    /// <summary>Current value snapshot (pull). Use: CPU%, memory, cache size. Maps to ObservableGauge.</summary>
    Gauge,

    /// <summary>Monotonic counter observed on collection (pull). Use: total GC count, total CPU time.</summary>
    ObservableCounter,

    /// <summary>Non-monotonic counter observed on collection (pull). Use: thread count, handle count.</summary>
    ObservableUpDownCounter
}

/// <summary>
///     Factory for creating <see cref="Meter"/> with OTel conventions.
/// </summary>
public static class MeterFactory
{
    /// <summary>
    ///     Creates a Meter with .NET 10 options and OTel schema URL.
    /// </summary>
    public static Meter Create(
        string name,
        string? version = null,
        string? schemaUrl = null)
    {
        return new Meter(new MeterOptions(name)
        {
            Version = version,
            TelemetrySchemaUrl = schemaUrl ?? OTelSchema.Current
        });
    }

    /// <summary>
    ///     Creates a Meter from assembly metadata.
    /// </summary>
    public static Meter CreateFromAssembly<T>(string? schemaUrl = null)
    {
        var assembly = typeof(T).Assembly;
        var name = assembly.GetName().Name ?? typeof(T).Namespace ?? "Unknown";
        var version = assembly.GetName().Version?.ToString();

        return Create(name, version, schemaUrl);
    }
}

/// <summary>
///     OTel metric unit conventions.
/// </summary>
/// <remarks>
///     See: https://opentelemetry.io/docs/specs/semconv/general/metrics/#instrument-units
/// </remarks>
public static class MetricUnits
{
    // Time
    public const string Seconds = "s";
    public const string Milliseconds = "ms";
    public const string Microseconds = "us";
    public const string Nanoseconds = "ns";

    // Size
    public const string Bytes = "By";
    public const string Kilobytes = "KiBy";
    public const string Megabytes = "MiBy";
    public const string Gigabytes = "GiBy";

    // Count (dimensionless with semantic hint)
    public const string Count = "{count}";
    public const string Requests = "{request}";
    public const string Errors = "{error}";
    public const string Items = "{item}";
    public const string Connections = "{connection}";
    public const string Tokens = "{token}";
    public const string Messages = "{message}";
    public const string Operations = "{operation}";
    public const string Threads = "{thread}";
    public const string Handles = "{handle}";

    // Rate / Ratio
    public const string Percent = "%";
    public const string Ratio = "1";
}

/// <summary>
///     Extension methods for <see cref="Meter"/> with convention-based naming.
/// </summary>
public static class MeterExtensions
{
    // =========================================================================
    // Push Instruments (call Add/Record on each event)
    // =========================================================================

    /// <summary>Creates a monotonic counter with standard naming.</summary>
    /// <remarks>Use for: request counts, error counts, bytes transferred.</remarks>
    public static Counter<T> CreateCounter<T>(
        this Meter meter,
        string baseName,
        string unit,
        string description)
        where T : struct =>
        meter.CreateCounter<T>($"{meter.Name}.{baseName}", unit, description);

    /// <summary>Creates a non-monotonic counter with standard naming.</summary>
    /// <remarks>Use for: active connections, queue depth, in-flight requests.</remarks>
    public static UpDownCounter<T> CreateUpDownCounter<T>(
        this Meter meter,
        string baseName,
        string unit,
        string description)
        where T : struct =>
        meter.CreateUpDownCounter<T>($"{meter.Name}.{baseName}", unit, description);

    /// <summary>Creates a histogram with standard naming.</summary>
    /// <remarks>Use for: latency, request size, batch size distributions.</remarks>
    public static Histogram<T> CreateHistogram<T>(
        this Meter meter,
        string baseName,
        string unit,
        string description)
        where T : struct =>
        meter.CreateHistogram<T>($"{meter.Name}.{baseName}", unit, description);

    // =========================================================================
    // Pull Instruments (callback invoked on collection)
    // =========================================================================

    /// <summary>Creates an observable gauge (current value) with standard naming.</summary>
    /// <remarks>Use for: CPU%, memory usage, cache size, pool available.</remarks>
    public static ObservableGauge<T> CreateGauge<T>(
        this Meter meter,
        string baseName,
        Func<T> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableGauge($"{meter.Name}.{baseName}", observeValue, unit, description);

    /// <summary>Creates an observable gauge with tags.</summary>
    public static ObservableGauge<T> CreateGauge<T>(
        this Meter meter,
        string baseName,
        Func<Measurement<T>> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableGauge($"{meter.Name}.{baseName}", observeValue, unit, description);

    /// <summary>Creates an observable gauge with multiple measurements.</summary>
    public static ObservableGauge<T> CreateGauge<T>(
        this Meter meter,
        string baseName,
        Func<IEnumerable<Measurement<T>>> observeValues,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableGauge($"{meter.Name}.{baseName}", observeValues, unit, description);

    /// <summary>Creates an observable counter (monotonic, pull) with standard naming.</summary>
    /// <remarks>Use for: total GC collections, total CPU time, total page faults.</remarks>
    public static ObservableCounter<T> CreateObservableCounter<T>(
        this Meter meter,
        string baseName,
        Func<T> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableCounter($"{meter.Name}.{baseName}", observeValue, unit, description);

    /// <summary>Creates an observable counter with tags.</summary>
    public static ObservableCounter<T> CreateObservableCounter<T>(
        this Meter meter,
        string baseName,
        Func<Measurement<T>> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableCounter($"{meter.Name}.{baseName}", observeValue, unit, description);

    /// <summary>Creates an observable up-down counter (non-monotonic, pull) with standard naming.</summary>
    /// <remarks>Use for: thread count, handle count, connection pool size.</remarks>
    public static ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
        this Meter meter,
        string baseName,
        Func<T> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableUpDownCounter($"{meter.Name}.{baseName}", observeValue, unit, description);

    /// <summary>Creates an observable up-down counter with tags.</summary>
    public static ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(
        this Meter meter,
        string baseName,
        Func<Measurement<T>> observeValue,
        string unit,
        string description)
        where T : struct =>
        meter.CreateObservableUpDownCounter($"{meter.Name}.{baseName}", observeValue, unit, description);
}

/// <summary>
///     Helper for building TagLists with common dimensions.
/// </summary>
public readonly struct MetricTags
{
    private readonly TagList _tags;

    private MetricTags(TagList tags) => _tags = tags;

    /// <summary>Creates an empty tag builder.</summary>
    public static MetricTags Empty => new([]);

    /// <summary>Adds a tag.</summary>
    public MetricTags With(string key, object? value)
    {
        var tags = _tags;
        tags.Add(key, value);
        return new MetricTags(tags);
    }

    /// <summary>Adds HTTP tags.</summary>
    public MetricTags WithHttp(string method, int statusCode, string? route = null)
    {
        var tags = _tags;
        tags.Add(SpanAttributes.HttpRequestMethod, method);
        tags.Add(SpanAttributes.HttpResponseStatusCode, statusCode);
        if (route is not null)
            tags.Add(SpanAttributes.HttpRoute, route);
        return new MetricTags(tags);
    }

    /// <summary>Adds database tags.</summary>
    public MetricTags WithDatabase(string system, string? operation = null)
    {
        var tags = _tags;
        tags.Add(SpanAttributes.DbSystemName, system);
        if (operation is not null)
            tags.Add(SpanAttributes.DbOperationName, operation);
        return new MetricTags(tags);
    }

    /// <summary>Adds GenAI tags.</summary>
    public MetricTags WithGenAi(string provider, string? model = null)
    {
        var tags = _tags;
        tags.Add(SpanAttributes.GenAiProviderName, provider);
        if (model is not null)
            tags.Add(SpanAttributes.GenAiRequestModel, model);
        return new MetricTags(tags);
    }

    /// <summary>Adds error tags.</summary>
    public MetricTags WithError(string errorType)
    {
        var tags = _tags;
        tags.Add(SpanAttributes.ErrorType, errorType);
        return new MetricTags(tags);
    }

    /// <summary>Converts to TagList for use with instruments.</summary>
    public TagList ToTagList() => _tags;

    /// <summary>Implicit conversion to TagList.</summary>
    public static implicit operator TagList(MetricTags tags) => tags._tags;
}

/// <summary>
///     Stopwatch-based duration recorder for histogram metrics.
///     Null-safe: accepts nullable histogram for scenarios where metrics may be disabled.
/// </summary>
public readonly struct DurationRecorder : IDisposable
{
    private readonly Histogram<double>? _histogram;
    private readonly TagList _tags;
    private readonly long _startTimestamp;

    public DurationRecorder(Histogram<double>? histogram, TagList tags = default)
    {
        _histogram = histogram;
        _tags = tags;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>Records the duration in seconds if histogram is not null.</summary>
    public void Dispose()
    {
        if (_histogram is null)
            return;

        var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
        _histogram.Record(elapsed.TotalSeconds, _tags);
    }
}

/// <summary>
///     Scoped counter increment/decrement for UpDownCounter tracking.
///     Null-safe: accepts nullable counter for scenarios where metrics may be disabled.
/// </summary>
/// <remarks>
///     Use for tracking active operations:
///     <code>
///     using (activeRequests.TrackActive(tags)) { await ProcessRequest(); }
///     </code>
/// </remarks>
public readonly struct ActiveTracker : IDisposable
{
    private readonly UpDownCounter<long>? _counter;
    private readonly TagList _tags;

    public ActiveTracker(UpDownCounter<long>? counter, TagList tags = default)
    {
        _counter = counter;
        _tags = tags;
        _counter?.Add(1, _tags);
    }

    /// <summary>Decrements the counter if not null.</summary>
    public void Dispose() => _counter?.Add(-1, _tags);
}

/// <summary>
///     Extension methods for metrics instruments.
///     All methods accept nullable instruments for null-safe usage.
/// </summary>
public static class InstrumentExtensions
{
    /// <summary>
    ///     Creates a scoped duration recorder that records on dispose.
    ///     Safe to call on null histogram - will be a no-op.
    /// </summary>
    public static DurationRecorder RecordDuration(
        this Histogram<double>? histogram,
        TagList tags = default) =>
        new(histogram, tags);

    /// <summary>
    ///     Creates a scoped duration recorder with MetricTags.
    ///     Safe to call on null histogram - will be a no-op.
    /// </summary>
    public static DurationRecorder RecordDuration(
        this Histogram<double>? histogram,
        MetricTags tags) =>
        new(histogram, tags.ToTagList());

    /// <summary>
    ///     Creates a scoped active tracker that increments on enter, decrements on dispose.
    ///     Safe to call on null counter - will be a no-op.
    /// </summary>
    public static ActiveTracker TrackActive(
        this UpDownCounter<long>? counter,
        TagList tags = default) =>
        new(counter, tags);

    /// <summary>
    ///     Creates a scoped active tracker with MetricTags.
    ///     Safe to call on null counter - will be a no-op.
    /// </summary>
    public static ActiveTracker TrackActive(
        this UpDownCounter<long>? counter,
        MetricTags tags) =>
        new(counter, tags.ToTagList());
}

/// <summary>
///     Pre-built observable callbacks for common system metrics.
/// </summary>
public static class SystemMetricCallbacks
{
    /// <summary>Returns GC heap size in bytes.</summary>
    public static long GetGcHeapSize() => GC.GetTotalMemory(forceFullCollection: false);

    /// <summary>Returns Gen0 collection count.</summary>
    public static long GetGen0Collections() => GC.CollectionCount(0);

    /// <summary>Returns Gen1 collection count.</summary>
    public static long GetGen1Collections() => GC.CollectionCount(1);

    /// <summary>Returns Gen2 collection count.</summary>
    public static long GetGen2Collections() => GC.CollectionCount(2);

    /// <summary>Returns thread pool thread count.</summary>
    public static int GetThreadPoolThreadCount()
    {
        ThreadPool.GetAvailableThreads(out var workers, out var io);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIo);
        return maxWorkers - workers + (maxIo - io);
    }

    /// <summary>Returns working set in bytes.</summary>
    public static long GetWorkingSet() => Environment.WorkingSet;
}
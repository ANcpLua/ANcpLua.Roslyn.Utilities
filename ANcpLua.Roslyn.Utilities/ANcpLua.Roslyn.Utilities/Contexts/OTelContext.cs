using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

/// <summary>
///     Provides cached type symbols and utility methods for analyzing OpenTelemetry and
///     <c>System.Diagnostics</c> instrumentation patterns in Roslyn compilations.
///     <para>
///         Caches well-known types from the BCL diagnostics API (<c>System.Diagnostics.Activity</c>,
///         <c>System.Diagnostics.Metrics.Meter</c>, etc.) and optionally from the OpenTelemetry SDK
///         when referenced by the compilation.
///     </para>
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 BCL types (<c>System.Diagnostics.*</c>) are always resolved. OpenTelemetry SDK types
///                 are <c>null</c> when the SDK is not referenced.
///             </description>
///         </item>
///         <item>
///             <description>All methods handle <c>null</c> input gracefully by returning <c>false</c>.</description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="AwaitableContext" />
/// <seealso cref="AspNetContext" />
/// <seealso cref="DisposableContext" />
/// <seealso cref="CollectionContext" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class OTelContext
{
    // ── BCL: System.Diagnostics ──────────────────────────────────────────────

    private INamedTypeSymbol? Activity { get; }
    private INamedTypeSymbol? ActivitySource { get; }
    private INamedTypeSymbol? ActivityKind { get; }
    private INamedTypeSymbol? ActivityStatusCode { get; }
    private INamedTypeSymbol? ActivityEvent { get; }
    private INamedTypeSymbol? ActivityLink { get; }
    private INamedTypeSymbol? ActivityContext { get; }
    private INamedTypeSymbol? ActivityTraceFlags { get; }
    private INamedTypeSymbol? ActivityListener { get; }
    private INamedTypeSymbol? ActivityTagsCollection { get; }
    private INamedTypeSymbol? TagList { get; }
    private INamedTypeSymbol? DiagnosticSource { get; }
    private INamedTypeSymbol? DiagnosticListener { get; }
    private INamedTypeSymbol? DistributedContextPropagator { get; }

    // ── BCL: System.Diagnostics.Metrics ──────────────────────────────────────

    private INamedTypeSymbol? Meter { get; }
    private INamedTypeSymbol? InstrumentOfT { get; }
    private INamedTypeSymbol? CounterOfT { get; }
    private INamedTypeSymbol? HistogramOfT { get; }
    private INamedTypeSymbol? UpDownCounterOfT { get; }
    private INamedTypeSymbol? ObservableInstrumentOfT { get; }
    private INamedTypeSymbol? ObservableCounterOfT { get; }
    private INamedTypeSymbol? ObservableGaugeOfT { get; }
    private INamedTypeSymbol? ObservableUpDownCounterOfT { get; }
    private INamedTypeSymbol? GaugeOfT { get; }
    private INamedTypeSymbol? MeasurementOfT { get; }
    private INamedTypeSymbol? MeterListener { get; }
    private INamedTypeSymbol? IMeterFactory { get; }

    // ── OpenTelemetry SDK (optional) ─────────────────────────────────────────

    private INamedTypeSymbol? TracerProvider { get; }
    private INamedTypeSymbol? MeterProvider { get; }
    private INamedTypeSymbol? TracerProviderBuilder { get; }
    private INamedTypeSymbol? MeterProviderBuilder { get; }
    private INamedTypeSymbol? LoggerProviderBuilder { get; }
    private INamedTypeSymbol? Tracer { get; }
    private INamedTypeSymbol? TelemetrySpan { get; }
    private INamedTypeSymbol? StatusCode { get; }
    private INamedTypeSymbol? Sampler { get; }
    private INamedTypeSymbol? BaseProcessorOfT { get; }
    private INamedTypeSymbol? ResourceBuilder { get; }
    private INamedTypeSymbol? Baggage { get; }

    /// <summary>
    ///     Gets a value indicating whether the OpenTelemetry SDK is referenced by the compilation.
    /// </summary>
    public bool HasOTelSdk { get; }

    /// <summary>
    ///     Well-known metadata names for instrumentation types resolved by <see cref="OTelContext" />.
    /// </summary>
    private static class WellKnownTypes
    {
        // BCL: System.Diagnostics
        internal const string Activity = "System.Diagnostics.Activity";
        internal const string ActivitySource = "System.Diagnostics.ActivitySource";
        internal const string ActivityKind = "System.Diagnostics.ActivityKind";
        internal const string ActivityStatusCode = "System.Diagnostics.ActivityStatusCode";
        internal const string ActivityEvent = "System.Diagnostics.ActivityEvent";
        internal const string ActivityLink = "System.Diagnostics.ActivityLink";
        internal const string ActivityContext = "System.Diagnostics.ActivityContext";
        internal const string ActivityTraceFlags = "System.Diagnostics.ActivityTraceFlags";
        internal const string ActivityListener = "System.Diagnostics.ActivityListener";
        internal const string ActivityTagsCollection = "System.Diagnostics.ActivityTagsCollection";
        internal const string TagList = "System.Diagnostics.TagList";
        internal const string DiagnosticSource = "System.Diagnostics.DiagnosticSource";
        internal const string DiagnosticListener = "System.Diagnostics.DiagnosticListener";
        internal const string DistributedContextPropagator = "System.Diagnostics.DistributedContextPropagator";

        // BCL: System.Diagnostics.Metrics
        internal const string Meter = "System.Diagnostics.Metrics.Meter";
        internal const string InstrumentOfT = "System.Diagnostics.Metrics.Instrument`1";
        internal const string CounterOfT = "System.Diagnostics.Metrics.Counter`1";
        internal const string HistogramOfT = "System.Diagnostics.Metrics.Histogram`1";
        internal const string UpDownCounterOfT = "System.Diagnostics.Metrics.UpDownCounter`1";
        internal const string ObservableInstrumentOfT = "System.Diagnostics.Metrics.ObservableInstrument`1";
        internal const string ObservableCounterOfT = "System.Diagnostics.Metrics.ObservableCounter`1";
        internal const string ObservableGaugeOfT = "System.Diagnostics.Metrics.ObservableGauge`1";
        internal const string ObservableUpDownCounterOfT = "System.Diagnostics.Metrics.ObservableUpDownCounter`1";
        internal const string GaugeOfT = "System.Diagnostics.Metrics.Gauge`1";
        internal const string MeasurementOfT = "System.Diagnostics.Metrics.Measurement`1";
        internal const string MeterListener = "System.Diagnostics.Metrics.MeterListener";
        internal const string IMeterFactory = "System.Diagnostics.Metrics.IMeterFactory";

        // OpenTelemetry SDK
        internal const string TracerProvider = "OpenTelemetry.Trace.TracerProvider";
        internal const string MeterProvider = "OpenTelemetry.Metrics.MeterProvider";
        internal const string TracerProviderBuilder = "OpenTelemetry.Trace.TracerProviderBuilder";
        internal const string MeterProviderBuilder = "OpenTelemetry.Metrics.MeterProviderBuilder";
        internal const string LoggerProviderBuilder = "OpenTelemetry.Logs.LoggerProviderBuilder";
        internal const string Tracer = "OpenTelemetry.Trace.Tracer";
        internal const string TelemetrySpan = "OpenTelemetry.Trace.TelemetrySpan";
        internal const string StatusCode = "OpenTelemetry.Trace.StatusCode";
        internal const string Sampler = "OpenTelemetry.Trace.Sampler";
        internal const string BaseProcessorOfT = "OpenTelemetry.BaseProcessor`1";
        internal const string ResourceBuilder = "OpenTelemetry.Resources.ResourceBuilder";
        internal const string Baggage = "OpenTelemetry.Baggage";
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OTelContext" /> class by resolving
    ///     well-known instrumentation type symbols from the specified compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation" /> from which to resolve type symbols.</param>
    public OTelContext(Compilation compilation)
    {
        // BCL: System.Diagnostics
        Activity = compilation.GetTypeByMetadataName(WellKnownTypes.Activity);
        ActivitySource = compilation.GetTypeByMetadataName(WellKnownTypes.ActivitySource);
        ActivityKind = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityKind);
        ActivityStatusCode = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityStatusCode);
        ActivityEvent = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityEvent);
        ActivityLink = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityLink);
        ActivityContext = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityContext);
        ActivityTraceFlags = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityTraceFlags);
        ActivityListener = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityListener);
        ActivityTagsCollection = compilation.GetTypeByMetadataName(WellKnownTypes.ActivityTagsCollection);
        TagList = compilation.GetTypeByMetadataName(WellKnownTypes.TagList);
        DiagnosticSource = compilation.GetTypeByMetadataName(WellKnownTypes.DiagnosticSource);
        DiagnosticListener = compilation.GetTypeByMetadataName(WellKnownTypes.DiagnosticListener);
        DistributedContextPropagator = compilation.GetTypeByMetadataName(WellKnownTypes.DistributedContextPropagator);

        // BCL: System.Diagnostics.Metrics
        Meter = compilation.GetTypeByMetadataName(WellKnownTypes.Meter);
        InstrumentOfT = compilation.GetTypeByMetadataName(WellKnownTypes.InstrumentOfT);
        CounterOfT = compilation.GetTypeByMetadataName(WellKnownTypes.CounterOfT);
        HistogramOfT = compilation.GetTypeByMetadataName(WellKnownTypes.HistogramOfT);
        UpDownCounterOfT = compilation.GetTypeByMetadataName(WellKnownTypes.UpDownCounterOfT);
        ObservableInstrumentOfT = compilation.GetTypeByMetadataName(WellKnownTypes.ObservableInstrumentOfT);
        ObservableCounterOfT = compilation.GetTypeByMetadataName(WellKnownTypes.ObservableCounterOfT);
        ObservableGaugeOfT = compilation.GetTypeByMetadataName(WellKnownTypes.ObservableGaugeOfT);
        ObservableUpDownCounterOfT = compilation.GetTypeByMetadataName(WellKnownTypes.ObservableUpDownCounterOfT);
        GaugeOfT = compilation.GetTypeByMetadataName(WellKnownTypes.GaugeOfT);
        MeasurementOfT = compilation.GetTypeByMetadataName(WellKnownTypes.MeasurementOfT);
        MeterListener = compilation.GetTypeByMetadataName(WellKnownTypes.MeterListener);
        IMeterFactory = compilation.GetTypeByMetadataName(WellKnownTypes.IMeterFactory);

        // OpenTelemetry SDK (optional — null when not referenced)
        TracerProvider = compilation.GetTypeByMetadataName(WellKnownTypes.TracerProvider);
        MeterProvider = compilation.GetTypeByMetadataName(WellKnownTypes.MeterProvider);
        TracerProviderBuilder = compilation.GetTypeByMetadataName(WellKnownTypes.TracerProviderBuilder);
        MeterProviderBuilder = compilation.GetTypeByMetadataName(WellKnownTypes.MeterProviderBuilder);
        LoggerProviderBuilder = compilation.GetTypeByMetadataName(WellKnownTypes.LoggerProviderBuilder);
        Tracer = compilation.GetTypeByMetadataName(WellKnownTypes.Tracer);
        TelemetrySpan = compilation.GetTypeByMetadataName(WellKnownTypes.TelemetrySpan);
        StatusCode = compilation.GetTypeByMetadataName(WellKnownTypes.StatusCode);
        Sampler = compilation.GetTypeByMetadataName(WellKnownTypes.Sampler);
        BaseProcessorOfT = compilation.GetTypeByMetadataName(WellKnownTypes.BaseProcessorOfT);
        ResourceBuilder = compilation.GetTypeByMetadataName(WellKnownTypes.ResourceBuilder);
        Baggage = compilation.GetTypeByMetadataName(WellKnownTypes.Baggage);

        HasOTelSdk = TracerProvider is not null;
    }

    // ── Activity checks ──────────────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.Activity</c>.
    /// </summary>
    public bool IsActivity(ITypeSymbol? type) => type is not null && Activity is not null && type.IsEqualTo(Activity);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivitySource</c>.
    /// </summary>
    public bool IsActivitySource(ITypeSymbol? type) => type is not null && ActivitySource is not null && type.IsEqualTo(ActivitySource);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityKind</c>.
    /// </summary>
    public bool IsActivityKind(ITypeSymbol? type) => type is not null && ActivityKind is not null && type.IsEqualTo(ActivityKind);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityStatusCode</c>.
    /// </summary>
    public bool IsActivityStatusCode(ITypeSymbol? type) => type is not null && ActivityStatusCode is not null && type.IsEqualTo(ActivityStatusCode);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityEvent</c>.
    /// </summary>
    public bool IsActivityEvent(ITypeSymbol? type) => type is not null && ActivityEvent is not null && type.IsEqualTo(ActivityEvent);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityLink</c>.
    /// </summary>
    public bool IsActivityLink(ITypeSymbol? type) => type is not null && ActivityLink is not null && type.IsEqualTo(ActivityLink);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityContext</c>.
    /// </summary>
    public bool IsActivityContext(ITypeSymbol? type) => type is not null && ActivityContext is not null && type.IsEqualTo(ActivityContext);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityTraceFlags</c>.
    /// </summary>
    public bool IsActivityTraceFlags(ITypeSymbol? type) => type is not null && ActivityTraceFlags is not null && type.IsEqualTo(ActivityTraceFlags);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityListener</c>.
    /// </summary>
    public bool IsActivityListener(ITypeSymbol? type) => type is not null && ActivityListener is not null && type.IsEqualTo(ActivityListener);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.ActivityTagsCollection</c>.
    /// </summary>
    public bool IsActivityTagsCollection(ITypeSymbol? type) => type is not null && ActivityTagsCollection is not null && type.IsEqualTo(ActivityTagsCollection);

    // ── Metrics checks ───────────────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.Metrics.Meter</c>.
    /// </summary>
    public bool IsMeter(ITypeSymbol? type) => type is not null && Meter is not null && type.IsEqualTo(Meter);

    /// <summary>
    ///     Determines whether the specified type is any <c>Instrument&lt;T&gt;</c> derived metric instrument
    ///     (<c>Counter&lt;T&gt;</c>, <c>Histogram&lt;T&gt;</c>, <c>UpDownCounter&lt;T&gt;</c>,
    ///     <c>ObservableCounter&lt;T&gt;</c>, <c>ObservableGauge&lt;T&gt;</c>,
    ///     <c>ObservableUpDownCounter&lt;T&gt;</c>, <c>Gauge&lt;T&gt;</c>).
    /// </summary>
    public bool IsInstrument(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        var original = named.OriginalDefinition;

        return (InstrumentOfT is not null && original.IsOrInheritsFrom(InstrumentOfT)) ||
               MatchesAnyInstrument(original);
    }

    /// <summary>
    ///     Determines whether the specified type is <c>Counter&lt;T&gt;</c>.
    /// </summary>
    public bool IsCounter(ITypeSymbol? type) => IsSpecificInstrument(type, CounterOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>Histogram&lt;T&gt;</c>.
    /// </summary>
    public bool IsHistogram(ITypeSymbol? type) => IsSpecificInstrument(type, HistogramOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>UpDownCounter&lt;T&gt;</c>.
    /// </summary>
    public bool IsUpDownCounter(ITypeSymbol? type) => IsSpecificInstrument(type, UpDownCounterOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>ObservableInstrument&lt;T&gt;</c>.
    /// </summary>
    public bool IsObservableInstrument(ITypeSymbol? type) => IsSpecificInstrument(type, ObservableInstrumentOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>ObservableCounter&lt;T&gt;</c>.
    /// </summary>
    public bool IsObservableCounter(ITypeSymbol? type) => IsSpecificInstrument(type, ObservableCounterOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>ObservableGauge&lt;T&gt;</c>.
    /// </summary>
    public bool IsObservableGauge(ITypeSymbol? type) => IsSpecificInstrument(type, ObservableGaugeOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>ObservableUpDownCounter&lt;T&gt;</c>.
    /// </summary>
    public bool IsObservableUpDownCounter(ITypeSymbol? type) => IsSpecificInstrument(type, ObservableUpDownCounterOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>Gauge&lt;T&gt;</c>.
    /// </summary>
    public bool IsGauge(ITypeSymbol? type) => IsSpecificInstrument(type, GaugeOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>Measurement&lt;T&gt;</c>.
    /// </summary>
    public bool IsMeasurement(ITypeSymbol? type) => IsSpecificInstrument(type, MeasurementOfT);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.Metrics.MeterListener</c>.
    /// </summary>
    public bool IsMeterListener(ITypeSymbol? type) => type is not null && MeterListener is not null && type.IsEqualTo(MeterListener);

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.Metrics.IMeterFactory</c>.
    /// </summary>
    public bool IsIMeterFactory(ITypeSymbol? type) => type is not null && IMeterFactory is not null && type.IsEqualTo(IMeterFactory);

    // ── Diagnostics checks ───────────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is <c>System.Diagnostics.TagList</c>.
    /// </summary>
    public bool IsTagList(ITypeSymbol? type) => type is not null && TagList is not null && type.IsEqualTo(TagList);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>System.Diagnostics.DiagnosticSource</c>.
    /// </summary>
    public bool IsDiagnosticSource(ITypeSymbol? type) => type is not null && DiagnosticSource is not null && type.IsOrInheritsFrom(DiagnosticSource);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>System.Diagnostics.DiagnosticListener</c>.
    /// </summary>
    public bool IsDiagnosticListener(ITypeSymbol? type) => type is not null && DiagnosticListener is not null && type.IsOrInheritsFrom(DiagnosticListener);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>System.Diagnostics.DistributedContextPropagator</c>.
    /// </summary>
    public bool IsDistributedContextPropagator(ITypeSymbol? type) => type is not null && DistributedContextPropagator is not null && type.IsOrInheritsFrom(DistributedContextPropagator);

    // ── OpenTelemetry SDK checks ─────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Trace.TracerProvider</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsTracerProvider(ITypeSymbol? type) => type is not null && TracerProvider is not null && type.IsOrInheritsFrom(TracerProvider);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Metrics.MeterProvider</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsMeterProvider(ITypeSymbol? type) => type is not null && MeterProvider is not null && type.IsOrInheritsFrom(MeterProvider);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Trace.TracerProviderBuilder</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsTracerProviderBuilder(ITypeSymbol? type) => type is not null && TracerProviderBuilder is not null && type.IsOrInheritsFrom(TracerProviderBuilder);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Metrics.MeterProviderBuilder</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsMeterProviderBuilder(ITypeSymbol? type) => type is not null && MeterProviderBuilder is not null && type.IsOrInheritsFrom(MeterProviderBuilder);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Logs.LoggerProviderBuilder</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsLoggerProviderBuilder(ITypeSymbol? type) => type is not null && LoggerProviderBuilder is not null && type.IsOrInheritsFrom(LoggerProviderBuilder);

    /// <summary>
    ///     Determines whether the specified type is <c>OpenTelemetry.Trace.Tracer</c>.
    ///     Returns <c>false</c> if the OpenTelemetry API is not referenced.
    /// </summary>
    public bool IsTracer(ITypeSymbol? type) => type is not null && Tracer is not null && type.IsEqualTo(Tracer);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Trace.TelemetrySpan</c>.
    ///     Returns <c>false</c> if the OpenTelemetry API is not referenced.
    /// </summary>
    public bool IsTelemetrySpan(ITypeSymbol? type) => type is not null && TelemetrySpan is not null && type.IsOrInheritsFrom(TelemetrySpan);

    /// <summary>
    ///     Determines whether the specified type is <c>OpenTelemetry.Trace.StatusCode</c>.
    ///     Returns <c>false</c> if the OpenTelemetry API is not referenced.
    /// </summary>
    public bool IsStatusCode(ITypeSymbol? type) => type is not null && StatusCode is not null && type.IsEqualTo(StatusCode);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Trace.Sampler</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsSampler(ITypeSymbol? type) => type is not null && Sampler is not null && type.IsOrInheritsFrom(Sampler);

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.BaseProcessor&lt;T&gt;</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsBaseProcessor(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named || BaseProcessorOfT is null)
            return false;

        return named.OriginalDefinition.IsOrInheritsFrom(BaseProcessorOfT);
    }

    /// <summary>
    ///     Determines whether the specified type is or inherits from <c>OpenTelemetry.Resources.ResourceBuilder</c>.
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsResourceBuilder(ITypeSymbol? type) => type is not null && ResourceBuilder is not null && type.IsOrInheritsFrom(ResourceBuilder);

    /// <summary>
    ///     Determines whether the specified type is <c>OpenTelemetry.Baggage</c>.
    ///     Returns <c>false</c> if the OpenTelemetry API is not referenced.
    /// </summary>
    public bool IsBaggage(ITypeSymbol? type) => type is not null && Baggage is not null && type.IsEqualTo(Baggage);

    // ── Composite checks ─────────────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is any tracing-related type
    ///     (<c>Activity</c>, <c>ActivitySource</c>, <c>ActivityEvent</c>, <c>ActivityLink</c>, <c>ActivityContext</c>).
    /// </summary>
    public bool IsTracingType(ITypeSymbol? type) =>
        IsActivity(type) || IsActivitySource(type) || IsActivityEvent(type) || IsActivityLink(type) || IsActivityContext(type);

    /// <summary>
    ///     Determines whether the specified type is any metrics-related type
    ///     (<c>Meter</c>, any <c>Instrument&lt;T&gt;</c> derivative, <c>Measurement&lt;T&gt;</c>, or <c>MeterListener</c>).
    /// </summary>
    public bool IsMetricsType(ITypeSymbol? type) =>
        IsMeter(type) || IsInstrument(type) || IsMeasurement(type) || IsMeterListener(type);

    /// <summary>
    ///     Determines whether the specified type is any observability infrastructure type
    ///     (tracing, metrics, diagnostics, or context propagation).
    /// </summary>
    public bool IsObservabilityType(ITypeSymbol? type) =>
        IsTracingType(type) || IsMetricsType(type) || IsDiagnosticSource(type) || IsDiagnosticListener(type) || IsDistributedContextPropagator(type);

    /// <summary>
    ///     Determines whether the specified type is any OpenTelemetry SDK type
    ///     (providers, builders, tracer, span, sampler, processor, resource builder, or baggage).
    ///     Returns <c>false</c> if the OpenTelemetry SDK is not referenced.
    /// </summary>
    public bool IsOTelSdkType(ITypeSymbol? type) =>
        IsTracerProvider(type) || IsMeterProvider(type) ||
        IsTracerProviderBuilder(type) || IsMeterProviderBuilder(type) || IsLoggerProviderBuilder(type) ||
        IsTracer(type) || IsTelemetrySpan(type) || IsStatusCode(type) ||
        IsSampler(type) || IsBaseProcessor(type) || IsResourceBuilder(type) || IsBaggage(type);

    // ── Private helpers ──────────────────────────────────────────────────────

    private static bool IsSpecificInstrument(ITypeSymbol? type, INamedTypeSymbol? expected)
    {
        if (type is not INamedTypeSymbol named || expected is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expected);
    }

    private bool MatchesAnyInstrument(ISymbol original) =>
        (CounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, CounterOfT)) ||
        (HistogramOfT is not null && SymbolEqualityComparer.Default.Equals(original, HistogramOfT)) ||
        (UpDownCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, UpDownCounterOfT)) ||
        (ObservableCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableCounterOfT)) ||
        (ObservableGaugeOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableGaugeOfT)) ||
        (ObservableUpDownCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableUpDownCounterOfT)) ||
        (GaugeOfT is not null && SymbolEqualityComparer.Default.Equals(original, GaugeOfT));
}

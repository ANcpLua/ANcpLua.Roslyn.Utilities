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
    private INamedTypeSymbol? ActivityListener { get; }
    private INamedTypeSymbol? ActivityTagsCollection { get; }
    private INamedTypeSymbol? TagList { get; }
    private INamedTypeSymbol? DiagnosticSource { get; }
    private INamedTypeSymbol? DiagnosticListener { get; }

    // ── BCL: System.Diagnostics.Metrics ──────────────────────────────────────

    private INamedTypeSymbol? Meter { get; }
    private INamedTypeSymbol? MeterOptions { get; }
    private INamedTypeSymbol? InstrumentOfT { get; }
    private INamedTypeSymbol? CounterOfT { get; }
    private INamedTypeSymbol? HistogramOfT { get; }
    private INamedTypeSymbol? UpDownCounterOfT { get; }
    private INamedTypeSymbol? ObservableCounterOfT { get; }
    private INamedTypeSymbol? ObservableGaugeOfT { get; }
    private INamedTypeSymbol? ObservableUpDownCounterOfT { get; }
    private INamedTypeSymbol? GaugeOfT { get; }
    private INamedTypeSymbol? MeterListener { get; }

    // ── OpenTelemetry SDK (optional) ─────────────────────────────────────────

    private INamedTypeSymbol? TracerProvider { get; }
    private INamedTypeSymbol? MeterProvider { get; }
    private INamedTypeSymbol? TracerProviderBuilder { get; }
    private INamedTypeSymbol? MeterProviderBuilder { get; }

    /// <summary>
    ///     Gets a value indicating whether the OpenTelemetry SDK is referenced by the compilation.
    /// </summary>
    public bool HasOTelSdk { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OTelContext" /> class by resolving
    ///     well-known instrumentation type symbols from the specified compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation" /> from which to resolve type symbols.</param>
    public OTelContext(Compilation compilation)
    {
        // BCL: System.Diagnostics
        Activity = compilation.GetTypeByMetadataName("System.Diagnostics.Activity");
        ActivitySource = compilation.GetTypeByMetadataName("System.Diagnostics.ActivitySource");
        ActivityKind = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityKind");
        ActivityStatusCode = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityStatusCode");
        ActivityEvent = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityEvent");
        ActivityLink = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityLink");
        ActivityListener = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityListener");
        ActivityTagsCollection = compilation.GetTypeByMetadataName("System.Diagnostics.ActivityTagsCollection");
        TagList = compilation.GetTypeByMetadataName("System.Diagnostics.TagList");
        DiagnosticSource = compilation.GetTypeByMetadataName("System.Diagnostics.DiagnosticSource");
        DiagnosticListener = compilation.GetTypeByMetadataName("System.Diagnostics.DiagnosticListener");

        // BCL: System.Diagnostics.Metrics
        Meter = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.Meter");
        MeterOptions = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.MeterOptions");
        InstrumentOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.Instrument`1");
        CounterOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.Counter`1");
        HistogramOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.Histogram`1");
        UpDownCounterOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.UpDownCounter`1");
        ObservableCounterOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.ObservableCounter`1");
        ObservableGaugeOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.ObservableGauge`1");
        ObservableUpDownCounterOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.ObservableUpDownCounter`1");
        GaugeOfT = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.Gauge`1");
        MeterListener = compilation.GetTypeByMetadataName("System.Diagnostics.Metrics.MeterListener");

        // OpenTelemetry SDK (optional — null when not referenced)
        TracerProvider = compilation.GetTypeByMetadataName("OpenTelemetry.Trace.TracerProvider");
        MeterProvider = compilation.GetTypeByMetadataName("OpenTelemetry.Metrics.MeterProvider");
        TracerProviderBuilder = compilation.GetTypeByMetadataName("OpenTelemetry.Trace.TracerProviderBuilder");
        MeterProviderBuilder = compilation.GetTypeByMetadataName("OpenTelemetry.Metrics.MeterProviderBuilder");

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
    ///     Determines whether the specified type is <c>System.Diagnostics.Metrics.MeterListener</c>.
    /// </summary>
    public bool IsMeterListener(ITypeSymbol? type) => type is not null && MeterListener is not null && type.IsEqualTo(MeterListener);

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

    // ── Composite checks ─────────────────────────────────────────────────────

    /// <summary>
    ///     Determines whether the specified type is any tracing-related type
    ///     (<c>Activity</c>, <c>ActivitySource</c>, <c>ActivityEvent</c>, <c>ActivityLink</c>).
    /// </summary>
    public bool IsTracingType(ITypeSymbol? type) =>
        IsActivity(type) || IsActivitySource(type) || IsActivityEvent(type) || IsActivityLink(type);

    /// <summary>
    ///     Determines whether the specified type is any metrics-related type
    ///     (<c>Meter</c>, any <c>Instrument&lt;T&gt;</c> derivative, or <c>MeterListener</c>).
    /// </summary>
    public bool IsMetricsType(ITypeSymbol? type) =>
        IsMeter(type) || IsInstrument(type) || IsMeterListener(type);

    /// <summary>
    ///     Determines whether the specified type is any observability infrastructure type
    ///     (tracing, metrics, or diagnostics).
    /// </summary>
    public bool IsObservabilityType(ITypeSymbol? type) =>
        IsTracingType(type) || IsMetricsType(type) || IsDiagnosticSource(type) || IsDiagnosticListener(type);

    // ── Private helpers ──────────────────────────────────────────────────────

    private static bool IsSpecificInstrument(ITypeSymbol? type, INamedTypeSymbol? expected)
    {
        if (type is not INamedTypeSymbol named || expected is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expected);
    }

    private bool MatchesAnyInstrument(INamedTypeSymbol original) =>
        (CounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, CounterOfT)) ||
        (HistogramOfT is not null && SymbolEqualityComparer.Default.Equals(original, HistogramOfT)) ||
        (UpDownCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, UpDownCounterOfT)) ||
        (ObservableCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableCounterOfT)) ||
        (ObservableGaugeOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableGaugeOfT)) ||
        (ObservableUpDownCounterOfT is not null && SymbolEqualityComparer.Default.Equals(original, ObservableUpDownCounterOfT)) ||
        (GaugeOfT is not null && SymbolEqualityComparer.Default.Equals(original, GaugeOfT));
}

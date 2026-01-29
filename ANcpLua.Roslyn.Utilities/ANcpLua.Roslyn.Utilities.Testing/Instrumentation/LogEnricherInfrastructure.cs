// =============================================================================
// Log Enricher Infrastructure - Production utilities for ILogEnricher
// Base classes and helpers for Activity-based log enrichment
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace ANcpLua.Roslyn.Utilities.Instrumentation;

/// <summary>
///     Base class for log enrichers that extract context from <see cref="Activity.Current"/>.
/// </summary>
public abstract class ActivityLogEnricher : ILogEnricher
{
    /// <summary>
    ///     Override to define which Activity tags should be added to logs.
    /// </summary>
    protected abstract IEnumerable<ActivityTagMapping> GetTagMappings();

    /// <summary>
    ///     Override for custom enrichment logic beyond tag mappings.
    /// </summary>
    protected virtual void EnrichFromActivity(IEnrichmentTagCollector collector, Activity activity)
    {
    }

    public void Enrich(IEnrichmentTagCollector collector)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        foreach (var mapping in GetTagMappings())
        {
            var value = activity.GetTagItem(mapping.ActivityTagName);
            if (value is not null)
            {
                collector.Add(mapping.LogTagName, value);
            }
            else if (mapping.DefaultValue is not null)
            {
                collector.Add(mapping.LogTagName, mapping.DefaultValue);
            }
        }

        EnrichFromActivity(collector, activity);
    }
}

/// <summary>
///     Defines a mapping from Activity tag to log tag.
/// </summary>
public readonly record struct ActivityTagMapping(
    string ActivityTagName,
    string LogTagName,
    object? DefaultValue = null)
{
    /// <summary>Creates a mapping where Activity and log tag names are the same.</summary>
    public static ActivityTagMapping Same(string tagName, object? defaultValue = null) =>
        new(tagName, tagName, defaultValue);
}

/// <summary>
///     Pre-built enricher for trace context (trace.id, span.id).
/// </summary>
public sealed class TraceContextEnricher : ILogEnricher
{
    public void Enrich(IEnrichmentTagCollector collector)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        collector.Add(LogTags.TraceId, activity.TraceId.ToString());
        collector.Add(LogTags.SpanId, activity.SpanId.ToString());

        if (activity.ParentSpanId != default)
            collector.Add(LogTags.ParentSpanId, activity.ParentSpanId.ToString());
    }
}

/// <summary>
///     Pre-built enricher for GenAI context from Activity.Current.
/// </summary>
public sealed class GenAiContextEnricher : ActivityLogEnricher
{
    protected override IEnumerable<ActivityTagMapping> GetTagMappings() =>
    [
        new(SpanAttributes.GenAiProviderName, LogTags.GenAiProvider),
        new(SpanAttributes.GenAiRequestModel, LogTags.GenAiModel),
        new(SpanAttributes.GenAiOperationName, LogTags.GenAiOperation),
        new(SpanAttributes.GenAiUsageInputTokens, LogTags.GenAiInputTokens),
        new(SpanAttributes.GenAiUsageOutputTokens, LogTags.GenAiOutputTokens)
    ];
}

/// <summary>
///     Pre-built enricher for service identity.
/// </summary>
public sealed class ServiceIdentityEnricher : ILogEnricher
{
    private readonly string _serviceName;
    private readonly string _serviceVersion;
    private readonly string _instanceId;

    public ServiceIdentityEnricher(string serviceName, string serviceVersion, string? instanceId = null)
    {
        _serviceName = serviceName;
        _serviceVersion = serviceVersion;
        _instanceId = instanceId ?? Environment.MachineName;
    }

    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add(LogTags.ServiceName, _serviceName);
        collector.Add(LogTags.ServiceVersion, _serviceVersion);
        collector.Add(LogTags.ServiceInstanceId, _instanceId);
    }
}

/// <summary>
///     Fluent builder for creating custom activity-based enrichers.
/// </summary>
public sealed class EnricherBuilder
{
    private readonly List<ActivityTagMapping> _mappings = [];
    private Action<IEnrichmentTagCollector, Activity>? _customEnrichment;

    /// <summary>Adds a tag mapping from Activity to log.</summary>
    public EnricherBuilder WithTag(string activityTag, string? logTag = null, object? defaultValue = null)
    {
        _mappings.Add(new ActivityTagMapping(activityTag, logTag ?? activityTag, defaultValue));
        return this;
    }

    /// <summary>Adds a tag mapping where Activity and log names are the same.</summary>
    public EnricherBuilder WithSameTag(string tagName, object? defaultValue = null)
    {
        _mappings.Add(ActivityTagMapping.Same(tagName, defaultValue));
        return this;
    }

    /// <summary>Adds trace context tags (trace.id, span.id).</summary>
    public EnricherBuilder WithTraceContext()
    {
        _customEnrichment += (collector, activity) =>
        {
            collector.Add(LogTags.TraceId, activity.TraceId.ToString());
            collector.Add(LogTags.SpanId, activity.SpanId.ToString());
        };
        return this;
    }

    /// <summary>Adds all GenAI tags.</summary>
    public EnricherBuilder WithGenAiTags()
    {
        _mappings.Add(new(SpanAttributes.GenAiProviderName, LogTags.GenAiProvider));
        _mappings.Add(new(SpanAttributes.GenAiRequestModel, LogTags.GenAiModel));
        _mappings.Add(new(SpanAttributes.GenAiOperationName, LogTags.GenAiOperation));
        _mappings.Add(new(SpanAttributes.GenAiUsageInputTokens, LogTags.GenAiInputTokens));
        _mappings.Add(new(SpanAttributes.GenAiUsageOutputTokens, LogTags.GenAiOutputTokens));
        return this;
    }

    /// <summary>Adds custom enrichment logic.</summary>
    public EnricherBuilder WithCustom(Action<IEnrichmentTagCollector, Activity> enrichment)
    {
        _customEnrichment += enrichment;
        return this;
    }

    /// <summary>Builds the enricher.</summary>
    public ILogEnricher Build() => new DelegatingEnricher([.. _mappings], _customEnrichment);

    private sealed class DelegatingEnricher(
        IEnumerable<ActivityTagMapping> mappings,
        Action<IEnrichmentTagCollector, Activity>? customEnrichment) : ActivityLogEnricher
    {
        protected override IEnumerable<ActivityTagMapping> GetTagMappings() => mappings;

        protected override void EnrichFromActivity(IEnrichmentTagCollector collector, Activity activity)
        {
            customEnrichment?.Invoke(collector, activity);
        }
    }
}

/// <summary>
///     Static factory for creating enrichers.
/// </summary>
public static class Enrichers
{
    /// <summary>Creates a new enricher builder.</summary>
    public static EnricherBuilder Builder() => new();

    /// <summary>Creates a trace context enricher.</summary>
    public static ILogEnricher TraceContext() => new TraceContextEnricher();

    /// <summary>Creates a GenAI context enricher.</summary>
    public static ILogEnricher GenAiContext() => new GenAiContextEnricher();

    /// <summary>Creates a service identity enricher.</summary>
    public static ILogEnricher ServiceIdentity(string name, string version, string? instanceId = null) =>
        new ServiceIdentityEnricher(name, version, instanceId);
}
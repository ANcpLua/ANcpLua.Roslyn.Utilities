// Licensed to the .NET Foundation under one or more agreements.

using System.Text;
using System.Text.Json;

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting;

/// <summary>
/// Parses Server-Sent Events (SSE) response content into structured JSON elements.
/// </summary>
public static class SseResponseParser
{
    private const string DataPrefix = "data:";
    private const string TypeProperty = "type";

    /// <summary>
    /// Parses raw SSE response text into a list of <see cref="JsonElement"/> events.
    /// Each "data:" line is parsed as JSON. Multi-line data payloads are concatenated.
    /// </summary>
    public static IReadOnlyList<JsonElement> Parse(string responseContent)
    {
        List<JsonElement> events = [];
        using StringReader reader = new(responseContent);
        StringBuilder dataBuilder = new();
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                string payload = line.Length > DataPrefix.Length && line[DataPrefix.Length] == ' '
                    ? line[(DataPrefix.Length + 1)..]
                    : line[DataPrefix.Length..];
                dataBuilder.Append(payload);
            }
            else if (line.Length == 0 && dataBuilder.Length > 0)
            {
                using JsonDocument document = JsonDocument.Parse(dataBuilder.ToString());
                events.Add(document.RootElement.Clone());
                dataBuilder.Clear();
            }
        }

        if (dataBuilder.Length > 0)
        {
            using JsonDocument document = JsonDocument.Parse(dataBuilder.ToString());
            events.Add(document.RootElement.Clone());
        }

        return events;
    }

    /// <summary>
    /// Extracts all events of a specific type from parsed SSE events.
    /// </summary>
    public static IReadOnlyList<JsonElement> FilterByType(IReadOnlyList<JsonElement> events, string eventType) =>
        events.Where(e =>
            e.TryGetProperty(TypeProperty, out JsonElement typeProp) &&
            string.Equals(typeProp.GetString(), eventType, StringComparison.Ordinal)).ToList();
}

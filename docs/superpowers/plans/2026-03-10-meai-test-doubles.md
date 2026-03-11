# MEAI Test Doubles Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three test doubles (FakeChatClient, ActivityCollector, FakeHttpMessageHandler) to ANcpLua.Roslyn.Utilities.Testing for testing MEAI/agent code with OTel instrumentation.

**Architecture:** Three new files in the existing `Instrumentation/` folder. Uses existing `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` dependency (already in Directory.Packages.props) which transitively provides `Microsoft.Extensions.AI` (`IChatClient`, `ChatMessage`, etc.) and `Microsoft.Agents.AI` (`AIAgent`, `AgentResponse`, etc.). All follow existing Testing package patterns: fluent API, xUnit assertions, thread-safe, disposable.

**Tech Stack:** C# 14, .NET 10 LTS (net10.0), Microsoft.Agents.AI (via existing AGUI package), System.Diagnostics, xUnit

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `src/ANcpLua.Roslyn.Utilities.Testing/ANcpLua.Roslyn.Utilities.Testing.csproj` | Add existing AGUI PackageReference |
| Create | `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/FakeChatClient.cs` | IChatClient test double |
| Create | `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/ActivityCollector.cs` | ActivityListener-based span collector + assertions |
| Create | `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/FakeHttpMessageHandler.cs` | HttpMessageHandler test double |

---

## Chunk 1: Dependencies + FakeChatClient

### Task 1: Add Microsoft.Agents.AI package reference to Testing csproj

**Files:**
- Modify: `src/ANcpLua.Roslyn.Utilities.Testing/ANcpLua.Roslyn.Utilities.Testing.csproj`

The `PackageVersion` for `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` already exists in `Directory.Packages.props` at version `$(MicrosoftAgentsAIHostingAGUIAspNetCoreVersion)`. We just need the `PackageReference`.

- [ ] **Step 1: Add PackageReference to Testing csproj**

Add a new ItemGroup after "Web Testing":
```xml
<ItemGroup Label="Agent Testing">
    <PackageReference Include="Microsoft.Agents.AI.Hosting.AGUI.AspNetCore"/>
</ItemGroup>
```

This transitively provides:
- `Microsoft.Agents.AI` — `AIAgent`, `AgentResponse`, `ChatClientAgent`
- `Microsoft.Agents.AI.Abstractions` — agent abstractions
- `Microsoft.Extensions.AI` — `IChatClient`, `ChatMessage`, `ChatResponse`, `ChatOptions`
- `Microsoft.Agents.AI.Workflows` — workflow types

- [ ] **Step 2: Verify build**

Run: `dotnet build src/ANcpLua.Roslyn.Utilities.Testing -c Release`
Expected: Build succeeds, MEAI + Agents types available.

- [ ] **Step 3: Commit**

```
feat: add Microsoft.Agents.AI package reference to Testing
```

### Task 2: Implement FakeChatClient

**Files:**
- Create: `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/FakeChatClient.cs`

- [ ] **Step 1: Create FakeChatClient**

```csharp
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Configurable <see cref="IChatClient"/> test double that returns canned responses
///     and records all calls for assertion. Supports both non-streaming and streaming paths.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var client = new FakeChatClient()
///         .WithResponse("Hello!")
///         .WithStreamingResponse("Hel", "lo!");
///
///     var response = await client.GetResponseAsync([new(ChatRole.User, "Hi")]);
///     Assert.Equal("Hello!", response.Text);
///     Assert.Single(client.Calls);
///     </code>
/// </remarks>
public sealed class FakeChatClient : IChatClient
{
    private readonly Queue<object> _responses = new(); // ChatResponse | ChatResponseUpdate[] | Exception
    private readonly Lock _lock = new();

    /// <summary>
    ///     All calls made to <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>,
    ///     in order. Each entry records the messages and options passed by the caller.
    /// </summary>
    public List<ChatClientCall> Calls { get; } = [];

    /// <summary>
    ///     Metadata returned by <see cref="GetService{TService}"/> when <c>TService</c>
    ///     is <see cref="ChatClientMetadata"/>. Defaults to provider "fake", model "fake-model".
    /// </summary>
    public ChatClientMetadata Metadata { get; set; } = new("fake", null, "fake-model");

    // ── Builder methods ──────────────────────────────────────────────────────

    /// <summary>
    ///     Enqueues a canned non-streaming response with the given text.
    /// </summary>
    public FakeChatClient WithResponse(
        string text,
        ChatFinishReason? finishReason = null,
        UsageDetails? usage = null,
        string? modelId = null)
    {
        var message = new ChatMessage(ChatRole.Assistant, text);
        var response = new ChatResponse(message)
        {
            FinishReason = finishReason ?? ChatFinishReason.Stop,
            ModelId = modelId,
            Usage = usage
        };

        using (_lock.EnterScope())
            _responses.Enqueue(response);

        return this;
    }

    /// <summary>
    ///     Enqueues a canned streaming response that yields one update per chunk.
    /// </summary>
    public FakeChatClient WithStreamingResponse(params string[] chunks)
    {
        var updates = chunks.Select(static chunk => new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Text = chunk
        }).ToArray();

        using (_lock.EnterScope())
            _responses.Enqueue(updates);

        return this;
    }

    /// <summary>
    ///     Enqueues an exception to be thrown on the next call (streaming or non-streaming).
    /// </summary>
    public FakeChatClient WithError(Exception exception)
    {
        using (_lock.EnterScope())
            _responses.Enqueue(exception);

        return this;
    }

    /// <summary>
    ///     Enqueues a typed exception to be thrown on the next call.
    /// </summary>
    public FakeChatClient WithError<TException>() where TException : Exception, new()
    {
        return WithError(new TException());
    }

    // ── IChatClient ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        RecordCall(messages, options);

        var next = DequeueNext();

        return next switch
        {
            Exception ex => Task.FromException<ChatResponse>(ex),
            ChatResponse response => Task.FromResult(response),
            ChatResponseUpdate[] updates => Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, string.Concat(updates.Select(static u => u.Text))))
            {
                FinishReason = ChatFinishReason.Stop
            }),
            _ => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)))
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RecordCall(messages, options);

        var next = DequeueNext();

        switch (next)
        {
            case Exception ex:
                throw ex;

            case ChatResponseUpdate[] updates:
                foreach (var update in updates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return update;
                }
                break;

            case ChatResponse response:
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Text = response.Text
                };
                break;

            default:
                yield break;
        }

        // Suppress compiler warning — await is required for async IAsyncEnumerable
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public TService? GetService<TService>(object? key = null) where TService : class
    {
        if (typeof(TService) == typeof(ChatClientMetadata))
            return Metadata as TService;

        return this as TService;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to release.
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private void RecordCall(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        Calls.Add(new ChatClientCall(messages.ToList(), options));
    }

    private object? DequeueNext()
    {
        using (_lock.EnterScope())
            return _responses.Count > 0 ? _responses.Dequeue() : null;
    }
}

/// <summary>
///     Records a single call to <see cref="FakeChatClient"/>.
/// </summary>
/// <param name="Messages">The chat messages passed to the call.</param>
/// <param name="Options">The chat options passed to the call, if any.</param>
public sealed record ChatClientCall(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/ANcpLua.Roslyn.Utilities.Testing -c Release`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```
feat: add FakeChatClient test double for MEAI
```

---

## Chunk 2: ActivityCollector

### Task 3: Implement ActivityCollector

**Files:**
- Create: `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/ActivityCollector.cs`

- [ ] **Step 1: Create ActivityCollector**

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Captures completed <see cref="Activity"/> instances from named
///     <see cref="ActivitySource"/>s for test assertions.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var collector = new ActivityCollector("Qyl.Agents");
///
///     // ... exercise code that creates activities ...
///
///     var span = collector.Single("chat gpt-4");
///     span.AssertTag("gen_ai.provider.name", "openai");
///     span.AssertStatus(ActivityStatusCode.Ok);
///     </code>
/// </remarks>
public sealed class ActivityCollector : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _activities = [];
    private readonly HashSet<string>? _sourceFilter;

    /// <summary>
    ///     Creates a collector that captures activities from the specified source names.
    ///     If no names are provided, captures from all sources.
    /// </summary>
    /// <param name="sourceNames">
    ///     Optional source names to filter on. Pass none to capture all sources.
    /// </param>
    public ActivityCollector(params string[] sourceNames)
    {
        _sourceFilter = sourceNames.Length > 0 ? [..sourceNames] : null;

        _listener = new ActivityListener
        {
            ShouldListenTo = ShouldListen,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>All captured activities.</summary>
    public IReadOnlyList<Activity> Activities => [.. _activities];

    /// <summary>Returns the single activity matching the operation name prefix.</summary>
    /// <param name="operationNamePrefix">Prefix to match against <see cref="Activity.OperationName"/>.</param>
    public Activity Single(string operationNamePrefix)
    {
        var matches = _activities
            .Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))
            .ToList();

        Assert.True(matches.Count is 1,
            $"Expected exactly 1 activity matching '{operationNamePrefix}', found {matches.Count}. " +
            $"All activities: [{string.Join(", ", _activities.Select(static a => a.OperationName))}]");

        return matches[0];
    }

    /// <summary>Returns all activities matching the operation name prefix.</summary>
    public IReadOnlyList<Activity> Where(string operationNamePrefix)
    {
        return _activities
            .Where(a => a.OperationName.StartsWith(operationNamePrefix, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Asserts that no activities were captured.</summary>
    public ActivityCollector ShouldBeEmpty()
    {
        Assert.True(_activities.IsEmpty,
            $"Expected no activities, found {_activities.Count}: " +
            $"[{string.Join(", ", _activities.Select(static a => a.OperationName))}]");
        return this;
    }

    /// <summary>Asserts that at least <paramref name="expected"/> activities were captured.</summary>
    public ActivityCollector ShouldHaveCount(int expected)
    {
        Assert.True(_activities.Count >= expected,
            $"Expected at least {expected} activities, found {_activities.Count}.");
        return this;
    }

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();

    private bool ShouldListen(ActivitySource source) =>
        _sourceFilter is null || _sourceFilter.Contains(source.Name);
}

/// <summary>
///     Fluent assertion extensions for <see cref="Activity"/>.
/// </summary>
public static class ActivityAssert
{
    /// <summary>Asserts that the activity has a tag with the expected value.</summary>
    public static Activity AssertTag(this Activity activity, string key, object? expected)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(Equals(actual, expected),
            $"Activity '{activity.OperationName}': expected tag '{key}' = '{expected}', got '{actual}'.");
        return activity;
    }

    /// <summary>Asserts that the activity has a tag (any value).</summary>
    public static Activity AssertHasTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(actual is not null,
            $"Activity '{activity.OperationName}': expected tag '{key}' to be present, but it was not.");
        return activity;
    }

    /// <summary>Asserts that the activity does NOT have a tag.</summary>
    public static Activity AssertNoTag(this Activity activity, string key)
    {
        var actual = activity.GetTagItem(key);
        Assert.True(actual is null,
            $"Activity '{activity.OperationName}': expected tag '{key}' to be absent, but found '{actual}'.");
        return activity;
    }

    /// <summary>Asserts the activity status code.</summary>
    public static Activity AssertStatus(this Activity activity, ActivityStatusCode expected)
    {
        Assert.True(activity.Status == expected,
            $"Activity '{activity.OperationName}': expected status '{expected}', got '{activity.Status}'.");
        return activity;
    }

    /// <summary>Asserts the activity has an event with the given name.</summary>
    public static Activity AssertHasEvent(this Activity activity, string eventName)
    {
        Assert.True(activity.Events.Any(e => e.Name == eventName),
            $"Activity '{activity.OperationName}': expected event '{eventName}', " +
            $"found: [{string.Join(", ", activity.Events.Select(static e => e.Name))}].");
        return activity;
    }

    /// <summary>Asserts the activity kind.</summary>
    public static Activity AssertKind(this Activity activity, ActivityKind expected)
    {
        Assert.True(activity.Kind == expected,
            $"Activity '{activity.OperationName}': expected kind '{expected}', got '{activity.Kind}'.");
        return activity;
    }

    /// <summary>Asserts the activity duration is within a range.</summary>
    public static Activity AssertDuration(this Activity activity, TimeSpan min, TimeSpan max)
    {
        Assert.True(activity.Duration >= min && activity.Duration <= max,
            $"Activity '{activity.OperationName}': duration {activity.Duration} not in [{min}, {max}].");
        return activity;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/ANcpLua.Roslyn.Utilities.Testing -c Release`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```
feat: add ActivityCollector test harness for OTel span assertions
```

---

## Chunk 3: FakeHttpMessageHandler

### Task 4: Implement FakeHttpMessageHandler

**Files:**
- Create: `src/ANcpLua.Roslyn.Utilities.Testing/Instrumentation/FakeHttpMessageHandler.cs`

- [ ] **Step 1: Create FakeHttpMessageHandler**

```csharp
using System.Net;

namespace ANcpLua.Roslyn.Utilities.Testing.Instrumentation;

/// <summary>
///     Configurable <see cref="HttpMessageHandler"/> that returns canned responses
///     and records all requests for assertion.
/// </summary>
/// <remarks>
///     <para>Usage:</para>
///     <code>
///     using var handler = new FakeHttpMessageHandler()
///         .WithResponse("*/oauth/access_token", HttpStatusCode.OK, """{"access_token":"tok"}""")
///         .WithResponse("*/user", HttpStatusCode.OK, """{"login":"testuser"}""");
///
///     using var httpClient = new HttpClient(handler);
///     // ... use httpClient in code under test ...
///
///     Assert.Equal(2, handler.Requests.Count);
///     </code>
/// </remarks>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<ResponseRule> _rules = [];
    private readonly Lock _lock = new();

    /// <summary>All requests sent through this handler, in order.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>
    ///     Fallback status code when no rule matches. Defaults to <see cref="HttpStatusCode.NotFound"/>.
    /// </summary>
    public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.NotFound;

    // ── Builder methods ──────────────────────────────────────────────────────

    /// <summary>
    ///     Adds a rule: requests whose URL contains <paramref name="urlPattern"/> get the
    ///     specified response. Rules are evaluated in order; first match wins.
    /// </summary>
    /// <param name="urlPattern">Substring to match in the request URL. Use "*" prefix for contains-match.</param>
    /// <param name="statusCode">HTTP status code to return.</param>
    /// <param name="body">Response body (JSON or other content).</param>
    /// <param name="contentType">Content-Type header. Defaults to "application/json".</param>
    public FakeHttpMessageHandler WithResponse(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        string contentType = "application/json")
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, body, contentType, Times: null));
        return this;
    }

    /// <summary>
    ///     Adds a rule that returns an error response (empty body) for matching URLs.
    /// </summary>
    public FakeHttpMessageHandler WithError(string urlPattern, HttpStatusCode statusCode)
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, string.Empty, "text/plain", Times: null));
        return this;
    }

    /// <summary>
    ///     Adds a rule that matches only N times, then falls through to the next rule.
    /// </summary>
    public FakeHttpMessageHandler WithResponse(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        int times,
        string contentType = "application/json")
    {
        _rules.Add(new ResponseRule(urlPattern.TrimStart('*'), statusCode, body, contentType, times));
        return this;
    }

    // ── HttpMessageHandler ───────────────────────────────────────────────────

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var url = request.RequestUri?.ToString() ?? string.Empty;

        using (_lock.EnterScope())
        {
            Requests.Add(new RecordedRequest(request.Method, url, request.Headers.Authorization?.ToString()));

            foreach (var rule in _rules)
            {
                if (!url.Contains(rule.UrlPattern, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (rule.Times.HasValue)
                {
                    if (rule.RemainingUses <= 0)
                        continue;
                    rule.RemainingUses--;
                }

                return Task.FromResult(new HttpResponseMessage(rule.StatusCode)
                {
                    Content = new StringContent(rule.Body, Encoding.UTF8, rule.ContentType)
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(DefaultStatusCode));
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private sealed class ResponseRule(
        string urlPattern,
        HttpStatusCode statusCode,
        string body,
        string contentType,
        int? Times)
    {
        public string UrlPattern => urlPattern;
        public HttpStatusCode StatusCode => statusCode;
        public string Body => body;
        public string ContentType => contentType;
        public int? Times => Times;
        public int RemainingUses { get; set; } = Times ?? 0;
    }
}

/// <summary>Records a single HTTP request sent through <see cref="FakeHttpMessageHandler"/>.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Url">The full request URL.</param>
/// <param name="Authorization">The Authorization header value, if present.</param>
public sealed record RecordedRequest(HttpMethod Method, string Url, string? Authorization);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/ANcpLua.Roslyn.Utilities.Testing -c Release`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```
feat: add FakeHttpMessageHandler test double for HTTP testing
```

---

## Chunk 4: Final verification

### Task 5: Full build + pack verification

- [ ] **Step 1: Full release build**

Run: `dotnet build -c Release`
Expected: All projects build clean.

- [ ] **Step 2: Pack**

Run: `dotnet pack -c Release`
Expected: Package created successfully with new types included.

- [ ] **Step 3: Final commit (if any fixups needed)**

```
chore: finalize MEAI test doubles integration
```

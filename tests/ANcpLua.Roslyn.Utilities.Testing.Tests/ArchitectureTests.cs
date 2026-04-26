// NOTE: The two uploaded files (toolu_01H1ZvgEia2Ca5u1GQ5L2YEX.txt and
// toolu_013YPZYSrN5QNhFb9cGBovyo.txt) are byte-identical (MD5 c233fff3a57b5339e41211a714aa52f2).
// Merging them is a no-op; below is the single deduped copy of their JSON-wrapped shell response.
// Use the test harness in AgentChatClientFactoryTests.cs for new architecture tests.


// ===================== MERGED ORPHAN TEST CODE (deduped) =====================
Shell Response: // ============================================================================
// ORPHAN TEST CODE — REVIEW FILE
// Branch: chore/purge-orphan-tests
// Reason purged: not using ANcpLua testing helpers
// Status: NOT compilable — for review only
//
// These are test scenarios worth re-implementing with proper framework helpers.
// Each section is from a different test file. Keep what you want to rewrite.
// ============================================================================


// ── ArchitectureTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/ArchitectureTests.cs

namespace Qyl.Collector.Tests
{
    using System.Reflection;
    using NetArchTest.Rules;
    using Qyl.Contracts.Primitives;
    using Xunit;
    using TestResult = NetArchTest.Rules.TestResult;

    /// <summary>
    ///     Enforces project dependency boundaries across the qyl platform.
    ///     v2 architecture: collector may depend on M.E.AI abstractions, but not
    ///     MAF runtime packages, GitHub Copilot SDK, or concrete provider SDKs.
    /// </summary>
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Server_Should_Not_Depend_On_Agent_Framework()
        {
            var result = Types.InAssembly(typeof(Program).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.Agents.AI",
                    "Microsoft.Agents.AI.Hosting",
                    "Microsoft.Agents.AI.Hosting.AGUI",
                    "GitHub.Copilot.SDK")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(FormatFailure(result));
        }

        [Fact]
        public void Contracts_Should_Not_Depend_On_Any_Project()
        {
            var result = Types.InAssembly(typeof(TimeConversions).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Qyl.Collector",
                    "Qyl.Loom",
                    "Qyl.Instrumentation",
                    "Qyl.Mcp")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(FormatFailure(result));
        }

        [Fact]
        public void Mcp_Should_Not_Reference_Collector_Assembly()
        {
            var mcpAssembly = LoadMcpAssembly();

            if (mcpAssembly is null)
            {
                return; // MCP not loaded in this test run — skip
            }

            var result = Types.InAssembly(mcpAssembly)
                .ShouldNot()
                .HaveDependencyOn("Qyl.Collector")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(FormatFailure(result));
        }

        private static string FormatFailure(TestResult result)
        {
            var violators = result.FailingTypeNames ?? [];
            return $"Architecture violation: {string.Join(", ", violators)}";
        }

        private static Assembly? LoadMcpAssembly()
        {
            var repoRoot = FindRepoRoot();
            if (repoRoot is null)
                return null;

            var assemblyPath = Path.Combine(
                repoRoot.FullName,
                "src",
                "qyl.mcp",
                "bin",
                "Debug",
                "net10.0",
                "qyl.mcp.dll");

            return File.Exists(assemblyPath)
                ? Assembly.LoadFrom(assemblyPath)
                : null;
        }

        private static DirectoryInfo? FindRepoRoot()
        {
            for (DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "qyl.slnx")))
                    return current;
            }

            return null;
        }
    }

// ── PatternEngineTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/Intelligence/PatternEngineTests.cs
}

namespace Qyl.Collector.Tests.Qyl.Collector.Tests.Intelligence.Qyl.Collector.Tests.Query.Qyl.Collector.Tests.Realtime.Qyl.Collector.Tests.Storage.Qyl.Mcp.Tests
{
    internal class RecordingHttpMessageHandler
    {
    }
}

namespace Qyl.Collector.Tests.Intelligence;

using Collector.Intelligence;
using Qyl.Contracts.Intelligence;
using Xunit;

public sealed class PatternEngineTests
{
    private static PatternEngine CreateEngine() =>
        new(DiagnosticPatterns.All, CausalRules.All, InvestigationStrategies.All);

    private static Signal Observed(string attribute, string? value = null) =>
        new() { Attribute = attribute, Operator = SignalOperator.Eq, Value = value };

    // ==========================================================================
    // Seed pattern registry integrity
    // ==========================================================================

    [Fact]
    public void SeedPatterns_AllHaveUniqueIds()
    {
        var ids = DiagnosticPatterns.All.Select(static p => p.Id).ToList();
        ids.Distinct(StringComparer.Ordinal).Count().Should().Be(ids.Count);
    }

    [Fact]
    public void SeedRules_All_Contains6Rules() => CausalRules.All.Count.Should().Be(6);

    [Fact]
    public void SeedRules_ReferenceExistingPatterns()
    {
        var patternIds = DiagnosticPatterns.All.Select(static p => p.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var rule in CausalRules.All)
        {
            patternIds.Should().Contain(rule.CausePattern);
            patternIds.Should().Contain(rule.EffectPattern);
        }
    }

    [Fact]
    public void SeedStrategies_AllHaveSteps()
    {
        foreach (var strategy in InvestigationStrategies.All)
        {
            strategy.Steps.Should().NotBeEmpty();
        }
    }

    // ==========================================================================
    // Pattern matching: positive cases — 10 seed patterns
    // ==========================================================================

    [Fact]
    public void Evaluate_GenAiRateLimit_MatchesWhenAllSignalsPresent()
    {
        var engine = CreateEngine();
        var signals = new List<Signal>
        {
            Observed("status_code", "2"),
            Observed("gen_ai_provider_name", "openai"),
            Observed("error_type", "rate_limit_exceeded")
        };

        var matches = engine.Evaluate(signals);

        var match = matches.Should().ContainSingle(static m => m.Pattern.Id == "genai_rate_limit").Which;
        match.Score.Should().BeApproximately(0.9, 1e-5);
    }

    [Fact]
    public void Evaluate_GenAiTokenExhaustion_MatchesOnStopReasonLength()
    {
        var engine = CreateEngine();
        var signals = new List<Signal> { Observed("gen_ai_stop_reason", "length") };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "genai_token_exhaustion");
    }

    [Fact]
    public void Evaluate_GenAiContentFilter_MatchesOnStopReasonContainingContentFilter()
    {
        var engine = CreateEngine();
        var signals = new List<Signal> { Observed("gen_ai_stop_reason", "content_filter_triggered") };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "genai_content_filter");
    }

    [Fact]
    public void Evaluate_DbTimeout_MatchesOnTimeoutExceptionWithDbAndDuration()
    {
        var engine = CreateEngine();
        var signals = new List<Signal>
        {
            Observed("exception_type", "TimeoutException"),
            Observed("db.system.name", "postgresql"),
            Observed("duration_ns", "3000000000")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "db_timeout");
    }

    [Fact]
    public void Evaluate_DbNPlusOne_MatchesOnHighSpanCountUnderParent()
    {
        var engine = CreateEngine();
        var signals = new List<Signal>
        {
            Observed("db.system.name", "postgresql"),
            Observed("parent_span_id", "abc123"),
            Observed("span_count_under_parent", "25")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "db_n_plus_one");
    }

    [Fact]
    public void Evaluate_Http5xxCluster_MatchesWithResolvedThresholds()
    {
        // Seed pattern uses symbolic "baseline*3" — test with pre-resolved numeric threshold
        var resolvedPattern = new DiagnosticPattern
        {
            Id = "http_5xx_cluster",
            Category = PatternCategory.Error,
            Signals =
            [
                new Signal { Attribute = "http.response.status_code", Operator = SignalOperator.Gte, Value = "500" },
                new Signal { Attribute = "occurrence_rate", Operator = SignalOperator.Gt, Value = "9" }
            ],
            Hypothesis = "Server error spike.",
            Confidence = 0.75
        };
        var engine = new PatternEngine([resolvedPattern], CausalRules.All, InvestigationStrategies.All);
        var signals = new List<Signal>
        {
            Observed("http.response.status_code", "503"), Observed("occurrence_rate", "15")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "http_5xx_cluster");
    }

    [Fact]
    public void Evaluate_DeploymentRegression_MatchesWithResolvedThresholds()
    {
        // Seed pattern uses symbolic "last_deployment_time" — test with resolved numeric timestamp
        var resolvedPattern = new DiagnosticPattern
        {
            Id = "deployment_regression",
            Category = PatternCategory.Error,
            Signals =
            [
                new Signal { Attribute = "error_type", Operator = SignalOperator.Exists },
                new Signal { Attribute = "first_seen_at", Operator = SignalOperator.Gt, Value = "1711100000" }
            ],
            Hypothesis = "New error class after deployment.",
            Confidence = 0.80
        };
        var engine = new PatternEngine([resolvedPattern], CausalRules.All, InvestigationStrategies.All);
        var signals = new List<Signal>
        {
            Observed("error_type", "NullReferenceException"), Observed("first_seen_at", "1711108800")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "deployment_regression");
    }

    [Fact]
    public void Evaluate_CascadingTimeout_MatchesOnTimeoutWithDownstreamError()
    {
        var engine = CreateEngine();
        var signals = new List<Signal>
        {
            Observed("exception_type", "TaskCanceledException: The request was canceled due to Timeout"),
            Observed("downstream_service_error", "true")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "cascading_timeout");
    }

    [Fact]
    public void Evaluate_MemoryPressureLatency_MatchesWithResolvedThresholds()
    {
        // Seed pattern uses symbolic "p99_baseline" — test with resolved numeric value
        var resolvedPattern = new DiagnosticPattern
        {
            Id = "memory_pressure_latency",
            Category = PatternCategory.Latency,
            Signals =
            [
                new Signal
                {
                    Attribute = "process.runtime.dotnet.gc.duration", Operator = SignalOperator.Gt, Value = "100"
                },
                new Signal { Attribute = "avg_latency", Operator = SignalOperator.Gt, Value = "200" }
            ],
            Hypothesis = "GC pressure causing latency.",
            Confidence = 0.65
        };
        var engine = new PatternEngine([resolvedPattern], CausalRules.All, InvestigationStrategies.All);
        var signals = new List<Signal>
        {
            Observed("process.runtime.dotnet.gc.duration", "250"), Observed("avg_latency", "500")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "memory_pressure_latency");
    }

    [Fact]
    public void Evaluate_CostSpike_MatchesWithResolvedThresholds()
    {
        // Seed pattern uses symbolic "daily_average*3" — test with resolved numeric value
        var resolvedPattern = new DiagnosticPattern
        {
            Id = "cost_spike",
            Category = PatternCategory.Cost,
            Signals =
            [
                new Signal { Attribute = "gen_ai_cost_usd", Operator = SignalOperator.Gt, Value = "30.0" }
            ],
            Hypothesis = "Abnormal cost increase.",
            Confidence = 0.75
        };
        var engine = new PatternEngine([resolvedPattern], CausalRules.All, InvestigationStrategies.All);
        var signals = new List<Signal> { Observed("gen_ai_cost_usd", "150.0") };

        var matches = engine.Evaluate(signals);

        matches.Should().ContainSingle(static m => m.Pattern.Id == "cost_spike");
    }

    // ==========================================================================
    // Pattern matching: negative cases
    // ==========================================================================

    [Fact]
    public void Evaluate_GenAiRateLimit_DoesNotMatch_WhenMissingProviderName()
    {
        var engine = CreateEngine();
        var signals = new List<Signal> { Observed("status_code", "2"), Observed("error_type", "rate_limit") };

        var matches = engine.Evaluate(signals);

        matches.Should().NotContain(static m => m.Pattern.Id == "genai_rate_limit");
    }

    [Fact]
    public void Evaluate_DbTimeout_DoesNotMatch_WhenDurationBelowThreshold()
    {
        var engine = CreateEngine();
        var signals = new List<Signal>
        {
            Observed("exception_type", "TimeoutException"),
            Observed("db.system.name", "postgresql"),
            Observed("duration_ns", "1000000000")
        };

        var matches = engine.Evaluate(signals);

        matches.Should().NotContain(static m => m.Pattern.Id == "db_timeout");
    }

    [Fact]
    public void Evaluate_NoSignals_ReturnsEmpty()
    {
        var engine = CreateEngine();

        var matches = engine.Evaluate([]);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_UnrelatedSignals_ReturnsEmpty()
    {
        var engine = CreateEngine();
        var signals = new List<Signal> { Observed("http.method", "GET"), Observed("user_agent", "Mozilla/5.0") };

        var matches = engine.Evaluate(signals);

        matches.Should().BeEmpty();
    }

    // ==========================================================================
    // Ranking
    // ==========================================================================

    [Fact]
    public void Evaluate_ReturnsMatches_SortedByScoreDescending()
    {
        var engine = CreateEngine();
        // Signals that match multiple patterns (genai_rate_limit @ 0.9 and cascading_timeout @ 0.7)
        var signals = new List<Signal>
        {
            Observed("status_code", "2"),
            Observed("gen_ai_provider_name", "openai"),
            Observed("error_type", "rate_limit"),
            Observed("exception_type", "TaskCanceledException: Timeout occurred"),
            Observed("downstream_service_error", "true")
        };

        var matches = engine.Evaluate(signals);

        matches.Count.Should().BeGreaterThanOrEqualTo(2);
        for (var i = 1; i < matches.Count; i++)
        {
            matches[i - 1].Score.Should().BeGreaterThanOrEqualTo(matches[i].Score,
                $"Match at {i - 1} (score {matches[i - 1].Score}) should rank above {i} (score {matches[i].Score})");
        }
    }

    // ==========================================================================
    // Causal graph
    // ==========================================================================

    [Fact]
    public void BuildCausalGraph_DeployRegressionAndHttp5xx_CreatesEdgeWithStrength085()
    {
        var engine = CreateEngine();
        var deployPattern = DiagnosticPatterns.All.First(static p => p.Id == "deployment_regression");
        var httpPattern = DiagnosticPatterns.All.First(static p => p.Id == "http_5xx_cluster");

        var matches = new List<PatternMatch>
        {
            new(deployPattern, deployPattern.Confidence, deployPattern.Signals),
            new(httpPattern, httpPattern.Confidence, httpPattern.Signals)
        };

        var graph = engine.BuildCausalGraph(matches);

        var edge = graph.Edges.Should().ContainSingle().Which;
        edge.CausePatternId.Should().Be("deployment_regression");
        edge.EffectPatternId.Should().Be("http_5xx_cluster");
        edge.Strength.Should().BeApproximately(0.85, 1e-5);
    }

    [Fact]
    public void BuildCausalGraph_RootCause_IsPatternWithNoIncomingEdges()
    {
        var engine = CreateEngine();
        var deployPattern = DiagnosticPatterns.All.First(static p => p.Id == "deployment_regression");
        var httpPattern = DiagnosticPatterns.All.First(static p => p.Id == "http_5xx_cluster");

        var matches = new List<PatternMatch>
        {
            new(deployPattern, deployPattern.Confidence, deployPattern.Signals),
            new(httpPattern, httpPattern.Confidence, httpPattern.Signals)
        };

        var graph = engine.BuildCausalGraph(matches);

        graph.RootCauses.Should().Contain("deployment_regression");
        graph.RootCauses.Should().NotContain("http_5xx_cluster");
    }

    [Fact]
    public void BuildCausalGraph_ChainedCausality_IdentifiesDeepRootCause()
    {
        var engine = CreateEngine();
        // Chain: db_n_plus_one → db_timeout → http_5xx_cluster
        var nPlusOne = DiagnosticPatterns.All.First(static p => p.Id == "db_n_plus_one");
        var dbTimeout = DiagnosticPatterns.All.First(static p => p.Id == "db_timeout");
        var http5xx = DiagnosticPatterns.All.First(static p => p.Id == "http_5xx_cluster");

        var matches = new List<PatternMatch>
        {
            new(nPlusOne, nPlusOne.Confidence, nPlusOne.Signals),
            new(dbTimeout, dbTimeout.Confidence, dbTimeout.Signals),
            new(http5xx, http5xx.Confidence, http5xx.Signals)
        };

        var graph = engine.BuildCausalGraph(matches);

        graph.Edges.Count.Should().Be(2);
        graph.RootCauses.Should().Contain("db_n_plus_one");
        graph.RootCauses.Should().NotContain("db_timeout");
        graph.RootCauses.Should().NotContain("http_5xx_cluster");
    }

    [Fact]
    public void BuildCausalGraph_SingleMatch_HasNoEdgesAndIsRootCause()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "genai_content_filter");

        var matches = new List<PatternMatch> { new(pattern, pattern.Confidence, pattern.Signals) };

        var graph = engine.BuildCausalGraph(matches);

        graph.Edges.Should().BeEmpty();
        graph.RootCauses.Should().ContainSingle().Which.Should().Be("genai_content_filter");
    }

    [Fact]
    public void BuildCausalGraph_EmptyMatches_ReturnsEmptyGraph()
    {
        var engine = CreateEngine();

        var graph = engine.BuildCausalGraph([]);

        graph.Edges.Should().BeEmpty();
        graph.RootCauses.Should().BeEmpty();
    }

    // ==========================================================================
    // Strategy selection
    // ==========================================================================

    [Fact]
    public void SelectStrategy_ErrorCategory_ReturnsInvestigateErrorIssue()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "http_5xx_cluster");
        var match = new PatternMatch(pattern, pattern.Confidence, pattern.Signals);

        var strategy = engine.SelectStrategy(match);

        strategy.Should().NotBeNull();
        strategy.Id.Should().Be("investigate_error_issue");
    }

    [Fact]
    public void SelectStrategy_LatencyCategory_ReturnsInvestigateLatency()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "cascading_timeout");
        var match = new PatternMatch(pattern, pattern.Confidence, pattern.Signals);

        var strategy = engine.SelectStrategy(match);

        strategy.Should().NotBeNull();
        strategy.Id.Should().Be("investigate_latency");
    }

    [Fact]
    public void SelectStrategy_CostCategory_ReturnsInvestigateCost()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "cost_spike");
        var match = new PatternMatch(pattern, pattern.Confidence, pattern.Signals);

        var strategy = engine.SelectStrategy(match);

        strategy.Should().NotBeNull();
        strategy.Id.Should().Be("investigate_cost");
    }

    [Fact]
    public void SelectStrategy_GenAiCategory_ReturnsInvestigateGenAi()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "genai_rate_limit");
        var match = new PatternMatch(pattern, pattern.Confidence, pattern.Signals);

        var strategy = engine.SelectStrategy(match);

        strategy.Should().NotBeNull();
        strategy.Id.Should().Be("investigate_genai");
    }

    [Fact]
    public void SelectStrategy_DataCategory_ReturnsNull_NoDataCategoryStrategy()
    {
        var engine = CreateEngine();
        var pattern = DiagnosticPatterns.All.First(static p => p.Id == "db_timeout");
        var match = new PatternMatch(pattern, pattern.Confidence, pattern.Signals);

        var strategy = engine.SelectStrategy(match);

        // No strategy with trigger "category:data" exists in seed data
        strategy.Should().BeNull();
    }
}

// ── LogSummaryServiceTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/Query/LogSummaryServiceTests.cs

namespace Qyl.Collector.Tests.Query;

using Collector.Query;
using Collector.Storage;
using Qyl.Contracts.Primitives;
using Xunit;

public sealed class LogSummaryServiceTests
{
    [Fact]
    public async Task BuildSummaryAsync_GroupsErrorPatterns_AndDetectsResolution()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "error", "Timeout after 1200 ms for user 42", now.AddMinutes(-2), 1),
            CreateLog("svc.api", "error", "Timeout after 980 ms for user 15", now.AddMinutes(-2).AddSeconds(1), 2),
            CreateLog("svc.api", "warn", "Retry scheduled in 2 seconds", now.AddMinutes(-2).AddSeconds(2), 3),
            CreateLog("svc.api", "info", "Request successfully completed", now.AddMinutes(-2).AddSeconds(3), 4)
        ], ct);

        var summary = await service.BuildSummaryAsync(
            "5m",
            "svc.api",
            null,
            null,
            null,
            ct);

        summary.Window.Should().Be("5m");
        summary.TotalCount.Should().Be(4);
        summary.ErrorCount.Should().Be(2);
        summary.WarningCount.Should().Be(1);
        summary.Cursor.Should().NotBeEmpty();
        summary.Summary.Should().ContainEquivalentOf("logged 4 entries");

        var topIssue = summary.TopIssues.Should().ContainSingle().Which;
        topIssue.Resolved.Should().BeTrue();
        topIssue.Count.Should().Be(2);
        topIssue.Pattern.Should().Contain("<N>");
    }

    [Fact]
    public async Task BuildSummaryAsync_WithCursor_ReturnsOnlyDeltaRows()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "info", "startup complete", now.AddMinutes(-1), 1)
        ], ct);

        var first = await service.BuildSummaryAsync(
            "5m",
            "svc.api",
            null,
            null,
            null,
            ct);

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "error", "database connection failed", now.AddSeconds(5), 2)
        ], ct);

        var delta = await service.BuildSummaryAsync(
            "5m",
            "svc.api",
            first.Cursor,
            null,
            null,
            ct);

        delta.TotalCount.Should().Be(1);
        delta.ErrorCount.Should().Be(1);
        delta.WarningCount.Should().Be(0);
        delta.TopIssues.Should().ContainSingle();
    }

    [Fact]
    public async Task WaitForLogAsync_ReturnsMatched_WhenFutureLogAppears()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);

        var waitTask = service.WaitForLogAsync(
            new LogWaitRequest(
                "svc.worker",
                Search: "ready",
                TimeoutSeconds: 3,
                PollIntervalMs: 100),
            ct);

        await Task.Delay(200, ct);
        await store.InsertLogsAsync(
        [
            CreateLog("svc.worker", "info", "worker is ready", TimeProvider.System.GetUtcNow().AddSeconds(1), 9)
        ], ct);

        var result = await waitTask;

        result.Matched.Should().BeTrue();
        result.Log.Should().NotBeNull();
        result.Log.Body.Should().ContainEquivalentOf("ready");
        result.WaitedMs.Should().BeGreaterThanOrEqualTo(0);
        result.PollCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BuildPatternsAsync_GroupsTemplatesAndAggregatesSeverity()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "error", "Timeout after 1200 ms for user 42", now.AddMinutes(-2), 1),
            CreateLog("svc.api", "error", "Timeout after 980 ms for user 15", now.AddMinutes(-2).AddSeconds(1), 2),
            CreateLog("svc.api", "fatal", "Timeout after 1100 ms for user 88", now.AddMinutes(-2).AddSeconds(2), 3),
            CreateLog("svc.api", "error", "Database unavailable", now.AddMinutes(-2).AddSeconds(3), 4)
        ], ct);

        var patterns = await service.BuildPatternsAsync(
            "5m",
            "svc.api",
            null,
            null,
            2,
            17,
            null,
            ct);

        var pattern = patterns.Should().ContainSingle().Which;
        pattern.Count.Should().Be(3);
        pattern.ServiceName.Should().Be("svc.api");
        pattern.Template.Should().Contain("<N>");
        pattern.PatternId.Should().NotBeEmpty();
        pattern.SeverityDistribution.Should().Contain(static x => x.Severity == "error" && x.Count == 2);
        pattern.SeverityDistribution.Should().Contain(static x => x.Severity == "fatal" && x.Count == 1);
    }

    [Fact]
    public async Task BuildPatternsAsync_RespectsExplicitTimeRange()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        var older = now.AddMinutes(-20);
        var recent = now.AddMinutes(-2);

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "error", "HTTP 500 for request 1", older, 1),
            CreateLog("svc.api", "error", "HTTP 500 for request 2", recent, 2)
        ], ct);

        var patterns = await service.BuildPatternsAsync(
            "5m",
            "svc.api",
            now.AddMinutes(-5),
            now,
            1,
            17,
            null,
            ct);

        var pattern = patterns.Should().ContainSingle().Which;
        pattern.Count.Should().Be(1);
        pattern.FirstSeen.Should().BeOnOrAfter(now.AddMinutes(-5).UtcDateTime);
    }

    [Fact]
    public async Task BuildStatsAsync_ReturnsSeverityBucketsAndTimeBounds()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "trace", "trace message", now.AddMinutes(-4), 1),
            CreateLog("svc.api", "info", "info message", now.AddMinutes(-3), 2),
            CreateLog("svc.api", "warn", "warn message", now.AddMinutes(-2), 3),
            CreateLog("svc.api", "error", "error message", now.AddMinutes(-1), 4),
            CreateLog("svc.api", "fatal", "fatal message", now.AddSeconds(-30), 5)
        ], ct);

        var stats = await service.BuildStatsAsync(
            "5m",
            "svc.api",
            null,
            null,
            null,
            null,
            ct);

        var bySeverity =
            stats.BySeverity.ToDictionary(static x => x.Severity, static x => x.Count, StringComparer.Ordinal);
        stats.Window.Should().Be("5m");
        stats.TotalCount.Should().Be(5);
        bySeverity["trace"].Should().Be(1);
        bySeverity["debug"].Should().Be(0);
        bySeverity["info"].Should().Be(1);
        bySeverity["warn"].Should().Be(1);
        bySeverity["error"].Should().Be(1);
        bySeverity["fatal"].Should().Be(1);
        stats.OldestTimestamp.Should().NotBeNull();
        stats.NewestTimestamp.Should().NotBeNull();
        stats.NewestTimestamp.Should().BeOnOrAfter(stats.OldestTimestamp.Value);
    }

    [Fact]
    public async Task BuildStatsAsync_RespectsFiltersAndExplicitRange()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new DuckDbStore(":memory:");
        var service = new LogSummaryService(store, TimeProvider.System);
        var now = TimeProvider.System.GetUtcNow();

        await store.InsertLogsAsync(
        [
            CreateLog("svc.api", "error", "timeout at gateway", now.AddMinutes(-3), 1),
            CreateLog("svc.api", "info", "timeout recovered", now.AddMinutes(-3).AddSeconds(1), 2),
            CreateLog("svc.worker", "error", "timeout at worker", now.AddMinutes(-3).AddSeconds(2), 3),
            CreateLog("svc.api", "error", "timeout old event", now.AddMinutes(-20), 4)
        ], ct);

        var stats = await service.BuildStatsAsync(
            "5m",
            "svc.api",
            now.AddMinutes(-5),
            now,
            17,
            "timeout",
            ct);

        var bySeverity =
            stats.BySeverity.ToDictionary(static x => x.Severity, static x => x.Count, StringComparer.Ordinal);
        stats.Window.Should().Be("custom");
        stats.TotalCount.Should().Be(1);
        bySeverity["error"].Should().Be(1);
        bySeverity["fatal"].Should().Be(0);
    }

    private static LogStorageRow CreateLog(
        string service,
        string severityText,
        string body,
        DateTimeOffset timestamp,
        int id) =>
        new()
        {
            LogId = $"sum-{id:D4}",
            TraceId = null,
            SpanId = null,
            SessionId = null,
            TimeUnixNano = TimeConversions.ToUnixNanoUnsigned(timestamp),
            ObservedTimeUnixNano = null,
            SeverityNumber = ToSeverityNumber(severityText),
            SeverityText = severityText,
            Body = body,
            ServiceName = service,
            AttributesJson = "{}",
            ResourceJson = "{}",
            SourceFile = null,
            SourceLine = null,
            SourceColumn = null,
            SourceMethod = null,
            CreatedAt = null
        };

    private static byte ToSeverityNumber(string severityText) =>
        severityText.ToLowerInvariant() switch
        {
            "trace" => 1,
            "debug" => 5,
            "info" => 9,
            "warn" => 13,
            "error" => 17,
            "fatal" => 21,
            _ => 0
        };
}

// ── LiveLogDeduplicatorTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/Realtime/LiveLogDeduplicatorTests.cs

namespace Qyl.Collector.Tests.Realtime;

using Collector.Realtime;
using Collector.Storage;
using Qyl.Contracts.Primitives;
using Xunit;

public sealed class LiveLogDeduplicatorTests
{
    [Fact]
    public void ProcessBatch_EmitsFirstLogImmediately_AndSummaryAfterQuietWindow()
    {
        var t0 = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero);
        var deduplicator = new LiveLogDeduplicator(TimeSpan.FromSeconds(5));

        var emitted = deduplicator.ProcessBatch(
        [
            CreateLog("svc.api", "error", "connection failed", t0, 1),
            CreateLog("svc.api", "error", "connection failed", t0.AddSeconds(1), 2)
        ]);

        emitted.Should().ContainSingle();
        emitted[0].IsDuplicateSummary.Should().BeFalse();
        emitted[0].RepeatCount.Should().Be(1);

        var flushed = deduplicator.FlushExpired(t0.AddSeconds(7).UtcDateTime);
        flushed.Should().ContainSingle();
        flushed[0].IsDuplicateSummary.Should().BeTrue();
        flushed[0].RepeatCount.Should().Be(1);
        flushed[0].Log.Body.Should().Be("connection failed");
    }

    [Fact]
    public void ProcessBatch_DeduplicatesAcrossInterleavedMessages()
    {
        var t0 = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero);
        var deduplicator = new LiveLogDeduplicator(TimeSpan.FromSeconds(5));

        var emitted = deduplicator.ProcessBatch(
        [
            CreateLog("svc.api", "warn", "A", t0, 1),
            CreateLog("svc.api", "warn", "B", t0.AddSeconds(1), 2),
            CreateLog("svc.api", "warn", "A", t0.AddSeconds(2), 3)
        ]);

        emitted.Count.Should().Be(2);
        emitted.Select(static x => x.Log.Body ?? string.Empty).ToArray().Should().BeEquivalentTo("A", "B");

        var flushed = deduplicator.FlushExpired(t0.AddSeconds(10).UtcDateTime);
        flushed.Should().ContainSingle();
        flushed[0].IsDuplicateSummary.Should().BeTrue();
        flushed[0].Log.Body.Should().Be("A");
        flushed[0].RepeatCount.Should().Be(1);
    }

    [Fact]
    public void ProcessBatch_ForceFlushes_WhenSuppressedLimitReached()
    {
        var t0 = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero);
        var deduplicator = new LiveLogDeduplicator(TimeSpan.FromSeconds(30), 2);

        var emitted = deduplicator.ProcessBatch(
        [
            CreateLog("svc.api", "info", "steady noise", t0, 1),
            CreateLog("svc.api", "info", "steady noise", t0.AddSeconds(1), 2),
            CreateLog("svc.api", "info", "steady noise", t0.AddSeconds(2), 3),
            CreateLog("svc.api", "info", "steady noise", t0.AddSeconds(3), 4)
        ]);

        emitted.Count.Should().Be(3);

        emitted[0].IsDuplicateSummary.Should().BeFalse();
        emitted[1].IsDuplicateSummary.Should().BeTrue();
        emitted[1].RepeatCount.Should().Be(2);
        emitted[2].IsDuplicateSummary.Should().BeFalse();
    }

    private static LogStorageRow CreateLog(
        string service,
        string severityText,
        string body,
        DateTimeOffset timestamp,
        int id) =>
        new()
        {
            LogId = $"log-{id:D4}",
            TraceId = null,
            SpanId = null,
            SessionId = null,
            TimeUnixNano = TimeConversions.ToUnixNanoUnsigned(timestamp),
            ObservedTimeUnixNano = null,
            SeverityNumber = ToSeverityNumber(severityText),
            SeverityText = severityText,
            Body = body,
            ServiceName = service,
            AttributesJson = "{}",
            ResourceJson = "{}",
            SourceFile = null,
            SourceLine = null,
            SourceColumn = null,
            SourceMethod = null,
            CreatedAt = null
        };

    private static byte ToSeverityNumber(string severityText) =>
        severityText.ToLowerInvariant() switch
        {
            "trace" => 1,
            "debug" => 5,
            "info" => 9,
            "warn" => 13,
            "error" => 17,
            "fatal" => 21,
            _ => 0
        };
}

// ── DuckDbStoreRegressionTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/Storage/DuckDbStoreRegressionTests.cs

namespace Qyl.Collector.Tests.Storage;

using Collector.Storage;
using DuckDB.NET.Data;
using Xunit;

public sealed class DuckDbStoreRegressionTests
{
    [Fact]
    public async Task DetectRegressionsAsync_BatchesFingerprintLookup()
    {
        await using var store = new DuckDbStore(":memory:");
        const string serviceName = "test-service";
        const int count = 100;

        // Seed resolved errors via upsert — the batched fingerprint lookup
        // path in DetectRegressionsAsync is exercised regardless of result count.
        for (var index = 0; index < count; index++)
        {
            var fingerprint = $"fingerprint-{index}";
            await UpsertErrorAsync(store, $"err-{index}", fingerprint, "resolved", serviceName);
        }

        var sw = Stopwatch.StartNew();
        var regressedIds = await store.DetectRegressionsAsync(serviceName, ct: TestContext.Current.CancellationToken);
        sw.Stop();

        // With one-row-per-fingerprint and upsert semantics, regression detection
        // requires separate 'resolved' and 'new' rows — which the unique index
        // prevents. The batched query path is still exercised and must not throw.
        regressedIds.Should().BeEmpty();
        Console.WriteLine($"DetectRegressionsAsync for {count} candidates took: {sw.ElapsedMilliseconds}ms");
    }

    private static Task UpsertErrorAsync(
        DuckDbStore store,
        string errorId,
        string fingerprint,
        string status,
        string serviceName) =>
        store.ExecuteWriteAsync(async (con, token) =>
        {
            var now = TimeProvider.System.GetUtcNow().UtcDateTime;
            await using var cmd = con.CreateCommand();
            cmd.CommandText = """
                              INSERT INTO errors (
                                  error_id,
                                  error_type,
                                  message,
                                  category,
                                  fingerprint,
                                  first_seen,
                                  last_seen,
                                  occurrence_count,
                                  status,
                                  affected_services
                              )
                              VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
                              ON CONFLICT (fingerprint) DO UPDATE SET
                                  status = EXCLUDED.status,
                                  last_seen = EXCLUDED.last_seen,
                                  occurrence_count = errors.occurrence_count + 1
                              """;
            cmd.Parameters.Add(new DuckDBParameter { Value = errorId });
            cmd.Parameters.Add(new DuckDBParameter { Value = "type" });
            cmd.Parameters.Add(new DuckDBParameter { Value = "msg" });
            cmd.Parameters.Add(new DuckDBParameter { Value = "cat" });
            cmd.Parameters.Add(new DuckDBParameter { Value = fingerprint });
            cmd.Parameters.Add(new DuckDBParameter { Value = now });
            cmd.Parameters.Add(new DuckDBParameter { Value = now });
            cmd.Parameters.Add(new DuckDBParameter { Value = 1L });
            cmd.Parameters.Add(new DuckDBParameter { Value = status });
            cmd.Parameters.Add(new DuckDBParameter { Value = serviceName });
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        });
}

// ── HttpTelemetryStoreTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.mcp.tests/HttpTelemetryStoreTests.cs

namespace Qyl.Mcp.Tests;

public sealed class HttpTelemetryStoreTests
{
    [Fact]
    public async Task GetRunAsync_MapsCollectorSessionToAgentRun()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "session_id": "run-42",
                  "service_name": "planner",
                  "span_count": 4,
                  "error_count": 1,
                  "total_input_tokens": 120,
                  "total_output_tokens": 30,
                  "total_cost_usd": 0.0042,
                  "start_time": "2026-04-02T10:00:00Z",
                  "end_time": "2026-04-02T10:00:03Z",
                  "providers": ["openai"],
                  "models": ["gpt-4o"]
                }
                """))
        };

        var store = CreateStore(handler);
        var run = await store.GetRunAsync("run-42");

        run.Should().NotBeNull();
        run!.RunId.Should().Be("run-42");
        run.AgentName.Should().Be("planner");
        run.Provider.Should().Be("openai");
        run.Model.Should().Be("gpt-4o");
        run.InputTokens.Should().Be(120);
        run.OutputTokens.Should().Be(30);
        run.Success.Should().BeFalse();
        run.ErrorType.Should().Be("Error");
        run.ErrorMessage.Should().Be("1 error(s)");
        run.Duration.Should().Be(TimeSpan.FromSeconds(3));
        handler.LastRequestUri.Should().Be(TestCollectorEndpoint.Path("/api/v1/sessions/run-42"));
    }

    [Fact]
    public async Task GetRunAsync_WhenCollectorRequestFails_ReturnsNull()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => throw new HttpRequestException("boom")
        };

        var store = CreateStore(handler);
        var run = await store.GetRunAsync("missing-run");

        run.Should().BeNull();
    }

    [Fact]
    public async Task SearchRunsAsync_FiltersModelErrorTypeAndSince()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "keep",
                      "service_name": "planner",
                      "span_count": 3,
                      "error_count": 1,
                      "total_input_tokens": 50,
                      "total_output_tokens": 10,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:00:00Z",
                      "end_time": "2026-04-02T10:00:01Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    },
                    {
                      "session_id": "wrong-model",
                      "service_name": "planner",
                      "span_count": 3,
                      "error_count": 1,
                      "total_input_tokens": 50,
                      "total_output_tokens": 10,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:00:00Z",
                      "end_time": "2026-04-02T10:00:01Z",
                      "providers": ["openai"],
                      "models": ["gpt-4.1"]
                    },
                    {
                      "session_id": "too-old",
                      "service_name": "planner",
                      "span_count": 3,
                      "error_count": 1,
                      "total_input_tokens": 50,
                      "total_output_tokens": 10,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-01T10:00:00Z",
                      "end_time": "2026-04-01T10:00:01Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    }
                  ],
                  "total": 3
                }
                """))
        };

        var store = CreateStore(handler);
        var results = await store.SearchRunsAsync(
            "openai",
            "gpt-4o",
            "Error",
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));

        var run = results.Should().ContainSingle().Which;
        run.RunId.Should().Be("keep");
        handler.LastRequestUri.Should().Be(
            $"{TestCollectorEndpoint.Path("/api/v1/sessions")}?limit=100&provider=openai");
    }

    [Fact]
    public async Task SearchRunsAsync_WhenCollectorRequestFails_ReturnsEmpty()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => throw new HttpRequestException("boom")
        };

        var store = CreateStore(handler);
        var results = await store.SearchRunsAsync(
            "openai",
            "gpt-4o",
            "Error",
            null);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTokenUsageAsync_GroupsByModelWithinRequestedRange()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "inside-a",
                      "service_name": "planner",
                      "span_count": 1,
                      "error_count": 0,
                      "total_input_tokens": 10,
                      "total_output_tokens": 4,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:15:00Z",
                      "end_time": "2026-04-02T10:16:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    },
                    {
                      "session_id": "inside-b",
                      "service_name": "planner",
                      "span_count": 1,
                      "error_count": 0,
                      "total_input_tokens": 6,
                      "total_output_tokens": 2,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:30:00Z",
                      "end_time": "2026-04-02T10:31:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    },
                    {
                      "session_id": "outside",
                      "service_name": "planner",
                      "span_count": 1,
                      "error_count": 0,
                      "total_input_tokens": 99,
                      "total_output_tokens": 99,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-03T12:00:00Z",
                      "end_time": "2026-04-03T12:01:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4.1"]
                    }
                  ],
                  "total": 3
                }
                """))
        };

        var store = CreateStore(handler);
        var summaries = await store.GetTokenUsageAsync(
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 2, 23, 59, 59, DateTimeKind.Utc),
            "model");

        var summary = summaries.Should().ContainSingle().Which;
        summary.GroupKey.Should().Be("gpt-4o");
        summary.TotalInputTokens.Should().Be(16);
        summary.TotalOutputTokens.Should().Be(6);
        summary.RunCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTokenUsageAsync_UsesServiceNameGroupingByDefault()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "planner-run",
                      "service_name": "planner",
                      "span_count": 1,
                      "error_count": 0,
                      "total_input_tokens": 8,
                      "total_output_tokens": 3,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:15:00Z",
                      "end_time": "2026-04-02T10:16:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    },
                    {
                      "session_id": "coder-run",
                      "service_name": "coder",
                      "span_count": 1,
                      "error_count": 0,
                      "total_input_tokens": 5,
                      "total_output_tokens": 2,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:30:00Z",
                      "end_time": "2026-04-02T10:31:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4.1"]
                    }
                  ],
                  "total": 2
                }
                """))
        };

        var store = CreateStore(handler);
        var summaries = await store.GetTokenUsageAsync(null, null, "service");

        summaries.Length.Should().Be(2);
        summaries.Should().Contain(static summary => summary.GroupKey == "planner" && summary.RunCount == 1);
        summaries.Should().Contain(static summary => summary.GroupKey == "coder" && summary.RunCount == 1);
    }

    [Fact]
    public async Task GetTokenUsageAsync_WhenCollectorReturnsNoSessions_ReturnsEmpty()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [],
                  "total": 0
                }
                """))
        };

        var store = CreateStore(handler);
        var summaries = await store.GetTokenUsageAsync(null, null, "model");

        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task ListErrorsAsync_ReturnsErroredSessionsAndHonorsAgentNameFilter()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "error-run",
                      "service_name": "planner",
                      "span_count": 3,
                      "error_count": 2,
                      "total_input_tokens": 5,
                      "total_output_tokens": 1,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:00:00Z",
                      "end_time": "2026-04-02T10:01:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    },
                    {
                      "session_id": "healthy-run",
                      "service_name": "planner",
                      "span_count": 3,
                      "error_count": 0,
                      "total_input_tokens": 5,
                      "total_output_tokens": 1,
                      "total_cost_usd": 0.001,
                      "start_time": "2026-04-02T10:00:00Z",
                      "end_time": "2026-04-02T10:01:00Z",
                      "providers": ["openai"],
                      "models": ["gpt-4o"]
                    }
                  ],
                  "total": 2
                }
                """))
        };

        var store = CreateStore(handler);
        var errors = await store.ListErrorsAsync(10, "planner");

        var error = errors.Should().ContainSingle().Which;
        error.RunId.Should().Be("error-run");
        error.ErrorType.Should().Be("Error");
        handler.LastRequestUri.Should().Be(
            $"{TestCollectorEndpoint.Path("/api/v1/sessions")}?limit=10&serviceName=planner");
    }

    [Fact]
    public async Task ListErrorsAsync_WhenCollectorRequestFails_ReturnsEmpty()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => throw new HttpRequestException("boom")
        };

        var store = CreateStore(handler);
        var errors = await store.ListErrorsAsync(10, "planner");

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatencyStatsAsync_ComputesPercentilesFromSpanCounts()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "run-a",
                      "service_name": "planner",
                      "span_count": 10,
                      "error_count": 0,
                      "total_input_tokens": 0,
                      "total_output_tokens": 0,
                      "total_cost_usd": 0.0,
                      "start_time": "2026-04-02T10:10:00Z",
                      "end_time": "2026-04-02T10:10:01Z",
                      "providers": [],
                      "models": []
                    },
                    {
                      "session_id": "run-b",
                      "service_name": "planner",
                      "span_count": 20,
                      "error_count": 0,
                      "total_input_tokens": 0,
                      "total_output_tokens": 0,
                      "total_cost_usd": 0.0,
                      "start_time": "2026-04-02T10:20:00Z",
                      "end_time": "2026-04-02T10:20:01Z",
                      "providers": [],
                      "models": []
                    },
                    {
                      "session_id": "run-c",
                      "service_name": "planner",
                      "span_count": 30,
                      "error_count": 0,
                      "total_input_tokens": 0,
                      "total_output_tokens": 0,
                      "total_cost_usd": 0.0,
                      "start_time": "2026-04-02T10:30:00Z",
                      "end_time": "2026-04-02T10:30:01Z",
                      "providers": [],
                      "models": []
                    }
                  ],
                  "total": 3
                }
                """))
        };

        var store = CreateStore(
            handler,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero)));
        var stats = await store.GetLatencyStatsAsync("planner", 2);

        stats.AgentName.Should().Be("planner");
        stats.P50Ms.Should().Be(20);
        stats.P95Ms.Should().Be(30);
        stats.P99Ms.Should().Be(30);
        stats.AvgMs.Should().Be(20);
        stats.MinMs.Should().Be(10);
        stats.MaxMs.Should().Be(30);
        stats.SampleCount.Should().Be(3);
    }

    [Fact]
    public async Task GetLatencyStatsAsync_WhenNoSessionsMatch_ReturnsZeroStats()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Responder = static (_, _) => Task.FromResult(JsonResponse(
                """
                {
                  "items": [
                    {
                      "session_id": "old-run",
                      "service_name": "planner",
                      "span_count": 10,
                      "error_count": 0,
                      "total_input_tokens": 0,
                      "total_output_tokens": 0,
                      "total_cost_usd": 0.0,
                      "start_time": "2026-04-01T08:00:00Z",
                      "end_time": "2026-04-01T08:00:01Z",
                      "providers": [],
                      "models": []
                    }
                  ],
                  "total": 1
                }
                """))
        };

        var store = CreateStore(
            handler,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero)));
        var stats = await store.GetLatencyStatsAsync("planner", 2);

        stats.AgentName.Should().Be("planner");
        stats.P50Ms.Should().Be(0);
        stats.P95Ms.Should().Be(0);
        stats.P99Ms.Should().Be(0);
        stats.AvgMs.Should().Be(0);
        stats.MinMs.Should().Be(0);
        stats.MaxMs.Should().Be(0);
        stats.SampleCount.Should().Be(0);
    }

    private static HttpTelemetryStore CreateStore(
        RecordingHttpMessageHandler handler,
        TimeProvider? timeProvider = null)
    {
        var client = new HttpClient(handler) { BaseAddress = TestCollectorEndpoint.BaseAddress };

        return new HttpTelemetryStore(
            client,
            timeProvider ?? TimeProvider.System,
            NullLogger<HttpTelemetryStore>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

;

public sealed class McpTelemetryTests : IDisposable
{
    private readonly List<Activity> _collected = [];
    private readonly ActivityListener _listener;

    public McpTelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == McpAttributes.SourceName,
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _collected.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void ActivitySource_EmitsUnderQylMcpName()
    {
        TelemetryConstants.ActivitySource.Name.Should().Be("qyl.mcp");

        using var activity = TelemetryConstants.ActivitySource.StartActivity("mcp.receive ping", ActivityKind.Server);

        activity.Should().NotBeNull();
        _collected.Should().ContainSingle(static a => a.OperationName == "mcp.receive ping");
    }

    [Fact]
    public void McpAttributes_WireValues_MatchSemconv()
    {
        // Protects the wire format — if someone renames a value, downstream breaks silently
        McpAttributes.MethodName.Should().Be("mcp.method.name");
        McpAttributes.ProtocolVersion.Should().Be("mcp.protocol.version");
        McpAttributes.SessionId.Should().Be("mcp.session.id");
        McpAttributes.ServerName.Should().Be("mcp.server.name");
        McpAttributes.JsonrpcRequestId.Should().Be("jsonrpc.request.id");
        McpAttributes.JsonrpcProtocolVersion.Should().Be("jsonrpc.protocol.version");
        McpAttributes.ErrorType.Should().Be("error.type");
    }

    [Fact]
    public void McpAttributes_Methods_MatchMcpSpec()
    {
        McpAttributes.Methods.ToolsCall.Should().Be("tools/call");
        McpAttributes.Methods.PromptsGet.Should().Be("prompts/get");
        McpAttributes.Methods.ResourcesRead.Should().Be("resources/read");
        McpAttributes.Methods.Initialize.Should().Be("initialize");
    }
}

// ── ToolManifestGeneratorTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.mcp.generators.tests/ToolManifestGeneratorTests.cs

namespace qyl.mcp.generators.tests;

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Qyl.Mcp.Generators;
using Xunit;

/// <summary>
///     Tests for the ToolManifestGenerator.
///     Verifies that the generator correctly discovers [McpServerToolType] classes
///     and their [McpServerTool] methods, emitting both the Type[] array and
///     the AOT-safe CreateTools factory method.
/// </summary>
public sealed class ToolManifestGeneratorTests
{
    private const string AttributeStubs = """
                                          namespace ModelContextProtocol.Server
                                          {
                                              [System.AttributeUsage(System.AttributeTargets.Class)]
                                              public sealed class McpServerToolTypeAttribute : System.Attribute { }

                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public sealed class McpServerToolAttribute : System.Attribute
                                              {
                                                  public string? Name { get; set; }
                                                  public string? Title { get; set; }
                                                  public bool ReadOnly { get; set; }
                                                  public bool Destructive { get; set; }
                                                  public bool Idempotent { get; set; }
                                                  public bool OpenWorld { get; set; }
                                              }
                                          }

                                          namespace Microsoft.Extensions.AI
                                          {
                                              public abstract class AIFunction { }
                                              public sealed class AIFunctionFactoryOptions { public string? Name { get; set; } }
                                              public static class AIFunctionFactory
                                              {
                                                  public static AIFunction Create(System.Delegate method, AIFunctionFactoryOptions? options = null)
                                                      => throw new System.NotImplementedException();
                                              }
                                          }

                                          namespace Microsoft.Extensions.DependencyInjection
                                          {
                                              public static class ServiceProviderServiceExtensions
                                              {
                                                  public static T GetRequiredService<T>(System.IServiceProvider services)
                                                      => throw new System.NotImplementedException();
                                              }
                                          }
                                          """;

    [Fact]
    public void SingleToolType_WithMethods_EmitsTypeArrayAndCreateTools()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class MyTools
                     {
                         [McpServerTool(Name = "test.greet")]
                         public string Greet(string name) => $"Hello {name}";

                         [McpServerTool(Name = "test.farewell")]
                         public string Farewell(string name) => $"Goodbye {name}";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("typeof(global::TestApp.Tools.MyTools)", generated, StringComparison.Ordinal);
        Assert.Contains("svc_TestApp_Tools_MyTools.Greet", generated, StringComparison.Ordinal);
        Assert.Contains("svc_TestApp_Tools_MyTools.Farewell", generated, StringComparison.Ordinal);
        Assert.Contains("Name = \"test.greet\"", generated, StringComparison.Ordinal);
        Assert.Contains("Name = \"test.farewell\"", generated, StringComparison.Ordinal);
        Assert.Contains("CreateTools(", generated, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<global::TestApp.Tools.MyTools>", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MethodName_UsedAsFallback_WhenAttributeNameIsNull()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class FallbackTools
                     {
                         [McpServerTool]
                         public string DoWork() => "done";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("Name = \"DoWork\"", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NoToolMethods_SkipsServiceResolution()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class EmptyTools
                     {
                         public string NotATool() => "nope";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("typeof(global::TestApp.Tools.EmptyTools)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<global::TestApp.Tools.EmptyTools>", generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedFile_IsExcluded()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class GeneratedTools
                     {
                         [McpServerTool(Name = "gen.tool")]
                         public string GenTool() => "gen";
                     }
                     """;

        var generated = RunGenerator(source, "GeneratedTools.g.cs");

        Assert.Empty(generated);
    }

    [Fact]
    public void MultipleClasses_DeterministicOrdering()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class ZetaTools
                     {
                         [McpServerTool(Name = "zeta.go")]
                         public string Go() => "z";
                     }

                     [McpServerToolType]
                     public sealed class AlphaTools
                     {
                         [McpServerTool(Name = "alpha.go")]
                         public string Go() => "a";
                     }
                     """;

        var generated = RunGenerator(source);

        var alphaIndex = generated.IndexOf("AlphaTools", StringComparison.Ordinal);
        var zetaIndex = generated.IndexOf("ZetaTools", StringComparison.Ordinal);
        Assert.True(alphaIndex < zetaIndex, "AlphaTools should appear before ZetaTools (ordered by FQN)");
    }

    [Fact]
    public void FilterParameter_EmittedOnCreateTools()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class FilterableTools
                     {
                         [McpServerTool(Name = "f.tool")]
                         public string Tool() => "t";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("filter?.Invoke(typeof(global::TestApp.Tools.FilterableTools)) != false", generated,
            StringComparison.Ordinal);
    }

    [Fact]
    public void StaticMethods_AreExcluded()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class MixedTools
                     {
                         [McpServerTool(Name = "instance.tool")]
                         public string InstanceTool() => "i";

                         [McpServerTool(Name = "static.tool")]
                         public static string StaticTool() => "s";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("Name = \"instance.tool\"", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Name = \"static.tool\"", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PrivateMethods_AreExcluded()
    {
        var source = """
                     using ModelContextProtocol.Server;

                     namespace TestApp.Tools;

                     [McpServerToolType]
                     public sealed class AccessTools
                     {
                         [McpServerTool(Name = "pub.tool")]
                         public string PublicTool() => "p";

                         [McpServerTool(Name = "priv.tool")]
                         private string PrivateTool() => "v";
                     }
                     """;

        var generated = RunGenerator(source);

        Assert.Contains("Name = \"pub.tool\"", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Name = \"priv.tool\"", generated, StringComparison.Ordinal);
    }

    private static string RunGenerator(string source, string filePath = "Test.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var stubTree = CSharpSyntaxTree.ParseText(AttributeStubs, path: "Stubs.cs");

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        TryAddReference(references, Path.Combine(runtimeDir, "System.Runtime.dll"));
        TryAddReference(references, Path.Combine(runtimeDir, "netstandard.dll"));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, stubTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ToolManifestGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(compilation).GetRunResult();

        var generatedSource = result.GeneratedTrees
            .FirstOrDefault(static t => t.FilePath.Contains("QylToolManifest", StringComparison.Ordinal));

        return generatedSource?.GetText().ToString() ?? string.Empty;
    }

    // Probe optional runtime reference without System.IO.File — RS1035 bans File in
    // projects pulling in Microsoft.CodeAnalysis.Analyzers via the generator ref.
    // MetadataReference.CreateFromFile throws FileNotFoundException on missing path;
    // that's expected when the SDK layout differs across hosts.
    private static void TryAddReference(List<MetadataReference> references, string path)
    {
        try
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
        catch (FileNotFoundException ex)
        {
            Trace.WriteLine($"optional ref skipped: {path} ({ex.Message})");
        }
    }
}

// ── CodingAgentEndpointsTests.cs ────────────────────────────────────────────────────────────
// Source: tests/qyl.collector.tests/Autofix/CodingAgentEndpointsTests.cs

namespace Qyl.Collector.Tests.Autofix;

using Collector.Storage;
using DuckDB.NET.Data;
using Qyl.Contracts.Loom;
using Xunit;

/// <summary>
///     Storage-layer tests for the coding agent run operations backing
///     POST/GET/PUT /api/v1/fix-runs/{fixRunId}/coding-agents.
/// </summary>
public sealed class CodingAgentEndpointsTests : IAsyncDisposable
{
    private readonly DuckDbStore _store = new(":memory:");

    public ValueTask DisposeAsync() => _store.DisposeAsync();

    private Task SeedFixRunAsync(string fixRunId) =>
        _store.ExecuteWriteAsync(async (con, token) =>
        {
            await using var cmd = con.CreateCommand();
            cmd.CommandText = """
                              INSERT INTO fix_runs (run_id, issue_id, status, policy)
                              VALUES ($1, 'issue-abc', 'running', 'auto')
                              ON CONFLICT (run_id) DO NOTHING
                              """;
            cmd.Parameters.Add(new DuckDBParameter { Value = fixRunId });
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        });

    private static CodingAgentRunRecord Record(string id, string fixRunId, string provider = "Loom") =>
        new()
        {
            Id = id,
            FixRunId = fixRunId,
            Provider = CodingAgentProviderNames.NormalizeSlug(provider),
            Status = "pending",
            CreatedAt = TimeProvider.System.GetUtcNow().UtcDateTime
        };

    [Fact]
    public async Task Insert_ThenGetById_ReturnsPersistedRecord()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedFixRunAsync("fix-001");

        await _store.InsertCodingAgentRunAsync(Record("agent-001", "fix-001", "cursor"), ct);
        var result = await _store.GetCodingAgentRunAsync("agent-001", ct);

        result.Should().NotBeNull();
        result.FixRunId.Should().Be("fix-001");
        result.Provider.Should().Be("cursor");
        result.Status.Should().Be("pending");
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        (await _store.GetCodingAgentRunAsync("does-not-exist", ct)).Should().BeNull();
    }

    [Fact]
    public async Task GetRunsForFixRun_ReturnsOnlyMatchingFixRun()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedFixRunAsync("fix-a");
        await SeedFixRunAsync("fix-b");
        await _store.InsertCodingAgentRunAsync(Record("agent-a", "fix-a"), ct);
        await _store.InsertCodingAgentRunAsync(Record("agent-b", "fix-b"), ct);

        var runs = await _store.GetCodingAgentRunsForFixRunAsync("fix-a", 50, ct);

        runs.Should().ContainSingle();
        runs[0].FixRunId.Should().Be("fix-a");
    }

    [Fact]
    public async Task GetRunsForFixRun_LimitIsRespected()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedFixRunAsync("fix-limit");
        for (var i = 0; i < 5; i++)
            await _store.InsertCodingAgentRunAsync(Record($"agent-l{i}", "fix-limit"), ct);

        var runs = await _store.GetCodingAgentRunsForFixRunAsync("fix-limit", 2, ct);

        runs.Count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatusAndSetsUrls()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedFixRunAsync("fix-upd");
        await _store.InsertCodingAgentRunAsync(Record("agent-upd", "fix-upd", "cursor"), ct);

        await _store.UpdateCodingAgentRunStatusAsync(
            "agent-upd", "completed",
            "https://github.com/acme/repo/pull/42",
            "https://cursor.sh/agents/xyz",
            ct);

        var updated = await _store.GetCodingAgentRunAsync("agent-upd", ct);

        updated.Should().NotBeNull();
        updated.Status.Should().Be("completed");
        updated.PrUrl.Should().Be("https://github.com/acme/repo/pull/42");
        updated.AgentUrl.Should().Be("https://cursor.sh/agents/xyz");
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_Running_DoesNotSetCompletedAt()
    {
        var ct = TestContext.Current.CancellationToken;
        await SeedFixRunAsync("fix-pend");
        await _store.InsertCodingAgentRunAsync(Record("agent-pend", "fix-pend"), ct);

        await _store.UpdateCodingAgentRunStatusAsync("agent-pend", "running", ct: ct);

        var updated = await _store.GetCodingAgentRunAsync("agent-pend", ct);

        updated.Should().NotBeNull();
        updated.CompletedAt.Should().BeNull();
    }
}

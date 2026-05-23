using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

/// <summary>
/// Regression tests for <see cref="IncrementalValuesProviderExtensions"/> behaviors that affect
/// generator correctness: cache stability, cancellation propagation, deterministic emission order.
/// </summary>
public sealed class IncrementalValuesProviderExtensionsTests
{
    [Fact]
    public void GeneratorErrorInfo_From_CapturesType_AndMessage()
    {
        var caught = CaptureThrow();

        var info = GeneratorErrorInfo.From(caught);

        info.TypeName.Should().Be(typeof(InvalidOperationException).FullName);
        info.Message.Should().Be("boom");
        info.ToString().Should().StartWith($"{typeof(InvalidOperationException).FullName}: boom");
    }

    [Fact]
    public void GeneratorErrorInfo_HasValueEquality_SoItIsCacheStable()
    {
        // Two distinct Exception instances with identical surface produce equal GeneratorErrorInfos.
        // This is the property that makes the type safe to flow through the incremental cache.
        var first = CaptureThrow();
        var second = CaptureThrow();

        var a = GeneratorErrorInfo.From(first);
        var b = GeneratorErrorInfo.From(second);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        ReferenceEquals(first, second).Should().BeFalse();
    }

    [Fact]
    public void GroupBy_PreservesKeyInsertionOrder()
    {
        // The previous implementation iterated a Dictionary directly, which has no documented
        // iteration order. Generators that consume GroupBy output must see a stable, deterministic
        // sequence so generated source is reproducible across runs.
        var observed = RunGroupByGenerator(GroupByOrderingGenerator.Source);

        observed.Should().Equal("Beta", "Alpha", "Gamma");
    }

    [Fact]
    public void SelectAndReportExceptions_ValuesOverload_PropagatesCancellation()
    {
        // The pre-cancelled token is the *system under test* — using
        // TestContext.Current.CancellationToken would defeat the purpose, so xUnit1051 is
        // suppressed for this test only.
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var driver = CSharpGeneratorDriver.Create(new CancellingGenerator());
        var ct = TestContext.Current.CancellationToken;
        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText("class C { }", cancellationToken: ct)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

#pragma warning disable xUnit1051
        Action act = () => driver.RunGenerators(compilation, cts.Token);
#pragma warning restore xUnit1051

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void SelectAndReportExceptions_SingleValueOverload_ReportsDiagnosticWithoutThrowing()
    {
        var ct = TestContext.Current.CancellationToken;
        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText("class C { }", cancellationToken: ct)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SingleValueFailingGenerator());
        driver = driver.RunGenerators(compilation, ct);
        var result = driver.GetRunResult().Results.Single();

        result.Exception.Should().BeNull();
        result.Diagnostics.Should().ContainSingle(diagnostic =>
            diagnostic.Id == "SINGLE001" &&
            diagnostic.GetMessage().Contains("single boom", StringComparison.Ordinal));
    }

    private static InvalidOperationException CaptureThrow()
    {
        try
        {
            ThrowDeep();
            throw new InvalidOperationException("unreachable");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    private static void ThrowDeep()
    {
        throw new InvalidOperationException("boom");
    }

    private static List<string> RunGroupByGenerator(string source)
    {
        var ct = TestContext.Current.CancellationToken;
        var compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: ct)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new GroupByOrderingGenerator());
        driver = driver.RunGenerators(compilation, ct);
        var result = driver.GetRunResult().Results.Single();
        result.Exception.Should().BeNull();

        return GroupByOrderingGenerator.LastObservedKeys;
    }

    /// <summary>
    /// Probes the <see cref="IncrementalValuesProviderExtensions.GroupBy{TSource,TKey}" /> emission
    /// order by feeding a fixed sequence and recording the keys back in a static field.
    /// </summary>
    private sealed class GroupByOrderingGenerator : IIncrementalGenerator
    {
        public const string Source = """
                                     namespace Probe
                                     {
                                         class Marker { }
                                     }
                                     """;

        public static List<string> LastObservedKeys { get; } = [];

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            LastObservedKeys.Clear();

            var seed = context.CompilationProvider.SelectMany(static (_, _) =>
                ImmutableArray.Create(
                    new Item("Beta", 1),
                    new Item("Alpha", 1),
                    new Item("Beta", 2),
                    new Item("Gamma", 1),
                    new Item("Alpha", 2)));

            var grouped = seed.GroupBy(static x => x.Key, static x => x.Value);

            context.RegisterSourceOutput(grouped, static (_, group) =>
            {
                lock (LastObservedKeys)
                {
                    LastObservedKeys.Add(group.Key);
                }
            });
        }

        private readonly record struct Item(string Key, int Value);
    }

    /// <summary>
    /// Drives <see cref="IncrementalValuesProviderExtensions.SelectAndReportExceptions{TSource,TResult}"
    /// /> with a selector that observes the cancellation token to confirm cancellation flows out
    /// of the pipeline rather than being swallowed.
    /// </summary>
    private sealed class CancellingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var seed = context.CompilationProvider
                .SelectMany(static (_, _) => ImmutableArray.Create(0));

            seed.SelectAndReportExceptions(
                    static (_, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return FileWithName.Empty;
                    },
                    context)
                .AddSource(context);
        }
    }

    /// <summary>
    /// Probes the single-value exception helper. It cannot filter out a bad item like
    /// <see cref="IncrementalValuesProvider{TValues}" />, so the API must expose an explicit
    /// result/diagnostic envelope and report the captured diagnostic.
    /// </summary>
    private sealed class SingleValueFailingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.CompilationProvider.SelectAndReportExceptions<Compilation, FileWithName>(
                static (_, _) => throw new InvalidOperationException("single boom"),
                context,
                "SINGLE001");
        }
    }
}

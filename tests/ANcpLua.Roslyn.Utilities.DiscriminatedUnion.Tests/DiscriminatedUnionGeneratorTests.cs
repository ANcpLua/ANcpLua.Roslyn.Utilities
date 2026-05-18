using ANcpLua.Analyzers.DiscriminatedUnion;
using ANcpLua.Roslyn.Utilities.Testing.GeneratorHelpers;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ANcpLua.Analyzers.DiscriminatedUnion.Tests;

public sealed class DiscriminatedUnionGeneratorTests
{
    [Fact]
    public void Emits_PrivateCtor_Sealed_Cases_And_Match_Switch()
    {
        const string source = """
                              using ANcpLua.Analyzers.DiscriminatedUnion;

                              namespace TestNs
                              {
                                  [DiscriminatedUnion]
                                  public partial record Msg
                                  {
                                      public partial record AddPoint(double X, double Y);
                                      public partial record Undo;
                                      public partial record Redo;
                                  }
                              }
                              """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        var generated = result.RunResult.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("TestNs.Msg.DiscriminatedUnion.g.cs", System.StringComparison.Ordinal))
            .ToString();

        generated.Should()
            .Contain("namespace TestNs;")
            .And.Contain("abstract partial record Msg")
            .And.Contain("private Msg() { }")
            .And.Contain("public abstract TResult Match<TResult>(")
            .And.Contain("public abstract void Switch(")
            .And.Contain("global::System.Func<AddPoint, TResult> addPoint")
            .And.Contain("global::System.Func<Undo, TResult> undo")
            .And.Contain("global::System.Func<Redo, TResult> redo")
            .And.Contain("global::System.Action<AddPoint> addPoint")
            .And.Contain("public sealed partial record AddPoint : Msg")
            .And.Contain("public sealed partial record Undo : Msg")
            .And.Contain("public sealed partial record Redo : Msg")
            .And.Contain(") => addPoint(this);")
            .And.Contain(") => undo(this);")
            .And.Contain(") => redo(this);");

        result.RunResult.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Output_Compilation_Has_No_Errors_And_Switch_Is_Exhaustive()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Result
                {
                    public partial record Ok(int Value);
                    public partial record Err(string Reason);
                }

                public static class Sample
                {
                    public static string Describe(Result r) => r.Match(
                        ok:  o => $"ok {o.Value}",
                        err: e => $"err {e.Reason}");
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        var errors = result.OutputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Id} @ {d.Location.GetLineSpan().StartLinePosition}: {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")
            .ToArray();

        var generated = string.Join(
            "\n----\n",
            result.RunResult.GeneratedTrees.Select(t => $"{t.FilePath}\n{t}"));

        errors.Should().BeEmpty("the generated DU must compile cleanly alongside consumer code; generated:\n" + generated);
    }

    [Fact]
    public void Closed_Hierarchy_Rejects_External_Inheritance()
    {
        // The private base ctor should make derivation outside the union root impossible.
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Msg
                {
                    public partial record A;
                    public partial record B;
                }

                public sealed record EvilMsg : Msg;  // must fail to compile
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        var errors = result.OutputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        errors.Should().NotBeEmpty("external types must not be able to participate in the closed union");
    }

    [Fact]
    public void Reports_AL0300_When_Root_Is_Not_Partial()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public record Msg
                {
                    public partial record A;
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0300");
    }

    [Fact]
    public void Reports_AL0301_When_Root_Has_No_Cases()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Empty
                {
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0301");
    }

    [Fact]
    public void Reports_AL0302_When_Case_Is_Not_Partial_Record()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Msg
                {
                    public class NotARecord { }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0302");
    }

    [Fact]
    public void Reports_AL0303_When_Root_Has_PrimaryCtor_Parameters()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Msg(string Common)
                {
                    public partial record A;
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0303");
    }

    [Fact]
    public void Generic_Union_Root_Flows_Type_Parameters_To_Cases()
    {
        const string source = """
            using ANcpLua.Analyzers.DiscriminatedUnion;

            namespace TestNs
            {
                [DiscriminatedUnion]
                public partial record Result<T>
                {
                    public partial record Ok(T Value);
                    public partial record Err(string Reason);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<DiscriminatedUnionGenerator>(source);

        var generated = result.RunResult.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("TestNs.Result.DiscriminatedUnion.g.cs", System.StringComparison.Ordinal))
            .ToString();

        generated.Should()
            .Contain("abstract partial record Result<T>")
            .And.Contain("public sealed partial record Ok : Result<T>")
            .And.Contain("public sealed partial record Err : Result<T>");

        var errors = result.OutputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        errors.Should().BeEmpty("a generic union must compile as cleanly as a non-generic one");
    }
}

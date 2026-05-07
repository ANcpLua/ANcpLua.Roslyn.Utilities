using ANcpLua.Analyzers.ExtensibleEnumMirror;
using ANcpLua.Roslyn.Utilities.Testing.GeneratorHelpers;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ANcpLua.Analyzers.ExtensibleEnumMirror.Tests;

public sealed class ExtensibleEnumMirrorGeneratorTests
{
    private const string FixtureExtensibleEnumStruct = """
        namespace TestNs
        {
            public readonly struct Reason : System.IEquatable<Reason>
            {
                private readonly string? _value;
                public Reason(string value) { _value = value; }
                public static Reason ContentFilter { get; } = new("content_filter");
                public static Reason MaxOutputTokens { get; } = new("max_output_tokens");
                public static bool operator ==(Reason l, Reason r) => l.Equals(r);
                public static bool operator !=(Reason l, Reason r) => !l.Equals(r);
                public bool Equals(Reason other) => string.Equals(_value, other._value, System.StringComparison.Ordinal);
                public override bool Equals(object? obj) => obj is Reason r && Equals(r);
                public override int GetHashCode() => _value?.GetHashCode() ?? 0;
            }
        }
        """;

    [Fact]
    public void Emits_KindEnum_And_Helpers_For_ExtensibleEnumStruct()
    {
        var source = $$"""
            using ANcpLua.Analyzers.ExtensibleEnumMirror;

            {{FixtureExtensibleEnumStruct}}

            namespace TestNs
            {
                [ExtensibleEnumMirror(typeof(Reason))]
                public partial class ReasonMirror { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<ExtensibleEnumMirrorGenerator>(source);

        var generated = result.RunResult.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("ReasonMirror.ExtensibleEnumMirror.g.cs", System.StringComparison.Ordinal))
            .ToString();

        generated.Should()
            .Contain("namespace TestNs;")
            .And.Contain("partial class ReasonMirror")
            .And.Contain("public enum Kind")
            .And.Contain("Unknown = 0,")
            .And.Contain("ContentFilter = 1,")
            .And.Contain("MaxOutputTokens = 2,")
            .And.Contain("public static Kind ToKind(global::TestNs.Reason value)")
            .And.Contain("if (value == global::TestNs.Reason.ContentFilter) return Kind.ContentFilter;")
            .And.Contain("public static global::TestNs.Reason ToStruct(Kind kind)")
            .And.Contain("if (kind == Kind.MaxOutputTokens) return global::TestNs.Reason.MaxOutputTokens;")
            .And.Contain("KnownValues")
            .And.Contain("global::TestNs.Reason.ContentFilter, global::TestNs.Reason.MaxOutputTokens");

        result.RunResult.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Output_Compilation_Has_No_Errors()
    {
        var source = $$"""
            using ANcpLua.Analyzers.ExtensibleEnumMirror;

            {{FixtureExtensibleEnumStruct}}

            namespace TestNs
            {
                [ExtensibleEnumMirror(typeof(Reason))]
                public partial class ReasonMirror { }

                public static class Sample
                {
                    public static string Describe(Reason r) => ReasonMirror.ToKind(r) switch
                    {
                        ReasonMirror.Kind.ContentFilter => "filter",
                        ReasonMirror.Kind.MaxOutputTokens => "tokens",
                        _ => "unknown"
                    };
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<ExtensibleEnumMirrorGenerator>(source);

        var errors = result.OutputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        errors.Should().BeEmpty("the generated mirror must compile cleanly alongside consumer code");
    }

    [Fact]
    public void Reports_AL0200_When_Marker_Is_Not_Partial()
    {
        var source = $$"""
            using ANcpLua.Analyzers.ExtensibleEnumMirror;

            {{FixtureExtensibleEnumStruct}}

            namespace TestNs
            {
                [ExtensibleEnumMirror(typeof(Reason))]
                public class ReasonMirror { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<ExtensibleEnumMirrorGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0200");
    }

    [Fact]
    public void Reports_AL0202_When_Target_Is_Not_Extensible_Enum_Struct()
    {
        var source = """
            using ANcpLua.Analyzers.ExtensibleEnumMirror;

            namespace TestNs
            {
                public class NotAStruct { }

                [ExtensibleEnumMirror(typeof(NotAStruct))]
                public partial class WrongMirror { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<ExtensibleEnumMirrorGenerator>(source);

        result.RunResult.Diagnostics.Should().Contain(d => d.Id == "AL0202");
    }

    [Fact]
    public void Emits_Mirror_With_Only_Unknown_When_Target_Has_No_Known_Values()
    {
        var source = """
            using ANcpLua.Analyzers.ExtensibleEnumMirror;

            namespace TestNs
            {
                public readonly struct Empty : System.IEquatable<Empty>
                {
                    public bool Equals(Empty other) => true;
                    public override bool Equals(object? obj) => obj is Empty;
                    public override int GetHashCode() => 0;
                    public static bool operator ==(Empty l, Empty r) => true;
                    public static bool operator !=(Empty l, Empty r) => false;
                }

                [ExtensibleEnumMirror(typeof(Empty))]
                public partial class EmptyMirror { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator<ExtensibleEnumMirrorGenerator>(source);

        var generated = result.RunResult.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("EmptyMirror.ExtensibleEnumMirror.g.cs", System.StringComparison.Ordinal))
            .ToString();

        generated.Should()
            .Contain("Unknown = 0,")
            .And.NotContain(" = 1,");

        result.RunResult.Diagnostics.Should().BeEmpty();
    }
}

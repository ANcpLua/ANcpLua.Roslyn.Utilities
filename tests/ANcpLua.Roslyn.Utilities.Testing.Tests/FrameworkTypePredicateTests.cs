using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class FrameworkTypePredicateTests
{
    [Fact]
    public void IsTaskType_AcceptsBuiltinTaskAndValueTask()
    {
        var methods = GetMethods("""
            using System.Threading.Tasks;
            public class Subject
            {
                public Task M0() => Task.CompletedTask;
                public Task<int> M1() => Task.FromResult(0);
                public ValueTask M2() => default;
                public ValueTask<int> M3() => default;
            }
            """);

        methods["M0"].ReturnType.IsTaskType().Should().BeTrue();
        methods["M1"].ReturnType.IsTaskType().Should().BeTrue();
        methods["M2"].ReturnType.IsTaskType().Should().BeTrue();
        methods["M3"].ReturnType.IsTaskType().Should().BeTrue();
    }

    [Fact]
    public void IsTaskType_RejectsUserDefinedTypeNamedTask()
    {
        var methods = GetMethods("""
            namespace Probe
            {
                public class Task { }
                public class Task<T> { }
                public class Subject
                {
                    public Task M() => null!;
                    public Task<int> N() => null!;
                }
            }
            """, subjectMetadataName: "Probe.Subject");

        methods["M"].ReturnType.IsTaskType().Should().BeFalse();
        methods["N"].ReturnType.IsTaskType().Should().BeFalse();
    }

    [Fact]
    public void IsSpanType_AcceptsSpanAndReadOnlySpan_RejectsUnrelatedTypes()
    {
        var methods = GetMethods("""
            using System;
            public class Subject
            {
                public Span<byte> A() => default;
                public ReadOnlySpan<byte> B() => default;
                public string C() => "";
                public int D() => 0;
            }
            """);

        methods["A"].ReturnType.IsSpanType().Should().BeTrue();
        methods["B"].ReturnType.IsSpanType().Should().BeTrue();
        methods["C"].ReturnType.IsSpanType().Should().BeFalse();
        methods["D"].ReturnType.IsSpanType().Should().BeFalse();
    }

    [Fact]
    public void IsMemoryType_AcceptsMemoryAndReadOnlyMemory_RejectsUnrelatedTypes()
    {
        var methods = GetMethods("""
            using System;
            public class Subject
            {
                public Memory<byte> A() => default;
                public ReadOnlyMemory<byte> B() => default;
                public string C() => "";
            }
            """);

        methods["A"].ReturnType.IsMemoryType().Should().BeTrue();
        methods["B"].ReturnType.IsMemoryType().Should().BeTrue();
        methods["C"].ReturnType.IsMemoryType().Should().BeFalse();
    }

    [Fact]
    public void GetElementType_ReturnsTypeArgumentForRecognizedContainersOnly()
    {
        var methods = GetMethods("""
            using System;
            using System.Collections.Generic;
            public class Subject
            {
                public Span<int> A() => default;
                public Memory<int> B() => default;
                public IEnumerable<int> C() => null!;
                public List<int> D() => null!;
                public string E() => "";
            }
            """);

        methods["A"].ReturnType.GetElementType()!.SpecialType.Should().Be(SpecialType.System_Int32);
        methods["B"].ReturnType.GetElementType()!.SpecialType.Should().Be(SpecialType.System_Int32);
        methods["C"].ReturnType.GetElementType()!.SpecialType.Should().Be(SpecialType.System_Int32);

        // List<T> implements IEnumerable<T> but is not itself IEnumerable<T>; GetElementType should
        // return null per the documented "containers themselves, not implementers" contract.
        methods["D"].ReturnType.GetElementType().Should().BeNull();
        methods["E"].ReturnType.GetElementType().Should().BeNull();
    }

    [Fact]
    public void GetTaskResultType_ReturnsArgumentForGenericTasksOnly()
    {
        var methods = GetMethods("""
            using System.Threading.Tasks;
            public class Subject
            {
                public Task M0() => Task.CompletedTask;
                public Task<int> M1() => Task.FromResult(0);
                public ValueTask M2() => default;
                public ValueTask<string> M3() => default;
            }
            """);

        methods["M0"].ReturnType.GetTaskResultType().Should().BeNull();
        methods["M1"].ReturnType.GetTaskResultType()!.SpecialType.Should().Be(SpecialType.System_Int32);
        methods["M2"].ReturnType.GetTaskResultType().Should().BeNull();
        methods["M3"].ReturnType.GetTaskResultType()!.SpecialType.Should().Be(SpecialType.System_String);
    }

    private static Dictionary<string, IMethodSymbol> GetMethods(string source, string subjectMetadataName = "Subject")
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "FrameworkTypeProbe",
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Span<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Memory<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var subject = compilation.GetTypeByMetadataName(subjectMetadataName)
                      ?? throw new InvalidOperationException(
                          $"the test compilation should expose '{subjectMetadataName}'");

        return subject.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .ToDictionary(m => m.Name);
    }
}

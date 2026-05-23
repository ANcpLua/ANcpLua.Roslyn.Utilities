using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TypeCacheTests
{
    [Fact]
    public void Get_IsThreadSafeUnderConcurrentAccess()
    {
        var compilation = CreateCompilation("public sealed class Probe { }");
        var resolveCalls = 0;

        var cache = new TypeCache<ProbeType>(type =>
        {
            Interlocked.Increment(ref resolveCalls);
            return type is ProbeType.ObjectType
                ? compilation.GetTypeByMetadataName("System.Object")
                : null;
        });

        var workerCount = Environment.ProcessorCount * 8;
        Parallel.For(0, workerCount,
            _ => cache.Get(ProbeType.ObjectType).Should().NotBeNull());

        resolveCalls.Should().Be(1);
        var first = cache.Get(ProbeType.ObjectType);
        var second = cache.Get(ProbeType.ObjectType);
        first.Should().BeSameAs(second);
    }

    private enum ProbeType
    {
        Unknown = 0,
        ObjectType = 1
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TypeCacheProbe",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        return compilation;
    }
}

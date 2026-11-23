using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

// The analyzer runners compile test code against the .NET 11 reference assemblies by default: a C# 15 union
// needs the framework's UnionAttribute and IUnion, which no earlier reference set carries.
public sealed class AnalyzerTestReferenceTests : AnalyzerTest<EmptyDiagnosticAnalyzer>
{
    [Fact]
    public Task DefaultReferences_AreNet11() => VerifyAsync("""
        public sealed record Cat(string Name);
        public sealed record Dog(int Age);
        public union Pet(Cat, Dog);
        """);
}

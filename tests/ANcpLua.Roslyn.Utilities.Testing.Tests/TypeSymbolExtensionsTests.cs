using System.Linq;
using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class TypeSymbolExtensionsTests
{
    [Fact]
    public void InheritsFrom_WithResolvedSymbol_UsesSymbolIdentity()
    {
        var compilation = CreateCompilation(SymbolShapesSource);

        var userType = compilation.GetTypeByMetadataName("Probe.UserType");
        var baseTypeA = compilation.GetTypeByMetadataName("NamespaceA.Base");
        var baseTypeB = compilation.GetTypeByMetadataName("NamespaceB.Base");

        userType.Should().NotBeNull();
        baseTypeA.Should().NotBeNull();
        baseTypeB.Should().NotBeNull();

        var resolvedUserType = RequireSymbol(userType);
        resolvedUserType.InheritsFrom(baseTypeA).Should().BeTrue();
        resolvedUserType.InheritsFrom(baseTypeB).Should().BeFalse();
        resolvedUserType.InheritsFromName("Base").Should().BeTrue();
        resolvedUserType.InheritsFromName("NamespaceA.Base").Should().BeTrue();
        resolvedUserType.InheritsFromName("NamespaceB.Base").Should().BeFalse();
    }

    [Fact]
    public void Implements_WithResolvedSymbol_UsesSymbolIdentity()
    {
        var compilation = CreateCompilation(SymbolShapesSource);

        var implType = RequireSymbol(compilation.GetTypeByMetadataName("Probe.UserType"));

        implType.Implements(compilation.GetTypeByMetadataName("NamespaceA.IService")).Should().BeTrue();
        implType.Implements(compilation.GetTypeByMetadataName("NamespaceA.IHandler`1")).Should().BeTrue();
        implType.Implements(compilation.GetTypeByMetadataName("NamespaceB.IService")).Should().BeFalse();
    }

    private const string SymbolShapesSource = """
namespace NamespaceA
{
    public class Base { }
    public interface IService { }
    public interface IHandler<T> { }
}

namespace NamespaceB
{
    public class Base { }
    public interface IService { }
}

namespace Probe
{
    public class UserType : NamespaceA.Base, NamespaceA.IService, NamespaceA.IHandler<int> { }
}
""";

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "SymbolShapes",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
        errors.Should().BeEmpty();
        return compilation;
    }

    private static INamedTypeSymbol RequireSymbol(INamedTypeSymbol? symbol)
    {
        return symbol ?? throw new InvalidOperationException("Expected test symbol to resolve.");
    }
}

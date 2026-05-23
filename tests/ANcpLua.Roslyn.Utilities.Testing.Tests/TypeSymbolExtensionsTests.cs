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

        var serviceType = compilation.GetTypeByMetadataName("NamespaceA.IService");
        var matchingServiceType = compilation.GetTypeByMetadataName("NamespaceA.IService");
        var otherServiceType = compilation.GetTypeByMetadataName("NamespaceB.IService");
        var implType = compilation.GetTypeByMetadataName("Probe.UserType");

        serviceType.Should().NotBeNull();
        matchingServiceType.Should().NotBeNull();
        otherServiceType.Should().NotBeNull();
        implType.Should().NotBeNull();

        var resolvedImplType = RequireSymbol(implType);
        var resolvedMatchingServiceType = RequireSymbol(matchingServiceType);
        var resolvedOtherServiceType = RequireSymbol(otherServiceType);
        resolvedImplType.Implements(serviceType).Should().BeTrue();
        resolvedImplType.Implements(resolvedMatchingServiceType).Should().BeTrue();
        resolvedImplType.Implements(resolvedOtherServiceType).Should().BeFalse();
    }

    private const string SymbolShapesSource = """
namespace NamespaceA
{
    public class Base { }
    public interface IService { }
}

namespace NamespaceB
{
    public class Base { }
    public interface IService { }
}

namespace Probe
{
    public class UserType : NamespaceA.Base, NamespaceA.IService { }

    public class ServiceConsumer : NamespaceB.IService { }
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

using ANcpLua.Roslyn.Utilities.Matching;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class MatchingDslTests
{
    [Theory]
    [InlineData("Base")]
    [InlineData("Probe.Base")]
    [InlineData("global::Probe.Base")]
    public void TypeMatcher_InheritsFromName_MatchesSimpleAndQualifiedNames(string baseTypeName)
    {
        var compilation = CreateCompilation(TypeShapeSource);
        var derived = compilation.GetTypeByMetadataName("Probe.Derived");

        derived.Should().NotBeNull();
        Match.Type().InheritsFrom(baseTypeName).Matches(derived!).Should().BeTrue();
    }

    [Theory]
    [InlineData("IBase")]
    [InlineData("Probe.IBase")]
    [InlineData("global::Probe.IBase")]
    public void TypeMatcher_Implements_MatchesSimpleAndQualifiedNames(string interfaceName)
    {
        var compilation = CreateCompilation(TypeShapeSource);
        var derived = compilation.GetTypeByMetadataName("Probe.Derived");

        derived.Should().NotBeNull();
        Match.Type().Implements(interfaceName).Matches(derived!).Should().BeTrue();
    }

    [Theory]
    [InlineData("Derived")]
    [InlineData("Probe.Derived")]
    [InlineData("global::Probe.Derived")]
    public void InvocationMatcher_OnType_MatchesSimpleAndQualifiedReceiverNames(string receiverTypeName)
    {
        var invocation = GetInvocation(TypeShapeSource, "Touch");

        Invoke.Method("Touch").OnType(receiverTypeName).Matches(invocation).Should().BeTrue();
    }

    [Theory]
    [InlineData("IBase")]
    [InlineData("Probe.IBase")]
    [InlineData("global::Probe.IBase")]
    public void InvocationMatcher_OnTypeImplementing_UsesSharedInterfaceMatching(string interfaceName)
    {
        var invocation = GetInvocation(TypeShapeSource, "Touch");

        Invoke.Method("Touch").OnTypeImplementing(interfaceName).Matches(invocation).Should().BeTrue();
    }

    [Fact]
    public void InvocationMatcher_ReturningTask_DoesNotMatchCustomTaskLikeType()
    {
        var fakeTaskInvocation = GetInvocation(TaskShapeSource, "Fake");
        var realTaskInvocation = GetInvocation(TaskShapeSource, "Real");

        Invoke.Method("Fake").ReturningTask().Matches(fakeTaskInvocation).Should().BeFalse();
        Invoke.Method("Real").ReturningTask().Matches(realTaskInvocation).Should().BeTrue();
    }

    private const string TypeShapeSource = """
                                           namespace Probe
                                           {
                                               public interface IBase { }
                                               public interface IDerived : IBase { }
                                               public class Base { }
                                               public class Derived : Base, IDerived
                                               {
                                                   public void Touch() { }
                                               }

                                               public class Runner
                                               {
                                                   public void Run()
                                                   {
                                                       var value = new Derived();
                                                       value.Touch();
                                                   }
                                               }
                                           }
                                           """;

    private const string TaskShapeSource = """
                                           namespace Probe
                                           {
                                               public sealed class Task { }

                                               public sealed class Runner
                                               {
                                                   public Task Fake() => null!;
                                                   public System.Threading.Tasks.Task Real() => null!;

                                                   public void Run()
                                                   {
                                                       Fake();
                                                       Real();
                                                   }
                                               }
                                           }
                                           """;

    private static IInvocationOperation GetInvocation(string source, string methodName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CreateCompilation(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocation = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation => invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == methodName,
                MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } => name == methodName,
                _ => false
            });

        return semanticModel.GetOperation(invocation).Should().BeAssignableTo<IInvocationOperation>().Subject;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CreateCompilation(CSharpSyntaxTree.ParseText(source));
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        var compilation = CSharpCompilation.Create(
            "MatchingProbe",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        return compilation;
    }
}

using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class InvocationExtensionsTests
{
    [Fact]
    public void HasCancellationTokenParameter_UsesSymbolIdentity()
    {
        var invocations = GetInvocations(InvocationSource);

        invocations["WithSystemToken"].HasCancellationTokenParameter().Should().BeTrue();
        invocations["WithAliasToken"].HasCancellationTokenParameter().Should().BeTrue();
        invocations["WithCustomToken"].HasCancellationTokenParameter().Should().BeFalse();
    }

    [Fact]
    public void IsCancellationTokenPassed_UsesSymbolIdentityWhenTokenIsPassed()
    {
        var invocations = GetInvocations(InvocationSource);

        invocations["WithSystemToken"].IsCancellationTokenPassed().Should().BeTrue();
        invocations["WithAliasToken"].IsCancellationTokenPassed().Should().BeTrue();
        invocations["WithCustomToken"].IsCancellationTokenPassed().Should().BeFalse();
    }

    private static System.Collections.Generic.Dictionary<string, IInvocationOperation> GetInvocations(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "InvocationProbe",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Should().NotContain(x => x.Severity == DiagnosticSeverity.Error);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocations = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(syntax => syntax.Expression switch
            {
                IdentifierNameSyntax => true,
                MemberAccessExpressionSyntax => true,
                _ => false
            })
            .Select(syntax => semanticModel.GetOperation(syntax).Should().BeAssignableTo<IInvocationOperation>().Subject)
            .ToDictionary(
                op => (op.Syntax as InvocationExpressionSyntax)?.Expression switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                    _ => string.Empty
                },
                op => op);

        invocations.Should().NotBeEmpty();
        invocations.Count.Should().Be(3);
        return invocations;
    }

    private const string InvocationSource = """
using System;
using System.Threading;
using Alias = System.Threading.CancellationToken;

namespace Probe
{
    public sealed class CustomCancellationToken { }

    public sealed class Subject
    {
        public void WithSystemToken(CancellationToken token) { }

        public void WithAliasToken(Alias token) { }

        public void WithCustomToken(CustomCancellationToken token) { }

        public void Run()
        {
            var token = new CancellationToken();
            var aliasToken = new Alias();

            WithSystemToken(token);
            WithAliasToken(aliasToken);
            WithCustomToken(new CustomCancellationToken());
        }
    }
}
""";
}

using ANcpLua.Roslyn.Utilities;
using ANcpLua.Roslyn.Utilities.Models;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class DiagnosticFlowTests
{
    [Fact]
    public void HasErrors_IgnoresDefaultDiagnosticInfo()
    {
        var flow = DiagnosticFlow.Fail<string>(default(DiagnosticInfo));

        flow.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Collect_IgnoresDefaultDiagnosticInfoWhenDeterminingFailure()
    {
        var result = DiagnosticFlow.Collect(
        [
            DiagnosticFlow.Ok("kept"),
            DiagnosticFlow.Fail<string>(default(DiagnosticInfo))
        ]);

        result.HasErrors.Should().BeFalse();
        result.Value.Should().Equal("kept");
    }

    [Fact]
    public void Try_PropagatesOperationCanceledException()
    {
        Action act = () => DiagnosticFlow.Try<string>(
            static () => throw new OperationCanceledException(),
            static _ => default);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void DiagnosticInfo_ToDiagnostic_RejectsDefaultValueClearly()
    {
        Action act = () => default(DiagnosticInfo).ToDiagnostic();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot convert default DiagnosticInfo to a Diagnostic.");
    }

    [Fact]
    public void HasErrors_DetectsErrorSeverityDiagnostic()
    {
        var descriptor = new DiagnosticDescriptor(
            "TEST001",
            "Title",
            "Message",
            "Testing",
            DiagnosticSeverity.Error,
            true);
        var diagnostic = DiagnosticInfo.Create(descriptor, Location.None);

        var flow = DiagnosticFlow.Fail<string>(diagnostic);

        flow.HasErrors.Should().BeTrue();
    }
}

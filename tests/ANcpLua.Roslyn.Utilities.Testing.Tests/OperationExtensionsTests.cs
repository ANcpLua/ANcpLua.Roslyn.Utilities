using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class OperationExtensionsTests
{
    [Fact]
    public void IsInsideTryBlock_ReturnsTrueForTryCatchAndFinallyScopes()
    {
        var marks = GetMarkedInvocations();

        marks["try"].IsInsideTryBlock().Should().BeTrue();
        marks["catch"].IsInsideTryBlock().Should().BeTrue();
        marks["finally"].IsInsideTryBlock().Should().BeTrue();
        marks["outside"].IsInsideTryBlock().Should().BeFalse();
    }

    [Fact]
    public void IsInsideCatchBlock_ReturnsTrueOnlyInCatchScopes()
    {
        var marks = GetMarkedInvocations();

        marks["catch"].IsInsideCatchBlock().Should().BeTrue();
        marks["try"].IsInsideCatchBlock().Should().BeFalse();
        marks["finally"].IsInsideCatchBlock().Should().BeFalse();
        marks["outside"].IsInsideCatchBlock().Should().BeFalse();
    }

    [Fact]
    public void IsInsideFinallyBlock_ReturnsTrueOnlyInFinallyScopes()
    {
        var marks = GetMarkedInvocations();

        marks["finally"].IsInsideFinallyBlock().Should().BeTrue();
        marks["try"].IsInsideFinallyBlock().Should().BeFalse();
        marks["catch"].IsInsideFinallyBlock().Should().BeFalse();
        marks["outside"].IsInsideFinallyBlock().Should().BeFalse();
    }

    [Fact]
    public void IsInsideLoopLockAndUsing_ReturnExpectedScopes()
    {
        var marks = GetMarkedInvocations();

        marks["loop"].IsInsideLoop().Should().BeTrue();
        marks["outside"].IsInsideLoop().Should().BeFalse();
        marks["lock"].IsInsideLockStatement().Should().BeTrue();
        marks["outside"].IsInsideLockStatement().Should().BeFalse();
        marks["using"].IsInsideUsingStatement().Should().BeTrue();
        marks["outside"].IsInsideUsingStatement().Should().BeFalse();
    }

    private static Dictionary<string, IInvocationOperation> GetMarkedInvocations()
    {
        const string source = """
                              using System;

                              sealed class Disposable : IDisposable
                              {
                                  public void Dispose() { }
                              }

                              sealed class Probe
                              {
                                  private static readonly object Gate = new();

                                  private static void Mark(string name) { }

                                  public void Run()
                                  {
                                      Mark("outside");

                                      for (var i = 0; i < 1; i++)
                                      {
                                          Mark("loop");
                                      }

                                      try
                                      {
                                          Mark("try");
                                      }
                                      catch (Exception)
                                      {
                                          Mark("catch");
                                      }
                                      finally
                                      {
                                          Mark("finally");
                                      }

                                      lock (Gate)
                                      {
                                          Mark("lock");
                                      }

                                      using (var disposable = new Disposable())
                                      {
                                          Mark("using");
                                      }
                                  }
                              }
                              """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "OperationProbe",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocations = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(static invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "Mark" });

        var result = new Dictionary<string, IInvocationOperation>(StringComparer.Ordinal);
        foreach (var invocation in invocations)
        {
            var operation = semanticModel.GetOperation(invocation).Should().BeAssignableTo<IInvocationOperation>().Subject;
            var marker = ((ILiteralOperation)operation.Arguments[0].Value).ConstantValue.Value.Should().BeOfType<string>().Subject;
            result.Add(marker, operation);
        }

        return result;
    }
}

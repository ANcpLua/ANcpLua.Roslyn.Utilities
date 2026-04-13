// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.Generators.UnitTests/SyntaxTreeFluentExtensions.cs

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace Noty.Workflows.Tests;

// Fluent assertions over a generated SyntaxTree. Read the assertions as a spec
// for what the incremental generator is expected to emit:
//
//   generatedTree.Should()
//                .AddHandler<FooMessage, BarResult>("this.HandleFooAsync")
//                .And.RegisterSentMessageType<BarResult>()
//                .And.RegisterYieldedOutputType<FinalResult>()
//                .And.HaveHierarchy("OuterClass", "InnerExecutor");
internal sealed class SyntaxTreeAssertions : ObjectAssertions<SyntaxTree, SyntaxTreeAssertions>
{
    private readonly string _syntaxString;

    public SyntaxTreeAssertions(SyntaxTree instance, AssertionChain assertionChain) : base(instance, assertionChain)
        => this._syntaxString = instance.ToString();

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName)
        => this.Match($".AddHandler({handlerName})", $"expected handler {handlerName} to be registered");

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName, string inTypeParam)
        => this.Match($".AddHandler<{inTypeParam}>({handlerName})", $"expected handler {handlerName} to be registered");

    public AndConstraint<SyntaxTreeAssertions> AddHandler(string handlerName, string inTypeParam, string outTypeParam)
        => this.Match($".AddHandler<{inTypeParam},{outTypeParam}>({handlerName})", $"expected handler {handlerName} to be registered");

    public AndConstraint<SyntaxTreeAssertions> AddHandler<TIn>(string handlerName, bool globalQualified = false)
        => this.AddHandler(handlerName, TypeParam<TIn>(globalQualified));

    public AndConstraint<SyntaxTreeAssertions> AddHandler<TIn, TOut>(string handlerName, bool globalQualified = false)
        => this.AddHandler(handlerName, TypeParam<TIn>(globalQualified), TypeParam<TOut>(globalQualified));

    public AndConstraint<SyntaxTreeAssertions> HaveNoHandlers()
        => this.MatchAbsent(".AddHandler(", "expected no handlers to be registered");

    public AndConstraint<SyntaxTreeAssertions> RegisterSentMessageType(string messageTypeParam)
        => this.Match($".SendsMessage<{messageTypeParam}>()", $"expected message type {messageTypeParam} to be registered");

    public AndConstraint<SyntaxTreeAssertions> RegisterSentMessageType<TMessage>(bool globalQualified = true)
        => this.RegisterSentMessageType(TypeParam<TMessage>(globalQualified));

    public AndConstraint<SyntaxTreeAssertions> NotRegisterSentMessageTypes()
        => this.MatchAbsent(".SendsMessage<", "expected no message types to be registered");

    public AndConstraint<SyntaxTreeAssertions> RegisterYieldedOutputType(string outputTypeParam)
        => this.Match($".YieldsOutput<{outputTypeParam}>()", $"expected output type {outputTypeParam} to be registered");

    public AndConstraint<SyntaxTreeAssertions> RegisterYieldedOutputType<TOutput>(bool globalQualified = true)
        => this.RegisterYieldedOutputType(TypeParam<TOutput>(globalQualified));

    public AndConstraint<SyntaxTreeAssertions> NotRegisterYieldedOutputTypes()
        => this.MatchAbsent(".YieldsOutput<", "expected no output types to be registered");

    public AndConstraint<SyntaxTreeAssertions> HaveNamespace()
        => this.Match("namespace ", "expected namespace declaration");

    public AndConstraint<SyntaxTreeAssertions> NotHaveNamespace()
        => this.MatchAbsent("namespace ", "expected no namespace declaration");

    public AndConstraint<SyntaxTreeAssertions> HaveHierarchy(params string[] expectedNesting)
    {
        if (expectedNesting.Length == 0)
        {
            return new(this);
        }

        int[] indicies = new int[expectedNesting.Length];
        for (int i = 0; i < expectedNesting.Length; i++)
        {
            indicies[i] = this._syntaxString.IndexOf($"partial class {expectedNesting[i]}", StringComparison.Ordinal);
        }

        var runningResult = this.Contain(0, indicies[0], expectedNesting[0]);
        for (int i = 1; i < expectedNesting.Length; i++)
        {
            runningResult = runningResult.And.Contain(i, indicies[i], expectedNesting[i])
                                         .And.InOrder(indicies[i - 1], indicies[i], expectedNesting[i - 1], expectedNesting[i]);
        }
        return runningResult;
    }

    private AndConstraint<SyntaxTreeAssertions> Match(string expected, string reason)
    {
        this.CurrentAssertionChain
            .ForCondition(this._syntaxString.Contains(expected))
            .BecauseOf(reason)
            .FailWith("Expected {context} to contain {0}{reason}, but it was not found. Actual syntax: {1}", expected, this._syntaxString);
        return new(this);
    }

    private AndConstraint<SyntaxTreeAssertions> MatchAbsent(string needle, string reason)
    {
        this.CurrentAssertionChain
            .ForCondition(!this._syntaxString.Contains(needle))
            .BecauseOf(reason)
            .FailWith("Expected {context} to not contain {0}{reason}. Actual syntax: {1}", needle, this._syntaxString);
        return new(this);
    }

    private AndConstraint<SyntaxTreeAssertions> Contain(int level, int index, string className)
    {
        this.CurrentAssertionChain
            .ForCondition(index > 0)
            .BecauseOf($"expected \"partial class {className}\" at nesting level {level}")
            .FailWith("Expected {context} to contain partial class {0} at level {1}{reason}. Actual syntax: {2}", className, level, this._syntaxString);
        return new(this);
    }

    private AndConstraint<SyntaxTreeAssertions> InOrder(int prev, int curr, string prevClass, string currClass)
    {
        this.CurrentAssertionChain
            .ForCondition(prev < curr)
            .BecauseOf($"expected \"partial class {prevClass}\" before \"partial class {currClass}\"")
            .FailWith("Expected {context} to declare {0} before {1}{reason}. Actual syntax: {2}", prevClass, currClass, this._syntaxString);
        return new(this);
    }

    private static string TypeParam<T>(bool globalQualified)
    {
        var type = typeof(T);
        return globalQualified ? $"global::{type.FullName}" : type.Name;
    }
}

internal static class SyntaxTreeFluentExtensions
{
    public static SyntaxTreeAssertions Should(this SyntaxTree syntaxTree) => new(syntaxTree, AssertionChain.GetOrCreate());
}

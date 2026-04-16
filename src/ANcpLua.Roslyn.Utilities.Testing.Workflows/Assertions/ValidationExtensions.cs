// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/ValidationExtensions.cs

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Agents.AI.Workflows.Execution;
using Microsoft.Extensions.AI;

namespace ANcpLua.Roslyn.Utilities.Testing.Workflows;

// Expression validators for Moq.Verify(It.Is(prototype.CreateValidator())) calls.
// CreatePolyValidator rewrites the inner lambda's parameter so an EdgeInfo validator
// can be written in terms of a concrete sub-type but matched against the base.
internal static class ValidationExtensions
{
    public static Expression<Func<EdgeConnection, bool>> CreateValidator(this EdgeConnection prototype)
        => actual => actual.SourceIds.Count == prototype.SourceIds.Count
                  && actual.SinkIds.Count == prototype.SinkIds.Count
                  && prototype.SourceIds.SequenceEqual(actual.SourceIds)
                  && prototype.SinkIds.SequenceEqual(actual.SinkIds);

    public static Expression<Func<TypeId, bool>> CreateValidator(this TypeId? prototype)
        => actual => (prototype == null && actual == null)
                  || (prototype != null && actual != null
                      && actual.AssemblyName == prototype.AssemblyName
                      && actual.TypeName == prototype.TypeName);

    public static Expression<Func<ExecutorInfo, bool>> CreateValidator(this ExecutorInfo prototype)
        => actual => actual.ExecutorId == prototype.ExecutorId
                  && actual.ExecutorType.Equals(prototype.ExecutorType);

    public static Expression<Func<RequestPortInfo, bool>> CreatePortInfoValidator(this RequestPort prototype)
        => actual => actual.PortId == prototype.Id
                  && actual.RequestType.IsMatch(prototype.Request)
                  && actual.ResponseType.IsMatch(prototype.Response);

    public static Expression<Func<DirectEdgeInfo, bool>> CreateValidator(this DirectEdgeInfo prototype)
        => actual => actual.Connection == prototype.Connection
                  && actual.HasCondition == prototype.HasCondition;

    public static Expression<Func<FanOutEdgeInfo, bool>> CreateValidator(this FanOutEdgeInfo prototype)
        => actual => actual.Connection == prototype.Connection
                  && actual.HasAssigner == prototype.HasAssigner;

    public static Expression<Func<FanInEdgeInfo, bool>> CreateValidator(this FanInEdgeInfo prototype)
        => actual => actual.Connection == prototype.Connection;

    public static Expression<Func<ScopeId, bool>> CreateValidator(this ScopeId prototype)
        => actual => actual.ExecutorId == prototype.ExecutorId
                  && actual.ScopeName == prototype.ScopeName;

    public static Expression<Func<ScopeKey, bool>> CreateValidator(this ScopeKey prototype)
        => actual => actual.Key == prototype.Key
                  && actual.ScopeId.ScopeName == prototype.ScopeId.ScopeName
                  && actual.ScopeId.ExecutorId == prototype.ScopeId.ExecutorId;

    public static Expression<Func<ExecutorIdentity, bool>> CreateValidator(this ExecutorIdentity prototype)
        => actual => actual.Id == prototype.Id;

    public static Expression<Func<ExternalRequest, bool>> CreateValidator(this ExternalRequest prototype)
        => actual => actual.RequestId == prototype.RequestId
                  && actual.PortInfo == prototype.PortInfo
                  && actual.Data == prototype.Data;

    public static Expression<Func<ExternalResponse, bool>> CreateValidator(this ExternalResponse prototype)
        => actual => actual.RequestId == prototype.RequestId
                  && actual.Data == prototype.Data;

    public static Expression<Func<ChatMessage, bool>> CreateValidatorCheckingText(this ChatMessage prototype)
        => actual => actual.Role == prototype.Role
                  && actual.AuthorName == prototype.AuthorName
                  && actual.CreatedAt == prototype.CreatedAt
                  && actual.MessageId == prototype.MessageId
                  && actual.Text == prototype.Text;

    public static Expression<Func<EdgeInfo, bool>> CreatePolyValidator(this EdgeInfo prototype)
        => prototype.Kind switch
        {
            EdgeKind.Direct => Wrap((DirectEdgeInfo)prototype, prototype.Kind, CreateValidator),
            EdgeKind.FanOut => Wrap((FanOutEdgeInfo)prototype, prototype.Kind, CreateValidator),
            EdgeKind.FanIn => Wrap((FanInEdgeInfo)prototype, prototype.Kind, CreateValidator),
            _ => throw new NotSupportedException($"Unsupported edge type: {prototype.Kind}"),
        };

    private static Expression<Func<EdgeInfo, bool>> Wrap<TInner>(TInner prototype, EdgeKind kind, Func<TInner, Expression<Func<TInner, bool>>> factory)
        where TInner : EdgeInfo
    {
        var innerValidatorExpr = factory(prototype);
        Debug.Assert(innerValidatorExpr.Parameters.Count == 1, "Validator is of unexpected arity");

        var innerParam = innerValidatorExpr.Parameters[0];
        var outerParam = Expression.Parameter(typeof(EdgeInfo), "actual");
        var convertExpr = Expression.Convert(outerParam, typeof(TInner));

        var visitor = new SubstitutionVisitor(innerParam, convertExpr);
        var innerBody = visitor.Visit(innerValidatorExpr.Body);

        var body = Expression.AndAlso(
            Expression.AndAlso(
                Expression.Equal(Expression.Property(outerParam, nameof(EdgeInfo.Kind)), Expression.Constant(kind)),
                Expression.TypeIs(outerParam, typeof(TInner))),
            innerBody);

        return Expression.Lambda<Func<EdgeInfo, bool>>(body, outerParam);
    }
}

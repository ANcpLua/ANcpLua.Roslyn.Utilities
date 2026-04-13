// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/SubstitutionVisitor.cs

using System.Linq.Expressions;

namespace Noty.Workflows.Tests;

// Rewrites a ParameterExpression reference to a new sub-expression.
// Used by ValidationExtensions to splice a typed body into a polymorphic lambda.
internal sealed class SubstitutionVisitor(ParameterExpression parameter, Expression substitution) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node.Name == parameter.Name ? substitution : base.VisitParameter(node);
}

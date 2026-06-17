/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression</c>.</remarks>
public class ConditionalExpression : Expression
{
    private readonly Expression condition;
    private readonly Expression ifTrue;
    private readonly Expression ifFalse;

    public ConditionalExpression(Expression condition, Expression ifTrue, Expression ifFalse)
    {
        this.condition = condition;
        this.ifTrue = ifTrue;
        this.ifFalse = ifFalse;
    }

    public Expression Condition => condition;

    public Expression IfTrue => ifTrue;

    public Expression IfFalse => ifFalse;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitConditional(this);
    }
}

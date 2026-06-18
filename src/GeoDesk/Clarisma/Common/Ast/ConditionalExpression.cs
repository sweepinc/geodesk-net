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

    readonly Expression _condition;
    readonly Expression _ifTrue;
    readonly Expression _ifFalse;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression(Expression, Expression, Expression)</c>.</remarks>
    public ConditionalExpression(Expression condition, Expression ifTrue, Expression ifFalse)
    {
        _condition = condition;
        _ifTrue = ifTrue;
        _ifFalse = ifFalse;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.condition()</c>.</remarks>
    public Expression Condition => _condition;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.ifTrue()</c>.</remarks>
    public Expression IfTrue => _ifTrue;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.ifFalse()</c>.</remarks>
    public Expression IfFalse => _ifFalse;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitConditional(this);
    }

}

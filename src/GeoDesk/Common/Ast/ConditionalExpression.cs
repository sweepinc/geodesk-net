/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node representing a ternary conditional: evaluating <see cref="Condition"/> and
/// yielding either <see cref="IfTrue"/> or <see cref="IfFalse"/>. All three sub-expressions are immutable.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression</c>.</remarks>
internal class ConditionalExpression : Expression
{

    readonly Expression _condition;
    readonly Expression _ifTrue;
    readonly Expression _ifFalse;

    /// <summary>
    /// Creates a conditional expression that evaluates to <paramref name="ifTrue"/> when
    /// <paramref name="condition"/> holds, otherwise <paramref name="ifFalse"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression(Expression, Expression, Expression)</c>.</remarks>
    public ConditionalExpression(Expression condition, Expression ifTrue, Expression ifFalse)
    {
        _condition = condition;
        _ifTrue = ifTrue;
        _ifFalse = ifFalse;
    }

    /// <summary>
    /// The boolean condition that selects between the two branches.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.condition()</c>.</remarks>
    public Expression Condition => _condition;

    /// <summary>
    /// The expression evaluated when <see cref="Condition"/> is true.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.ifTrue()</c>.</remarks>
    public Expression IfTrue => _ifTrue;

    /// <summary>
    /// The expression evaluated when <see cref="Condition"/> is false.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.ifFalse()</c>.</remarks>
    public Expression IfFalse => _ifFalse;

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitConditional"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ConditionalExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitConditional(this);
    }

}

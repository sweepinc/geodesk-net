/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node applying a single <see cref="Operator"/> to one operand expression (for
/// example unary negation or logical NOT). The operator and operand are immutable and participate
/// in value equality.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression</c>.</remarks>
internal class UnaryExpression : Expression
{

    readonly Operator _op;
    readonly Expression _operand;

    /// <summary>
    /// Creates a unary expression applying <paramref name="op"/> to <paramref name="operand"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression(Operator, Expression)</c>.</remarks>
    public UnaryExpression(Operator op, Expression operand)
    {
        _op = op;
        _operand = operand;
    }

    /// <summary>
    /// The unary operator applied to the operand.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.operator()</c>.</remarks>
    public Operator Operator => _op;

    /// <summary>
    /// The single operand expression the operator is applied to.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.operand()</c>.</remarks>
    public Expression Operand => _operand;

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitUnary"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitUnary(this);
    }

    /// <summary>
    /// Value equality: two unary expressions are equal when their operator and operand are equal.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.equals(Object)</c>.</remarks>
    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        var that = (UnaryExpression)o;
        return Equals(_op, that._op) && Equals(_operand, that._operand);
    }

    /// <summary>
    /// Returns a hash code derived from the operator and operand, consistent with <see cref="Equals(object?)"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_op, _operand);
    }

}

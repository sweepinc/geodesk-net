/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression</c>.</remarks>
public class UnaryExpression : Expression
{

    readonly Operator _op;
    readonly Expression _operand;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression(Operator, Expression)</c>.</remarks>
    public UnaryExpression(Operator op, Expression operand)
    {
        _op = op;
        _operand = operand;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.operator()</c>.</remarks>
    public Operator Operator => _op;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.operand()</c>.</remarks>
    public Expression Operand => _operand;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitUnary(this);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.equals(Object)</c>.</remarks>
    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        var that = (UnaryExpression)o;
        return Equals(_op, that._op) && Equals(_operand, that._operand);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.UnaryExpression.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_op, _operand);
    }

}

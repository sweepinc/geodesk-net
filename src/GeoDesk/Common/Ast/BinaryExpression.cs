/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression</c>.</remarks>
internal class BinaryExpression : Expression
{

    readonly Operator _op;
    readonly Expression _left;
    readonly Expression _right;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression(Operator, Expression, Expression)</c>.</remarks>
    public BinaryExpression(Operator op, Expression left, Expression right)
    {
        _op = op;
        _left = left;
        _right = right;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.equals(Object)</c>.</remarks>
    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        var that = (BinaryExpression)o;
        return Equals(_op, that._op) && Equals(_left, that._left) && Equals(_right, that._right);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_op, _left, _right);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.operator()</c>.</remarks>
    public Operator Operator => _op;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.left()</c>.</remarks>
    public Expression Left => _left;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.right()</c>.</remarks>
    public Expression Right => _right;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitBinary(this);
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.BinaryExpression</c>.</remarks>
public class BinaryExpression : Expression
{
    private readonly Operator op;
    private readonly Expression left;
    private readonly Expression right;

    public BinaryExpression(Operator op, Expression left, Expression right)
    {
        this.op = op;
        this.left = left;
        this.right = right;
    }

    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        BinaryExpression that = (BinaryExpression)o;
        return Equals(op, that.op) && Equals(left, that.left) && Equals(right, that.right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(op, left, right);
    }

    public Operator Operator => op;

    public Expression Left => left;

    public Expression Right => right;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitBinary(this);
    }
}

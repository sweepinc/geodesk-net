/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

public class UnaryExpression : Expression
{
    private readonly Operator op;
    private readonly Expression operand;

    public UnaryExpression(Operator op, Expression operand)
    {
        this.op = op;
        this.operand = operand;
    }

    public Operator Operator => op;

    public Expression Operand => operand;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitUnary(this);
    }

    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        UnaryExpression that = (UnaryExpression)o;
        return Equals(op, that.op) && Equals(operand, that.operand);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(op, operand);
    }
}

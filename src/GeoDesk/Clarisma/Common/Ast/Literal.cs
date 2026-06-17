/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

public class Literal : Expression
{
    private readonly object? value;

    public Literal(object? value)
    {
        this.value = value;
    }

    public object? Value => value;

    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        Literal literal = (Literal)o;
        return Equals(value, literal.value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value);
    }

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitLiteral(this);
    }
}

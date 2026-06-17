/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

public class StringExpression : Expression
{
    private readonly Expression[] parts;

    public StringExpression(Expression[] parts)
    {
        this.parts = parts;
    }

    public Expression[] Parts => parts;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitString(this);
    }
}

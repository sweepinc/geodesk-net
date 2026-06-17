/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

public class Variable : Expression
{
    private readonly string name;

    public Variable(string name)
    {
        this.name = name;
    }

    public string Name => name;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitVariable(this);
    }
}

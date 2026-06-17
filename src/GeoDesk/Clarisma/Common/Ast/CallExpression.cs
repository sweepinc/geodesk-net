/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

public class CallExpression : Expression
{
    private readonly Expression callee;
    private readonly Expression[] arguments;

    public CallExpression(Expression callee, Expression[] arguments)
    {
        this.callee = callee;
        this.arguments = arguments;
    }

    public Expression Callee => callee;

    public Expression[] Arguments => arguments;

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitCall(this);
    }
}

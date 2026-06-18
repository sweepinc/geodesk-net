/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression</c>.</remarks>
internal class StringExpression : Expression
{

    readonly Expression[] _parts;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression(Expression[])</c>.</remarks>
    public StringExpression(Expression[] parts)
    {
        _parts = parts;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression.parts()</c>.</remarks>
    public Expression[] Parts => _parts;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitString(this);
    }

}

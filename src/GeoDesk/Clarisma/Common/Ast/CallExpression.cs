/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression</c>.</remarks>
internal class CallExpression : Expression
{

    readonly Expression _callee;
    readonly Expression[] _arguments;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression(Expression, Expression[])</c>.</remarks>
    public CallExpression(Expression callee, Expression[] arguments)
    {
        _callee = callee;
        _arguments = arguments;
    }

    /// <remarks>Exposes the Java field <c>com.clarisma.common.ast.CallExpression.callee</c> (no getter in Java).</remarks>
    public Expression Callee => _callee;

    /// <remarks>Exposes the Java field <c>com.clarisma.common.ast.CallExpression.arguments</c> (no getter in Java).</remarks>
    public Expression[] Arguments => _arguments;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitCall(this);
    }

}

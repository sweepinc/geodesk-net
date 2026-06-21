/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node representing a function or method invocation: a callee expression applied to
/// a fixed array of argument expressions. Both the callee and arguments are immutable.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression</c>.</remarks>
internal class CallExpression : Expression
{

    readonly Expression _callee;
    readonly Expression[] _arguments;

    /// <summary>
    /// Creates a call expression invoking <paramref name="callee"/> with the given
    /// <paramref name="arguments"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression(Expression, Expression[])</c>.</remarks>
    public CallExpression(Expression callee, Expression[] arguments)
    {
        _callee = callee;
        _arguments = arguments;
    }

    /// <summary>
    /// The expression that resolves to the function or method being invoked.
    /// </summary>
    /// <remarks>Exposes the Java field <c>com.clarisma.common.ast.CallExpression.callee</c> (no getter in Java).</remarks>
    public Expression Callee => _callee;

    /// <summary>
    /// The argument expressions passed to the call, in order.
    /// </summary>
    /// <remarks>Exposes the Java field <c>com.clarisma.common.ast.CallExpression.arguments</c> (no getter in Java).</remarks>
    public Expression[] Arguments => _arguments;

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitCall"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.CallExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitCall(this);
    }

}

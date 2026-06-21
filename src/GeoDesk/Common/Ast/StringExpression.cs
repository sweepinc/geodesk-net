/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node representing a string built from an ordered sequence of part expressions,
/// such as an interpolated or concatenated string whose segments are evaluated and joined.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression</c>.</remarks>
internal class StringExpression : Expression
{

    readonly Expression[] _parts;

    /// <summary>
    /// Creates a string expression from the given ordered <paramref name="parts"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression(Expression[])</c>.</remarks>
    public StringExpression(Expression[] parts)
    {
        _parts = parts;
    }

    /// <summary>
    /// The component expressions that are evaluated and concatenated to form the string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression.parts()</c>.</remarks>
    public Expression[] Parts => _parts;

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitString"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.StringExpression.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitString(this);
    }

}

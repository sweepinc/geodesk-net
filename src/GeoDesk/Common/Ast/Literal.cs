/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal</c>.</remarks>
internal class Literal : Expression
{

    readonly object? _value;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal(Object)</c>.</remarks>
    public Literal(object? value)
    {
        _value = value;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.value()</c>.</remarks>
    public object? Value => _value;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.equals(Object)</c>.</remarks>
    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        var literal = (Literal)o;
        return Equals(_value, literal._value);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_value);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitLiteral(this);
    }

}

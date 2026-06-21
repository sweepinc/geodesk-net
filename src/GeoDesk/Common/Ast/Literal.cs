/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node wrapping a constant value (such as a number, string, or boolean) known at
/// parse time. The wrapped value participates in value equality.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal</c>.</remarks>
internal class Literal : Expression
{

    readonly object? _value;

    /// <summary>
    /// Creates a literal wrapping the given constant <paramref name="value"/> (which may be null).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal(Object)</c>.</remarks>
    public Literal(object? value)
    {
        _value = value;
    }

    /// <summary>
    /// The wrapped constant value, or null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.value()</c>.</remarks>
    public object? Value => _value;

    /// <summary>
    /// Value equality: two literals are equal when their wrapped values are equal.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.equals(Object)</c>.</remarks>
    public override bool Equals(object? o)
    {
        if (ReferenceEquals(this, o)) return true;
        if (o == null || GetType() != o.GetType()) return false;
        var literal = (Literal)o;
        return Equals(_value, literal._value);
    }

    /// <summary>
    /// Returns a hash code derived from the wrapped value, consistent with <see cref="Equals(object?)"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(_value);
    }

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitLiteral"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Literal.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitLiteral(this);
    }

}

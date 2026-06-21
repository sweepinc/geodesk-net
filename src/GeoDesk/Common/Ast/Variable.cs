/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// An expression node referencing a named variable whose value is resolved at evaluation time
/// rather than being known at parse time.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable</c>.</remarks>
internal class Variable : Expression
{

    readonly string _name;

    /// <summary>
    /// Creates a variable reference with the given <paramref name="name"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable(String)</c>.</remarks>
    public Variable(string name)
    {
        _name = name;
    }

    /// <summary>
    /// The name of the referenced variable.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable.name()</c>.</remarks>
    public string Name => _name;

    /// <summary>
    /// Dispatches to <see cref="IAstVisitor{R}.VisitVariable"/> on the supplied visitor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitVariable(this);
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable</c>.</remarks>
internal class Variable : Expression
{

    readonly string _name;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable(String)</c>.</remarks>
    public Variable(string name)
    {
        _name = name;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable.name()</c>.</remarks>
    public string Name => _name;

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Variable.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        return visitor.VisitVariable(this);
    }

}

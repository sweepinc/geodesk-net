/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// Abstract base class for nodes of an abstract syntax tree (AST), the parsed form of an
/// expression. Concrete subclasses represent literals, variables, operators, calls, and so on,
/// and are traversed with the visitor pattern via <see cref="Accept{R}"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.Ast</c>.</remarks>
internal abstract class Ast
{

    /// <summary>
    /// Dispatches this node to the matching method on the supplied <see cref="IAstVisitor{R}"/>,
    /// returning whatever value that visitor produces. Implements the visitor pattern double-dispatch.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Ast.accept(AstVisitor)</c>.</remarks>
    public abstract R Accept<R>(IAstVisitor<R> visitor);

}

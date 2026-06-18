/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.Ast</c>.</remarks>
internal abstract class Ast
{

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Ast.accept(AstVisitor)</c>.</remarks>
    public abstract R Accept<R>(IAstVisitor<R> visitor);

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// Abstract base for all expression nodes in the AST — the subset of <see cref="Ast"/> nodes that
/// produce a value when evaluated. Concrete subclasses include literals, variables, calls, and
/// binary, unary, and conditional operators.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.Expression</c>.</remarks>
internal abstract class Expression : Ast
{

    // TODO: why is accept() not implemented here?

}

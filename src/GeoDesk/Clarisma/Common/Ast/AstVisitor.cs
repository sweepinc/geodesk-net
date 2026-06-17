/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor</c>.</remarks>
public interface IAstVisitor<R>
{
    R VisitExpression(Expression exp);
    R VisitBinary(BinaryExpression exp);
    R VisitUnary(UnaryExpression exp);
    R VisitString(StringExpression exp);
    R VisitLiteral(Literal exp);
    R VisitVariable(Variable exp);
    R VisitCall(CallExpression exp);
    R VisitConditional(ConditionalExpression exp);
}

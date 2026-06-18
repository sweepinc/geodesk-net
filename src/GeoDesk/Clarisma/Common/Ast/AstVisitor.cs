/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor</c>.</remarks>
internal interface IAstVisitor<R>
{

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitExpression(Expression)</c>.</remarks>
    R VisitExpression(Expression exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitBinary(BinaryExpression)</c>.</remarks>
    R VisitBinary(BinaryExpression exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitUnary(UnaryExpression)</c>.</remarks>
    R VisitUnary(UnaryExpression exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitString(StringExpression)</c>.</remarks>
    R VisitString(StringExpression exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitLiteral(Literal)</c>.</remarks>
    R VisitLiteral(Literal exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitVariable(Variable)</c>.</remarks>
    R VisitVariable(Variable exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitCall(CallExpression)</c>.</remarks>
    R VisitCall(CallExpression exp);

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitConditional(ConditionalExpression)</c>.</remarks>
    R VisitConditional(ConditionalExpression exp);

}

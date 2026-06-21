/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// Visitor over the nodes of an expression AST. Each <c>Visit*</c> method handles one concrete
/// <see cref="Expression"/> kind and returns a result of type <typeparamref name="R"/>, allowing
/// tree traversals (evaluation, code generation, serialization) to be expressed without type switches.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor</c>.</remarks>
internal interface IAstVisitor<R>
{

    /// <summary>
    /// Visits a generic <see cref="Expression"/> node that has no more specific visit method.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitExpression(Expression)</c>.</remarks>
    R VisitExpression(Expression exp);

    /// <summary>
    /// Visits a <see cref="BinaryExpression"/> (an operator applied to a left and right operand).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitBinary(BinaryExpression)</c>.</remarks>
    R VisitBinary(BinaryExpression exp);

    /// <summary>
    /// Visits a <see cref="UnaryExpression"/> (an operator applied to a single operand).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitUnary(UnaryExpression)</c>.</remarks>
    R VisitUnary(UnaryExpression exp);

    /// <summary>
    /// Visits a <see cref="StringExpression"/> (a string literal node).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitString(StringExpression)</c>.</remarks>
    R VisitString(StringExpression exp);

    /// <summary>
    /// Visits a <see cref="Literal"/> (a numeric or other constant value node).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitLiteral(Literal)</c>.</remarks>
    R VisitLiteral(Literal exp);

    /// <summary>
    /// Visits a <see cref="Variable"/> (a named reference resolved at evaluation time).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitVariable(Variable)</c>.</remarks>
    R VisitVariable(Variable exp);

    /// <summary>
    /// Visits a <see cref="CallExpression"/> (a function or method invocation with arguments).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitCall(CallExpression)</c>.</remarks>
    R VisitCall(CallExpression exp);

    /// <summary>
    /// Visits a <see cref="ConditionalExpression"/> (a ternary <c>condition ? then : else</c> node).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.AstVisitor.visitConditional(ConditionalExpression)</c>.</remarks>
    R VisitConditional(ConditionalExpression exp);

}

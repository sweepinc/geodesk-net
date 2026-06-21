/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

// PORT-BLOCKED: In Java this extends com.clarisma.common.bytecode.Coder and emits JVM
// bytecode (via ASM) to evaluate an expression AST at runtime. .NET has no direct
// equivalent; a future pass would reimplement this with System.Reflection.Emit (IL)
// or System.Linq.Expressions. The bytecode package (Coder, Instructions) is likewise
// not ported. All visitor methods throw until that work is done.
/// <summary>
/// Visitor intended to compile an expression AST into executable code. In Java this emitted JVM
/// bytecode via ASM; the .NET port is currently blocked (see the PORT-BLOCKED note) so every visit
/// method throws <see cref="NotImplementedException"/> until a Reflection.Emit or expression-tree
/// implementation is written.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder</c>.</remarks>
internal class ExpressionCoder : IAstVisitor<object?>
{

    protected TypeChecker? _typeChecker;

    /// <summary>
    /// Sets the <see cref="TypeChecker"/> used to determine expression result types during coding.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.setTypeChecker(TypeChecker)</c>.</remarks>
    public void SetTypeChecker(TypeChecker typeChecker)
    {
        this._typeChecker = typeChecker;
    }

    /// <summary>
    /// Creates the exception thrown by every visit method, signalling that runtime code generation
    /// is not yet ported to .NET.
    /// </summary>
    static Exception NotPortable() => new NotImplementedException("PORT-BLOCKED: ExpressionCoder requires runtime bytecode/IL generation (ASM in Java).");

    /// <summary>
    /// Would emit code for a binary expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitBinary(BinaryExpression)</c>.</remarks>
    public object? VisitBinary(BinaryExpression exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a unary expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitUnary(UnaryExpression)</c>.</remarks>
    public object? VisitUnary(UnaryExpression exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a string expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitString(StringExpression)</c>.</remarks>
    public object? VisitString(StringExpression exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a literal; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitLiteral(Literal)</c>.</remarks>
    public object? VisitLiteral(Literal exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a variable reference; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitVariable(Variable)</c>.</remarks>
    public object? VisitVariable(Variable exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a call expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitCall(CallExpression)</c>.</remarks>
    public object? VisitCall(CallExpression exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a conditional expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitConditional(ConditionalExpression)</c>.</remarks>
    public object? VisitConditional(ConditionalExpression exp) => throw NotPortable();

    /// <summary>
    /// Would emit code for a generic expression; currently throws because coding is not ported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder.visitExpression(Expression)</c>.</remarks>
    public object? VisitExpression(Expression exp) => throw NotPortable();

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

// PORT-BLOCKED: In Java this extends com.clarisma.common.bytecode.Coder and emits JVM
// bytecode (via ASM) to evaluate an expression AST at runtime. .NET has no direct
// equivalent; a future pass would reimplement this with System.Reflection.Emit (IL)
// or System.Linq.Expressions. The bytecode package (Coder, Instructions) is likewise
// not ported. All visitor methods throw until that work is done.
/// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionCoder</c>.</remarks>
public class ExpressionCoder : IAstVisitor<object?>
{
    protected TypeChecker? typeChecker;

    public void SetTypeChecker(TypeChecker typeChecker)
    {
        this.typeChecker = typeChecker;
    }

    private static Exception NotPortable() => new NotImplementedException(
        "PORT-BLOCKED: ExpressionCoder requires runtime bytecode/IL generation (ASM in Java).");

    public object? VisitBinary(BinaryExpression exp) => throw NotPortable();

    public object? VisitUnary(UnaryExpression exp) => throw NotPortable();

    public object? VisitString(StringExpression exp) => throw NotPortable();

    public object? VisitLiteral(Literal exp) => throw NotPortable();

    public object? VisitVariable(Variable exp) => throw NotPortable();

    public object? VisitCall(CallExpression exp) => throw NotPortable();

    public object? VisitConditional(ConditionalExpression exp) => throw NotPortable();

    public object? VisitExpression(Expression exp) => throw NotPortable();
}

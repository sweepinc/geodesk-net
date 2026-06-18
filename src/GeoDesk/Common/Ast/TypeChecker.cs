/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

// In Java the type argument is Class<?>; here it is System.Type.
/// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker</c>.</remarks>
internal class TypeChecker : IAstVisitor<Type?>
{

    // Renamed from Java's getType() to avoid colliding with object.GetType().
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.getType(Expression)</c>.</remarks>
    public Type? GetTypeOf(Expression exp)
    {
        return exp.Accept<Type?>(this);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitBinary(BinaryExpression)</c>.</remarks>
    public Type? VisitBinary(BinaryExpression exp)
    {
        var op = exp.Operator;
        if (op == Operator.ADD || op == Operator.OR)
        {
            return typeof(bool);
        }

        // TODO

        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitUnary(UnaryExpression)</c>.</remarks>
    public Type? VisitUnary(UnaryExpression exp)
    {
        var op = exp.Operator;
        if (op == Operator.NOT) return typeof(bool);
        return GetTypeOf(exp.Operand);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitString(StringExpression)</c>.</remarks>
    public Type? VisitString(StringExpression exp)
    {
        return typeof(string);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitLiteral(Literal)</c>.</remarks>
    public Type? VisitLiteral(Literal exp)
    {
        // TODO: should we return the primitive type for wrapper classes?
        var c = exp.Value!.GetType();
        if (c == typeof(double)) return typeof(double); // TODO
        return c;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.commonType(Class, Class)</c>.</remarks>
    protected Type? CommonType(Type? a, Type? b)
    {
        if (a == b) return a;
        if (a == null) return b;
        if (b == null) return a;
        return typeof(double);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.commonType(Expression, Expression)</c>.</remarks>
    protected Type? CommonType(Expression a, Expression b)
    {
        return CommonType(GetTypeOf(a), GetTypeOf(b));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitVariable(Variable)</c>.</remarks>
    public Type? VisitVariable(Variable exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitCall(CallExpression)</c>.</remarks>
    public Type? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitConditional(ConditionalExpression)</c>.</remarks>
    public Type? VisitConditional(ConditionalExpression exp)
    {
        return CommonType(GetTypeOf(exp.IfTrue), GetTypeOf(exp.IfFalse));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitExpression(Expression)</c>.</remarks>
    public Type? VisitExpression(Expression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

}

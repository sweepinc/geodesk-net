/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Ast;

// In Java the type argument is Class<?>; here it is System.Type.
/// <summary>
/// Visitor that infers the result <see cref="Type"/> of an expression by recursively walking its
/// AST. Returns null when the type cannot be determined. Several node kinds are only partially
/// implemented (see the inline TODOs).
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker</c>.</remarks>
internal class TypeChecker : IAstVisitor<Type?>
{

    // Renamed from Java's getType() to avoid colliding with object.GetType().
    /// <summary>
    /// Infers and returns the result type of the given expression, or null if it cannot be
    /// determined.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.getType(Expression)</c>.</remarks>
    public Type? GetTypeOf(Expression exp)
    {
        return exp.Accept<Type?>(this);
    }

    /// <summary>
    /// Infers the type of a binary expression. Logical operators yield <see cref="bool"/>; other
    /// operators are not yet fully resolved and may return null.
    /// </summary>
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

    /// <summary>
    /// Infers the type of a unary expression: logical NOT yields <see cref="bool"/>, otherwise the
    /// operand's own type.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitUnary(UnaryExpression)</c>.</remarks>
    public Type? VisitUnary(UnaryExpression exp)
    {
        var op = exp.Operator;
        if (op == Operator.NOT) return typeof(bool);
        return GetTypeOf(exp.Operand);
    }

    /// <summary>
    /// A string expression always has type <see cref="string"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitString(StringExpression)</c>.</remarks>
    public Type? VisitString(StringExpression exp)
    {
        return typeof(string);
    }

    /// <summary>
    /// Infers the type of a literal from its wrapped value's runtime type.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitLiteral(Literal)</c>.</remarks>
    public Type? VisitLiteral(Literal exp)
    {
        // TODO: should we return the primitive type for wrapper classes?
        var c = exp.Value!.GetType();
        if (c == typeof(double)) return typeof(double); // TODO
        return c;
    }

    /// <summary>
    /// Returns the common type of two types: equal types collapse to themselves, a null pairs to the
    /// other, and otherwise the result widens to <see cref="double"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.commonType(Class, Class)</c>.</remarks>
    protected Type? CommonType(Type? a, Type? b)
    {
        if (a == b) return a;
        if (a == null) return b;
        if (b == null) return a;
        return typeof(double);
    }

    /// <summary>
    /// Returns the common type of two expressions by inferring each one's type and combining them.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.commonType(Expression, Expression)</c>.</remarks>
    protected Type? CommonType(Expression a, Expression b)
    {
        return CommonType(GetTypeOf(a), GetTypeOf(b));
    }

    /// <summary>
    /// Infers the type of a variable reference. Not yet implemented; currently returns null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitVariable(Variable)</c>.</remarks>
    public Type? VisitVariable(Variable exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <summary>
    /// Infers the type of a call expression. Not yet implemented; currently returns null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitCall(CallExpression)</c>.</remarks>
    public Type? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <summary>
    /// Infers the type of a conditional as the common type of its two branches.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitConditional(ConditionalExpression)</c>.</remarks>
    public Type? VisitConditional(ConditionalExpression exp)
    {
        return CommonType(GetTypeOf(exp.IfTrue), GetTypeOf(exp.IfFalse));
    }

    /// <summary>
    /// Handles an unrecognized expression node by returning null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker.visitExpression(Expression)</c>.</remarks>
    public Type? VisitExpression(Expression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Ast;

// In Java the type argument is Class<?>; here it is System.Type.
/// <remarks>Ported from Java <c>com.clarisma.common.ast.TypeChecker</c>.</remarks>
public class TypeChecker : IAstVisitor<Type?>
{
    // Renamed from Java's getType() to avoid colliding with object.GetType().
    public Type? GetTypeOf(Expression exp)
    {
        return exp.Accept<Type?>(this);
    }

    public Type? VisitBinary(BinaryExpression exp)
    {
        Operator op = exp.Operator;
        if (op == Operator.ADD || op == Operator.OR)
        {
            return typeof(bool);
        }

        // TODO

        return null;
    }

    public Type? VisitUnary(UnaryExpression exp)
    {
        Operator op = exp.Operator;
        if (op == Operator.NOT) return typeof(bool);
        return GetTypeOf(exp.Operand);
    }

    public Type? VisitString(StringExpression exp)
    {
        return typeof(string);
    }

    public Type? VisitLiteral(Literal exp)
    {
        // TODO: should we return the primitive type for wrapper classes?
        Type c = exp.Value!.GetType();
        if (c == typeof(double)) return typeof(double); // TODO
        return c;
    }

    protected Type? CommonType(Type? a, Type? b)
    {
        if (a == b) return a;
        if (a == null) return b;
        if (b == null) return a;
        return typeof(double);
    }

    protected Type? CommonType(Expression a, Expression b)
    {
        return CommonType(GetTypeOf(a), GetTypeOf(b));
    }

    public Type? VisitVariable(Variable exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    public Type? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    public Type? VisitConditional(ConditionalExpression exp)
    {
        return CommonType(GetTypeOf(exp.IfTrue), GetTypeOf(exp.IfFalse));
    }

    public Type? VisitExpression(Expression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }
}

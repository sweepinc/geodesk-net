/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using Clarisma.Common.Xml;

namespace Clarisma.Common.Ast;

// In Java implements AstVisitor<Void>; here uses object? as the (ignored) result type.
/// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter</c>.</remarks>
public class ExpressionXmlWriter : XmlWriter, IAstVisitor<object?>
{

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter(OutputStream)</c>.</remarks>
    public ExpressionXmlWriter(Stream @out)
        : base(@out)
    {
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitBinary(BinaryExpression)</c>.</remarks>
    public object? VisitBinary(BinaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Left.Accept(this);
        exp.Right.Accept(this);
        End();
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitUnary(UnaryExpression)</c>.</remarks>
    public object? VisitUnary(UnaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Operand.Accept(this);
        End();
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitString(StringExpression)</c>.</remarks>
    public object? VisitString(StringExpression exp)
    {
        // TODO
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitLiteral(Literal)</c>.</remarks>
    public object? VisitLiteral(Literal exp)
    {
        var val = exp.Value!;
        Begin(val.GetType().Name.ToLowerInvariant());
        Attr("value", val);
        End();
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitVariable(Variable)</c>.</remarks>
    public object? VisitVariable(Variable exp)
    {
        Begin("var");
        Attr("name", exp.Name);
        End();
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitCall(CallExpression)</c>.</remarks>
    public object? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitConditional(ConditionalExpression)</c>.</remarks>
    public object? VisitConditional(ConditionalExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitExpression(Expression)</c>.</remarks>
    public object? VisitExpression(Expression exp)
    {
        // unknown expression; do nothing
        return null;
    }

}

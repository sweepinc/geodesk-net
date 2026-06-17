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
    public ExpressionXmlWriter(Stream @out)
        : base(@out)
    {
    }

    public object? VisitBinary(BinaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Left.Accept(this);
        exp.Right.Accept(this);
        End();
        return null;
    }

    public object? VisitUnary(UnaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Operand.Accept(this);
        End();
        return null;
    }

    public object? VisitString(StringExpression exp)
    {
        // TODO
        return null;
    }

    public object? VisitLiteral(Literal exp)
    {
        object val = exp.Value!;
        Begin(val.GetType().Name.ToLowerInvariant());
        Attr("value", val);
        End();
        return null;
    }

    public object? VisitVariable(Variable exp)
    {
        Begin("var");
        Attr("name", exp.Name);
        End();
        return null;
    }

    public object? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    public object? VisitConditional(ConditionalExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    public object? VisitExpression(Expression exp)
    {
        // unknown expression; do nothing
        return null;
    }
}

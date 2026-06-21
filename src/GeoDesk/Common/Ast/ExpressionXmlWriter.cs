/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;

using GeoDesk.Common.Xml;

namespace GeoDesk.Common.Ast;

// In Java implements AstVisitor<Void>; here uses object? as the (ignored) result type.
/// <summary>
/// Visitor that serializes an expression AST to XML, writing each node as a nested element via the
/// underlying <see cref="XmlWriter"/>. The visit methods return an ignored <c>object?</c> result
/// (Java used <c>AstVisitor&lt;Void&gt;</c>). Some node kinds are not yet implemented.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter</c>.</remarks>
internal class ExpressionXmlWriter : XmlWriter, IAstVisitor<object?>
{

    /// <summary>
    /// Creates an XML writer that emits the serialized expression to the given output stream.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter(OutputStream)</c>.</remarks>
    public ExpressionXmlWriter(Stream @out) :
        base(@out)
    {

    }

    /// <summary>
    /// Writes a binary expression as an element named after its operator, with the left and right
    /// operands serialized as nested children.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitBinary(BinaryExpression)</c>.</remarks>
    public object? VisitBinary(BinaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Left.Accept(this);
        exp.Right.Accept(this);
        End();
        return null;
    }

    /// <summary>
    /// Writes a unary expression as an element named after its operator, with the single operand
    /// serialized as a nested child.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitUnary(UnaryExpression)</c>.</remarks>
    public object? VisitUnary(UnaryExpression exp)
    {
        Begin(exp.Operator.Name);
        exp.Operand.Accept(this);
        End();
        return null;
    }

    /// <summary>
    /// Writes a string expression. Not yet implemented; currently a no-op.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitString(StringExpression)</c>.</remarks>
    public object? VisitString(StringExpression exp)
    {
        // TODO
        return null;
    }

    /// <summary>
    /// Writes a literal as an element named after the value's type, carrying the value in a
    /// <c>value</c> attribute.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitLiteral(Literal)</c>.</remarks>
    public object? VisitLiteral(Literal exp)
    {
        var val = exp.Value!;
        Begin(val.GetType().Name.ToLowerInvariant());
        Attr("value", val);
        End();
        return null;
    }

    /// <summary>
    /// Writes a variable reference as a <c>var</c> element carrying the variable name in a
    /// <c>name</c> attribute.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitVariable(Variable)</c>.</remarks>
    public object? VisitVariable(Variable exp)
    {
        Begin("var");
        Attr("name", exp.Name);
        End();
        return null;
    }

    /// <summary>
    /// Writes a call expression. Not yet implemented; currently a no-op.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitCall(CallExpression)</c>.</remarks>
    public object? VisitCall(CallExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <summary>
    /// Writes a conditional expression. Not yet implemented; currently a no-op.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitConditional(ConditionalExpression)</c>.</remarks>
    public object? VisitConditional(ConditionalExpression exp)
    {
        // TODO Auto-generated method stub
        return null;
    }

    /// <summary>
    /// Handles an unrecognized expression node by doing nothing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.ExpressionXmlWriter.visitExpression(Expression)</c>.</remarks>
    public object? VisitExpression(Expression exp)
    {
        // unknown expression; do nothing
        return null;
    }

}

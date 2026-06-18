/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using Clarisma.Common.Util;

namespace Clarisma.Common.Xml;

// TODO: fix or scrap
//
// In Java this extends PrintWriter. The straight port wraps a TextWriter and exposes
// the print/format helpers that subclasses rely on.
/// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter</c>.</remarks>
internal class XmlWriter
{

    protected readonly TextWriter Out;
    readonly string _indentString = "  ";
    readonly Stack<string> _elements;
    bool _childElements = true;

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter(OutputStream)</c>.</remarks>
    public XmlWriter(Stream @out)
        : this(new StreamWriter(@out, new UTF8Encoding(false)) { AutoFlush = true })
    {
    }

    /// <remarks>Port-only constructor wrapping a <c>TextWriter</c> (Java's XmlWriter extends PrintWriter).</remarks>
    public XmlWriter(TextWriter @out)
    {
        Out = @out;
        _elements = new Stack<string>();
        PrintLn("<?xml version='1.0' encoding='UTF-8'?>");
    }

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(String)</c>.</remarks>
    protected void Print(string s) => Out.Write(s);

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(char)</c>.</remarks>
    protected void Print(char c) => Out.Write(c);

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(long)</c>.</remarks>
    protected void Print(long v) => Out.Write(v);

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.println(String)</c>.</remarks>
    protected void PrintLn(string s) => Out.Write(s + "\n");

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.format(String, Object...)</c>.</remarks>
    protected void Format(string format, params object?[] args) => Out.Write(JavaFormat.Format(format, args));

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.flush()</c>.</remarks>
    public void Flush() => Out.Flush();

    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.close()</c>.</remarks>
    public void Close() => Out.Dispose();

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.indent()</c>.</remarks>
    protected void Indent()
    {
        for (var i = 0; i < _elements.Count; i++) Print(_indentString);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.begin(String)</c>.</remarks>
    public void Begin(string tag)
    {
        if (!_childElements)
        {
            PrintLn(">");
        }
        Indent();
        Print("<");
        Print(tag);
        _elements.Push(tag);
        _childElements = false;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.attr(String, Object)</c>.</remarks>
    public void Attr(string a, object v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(Escape(v.ToString() ?? ""));
        Print('\"');
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.attr(String, long)</c>.</remarks>
    public void Attr(string a, long v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(v);
        Print('\"');
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.end()</c>.</remarks>
    public void End()
    {
        var tag = _elements.Pop();
        if (_childElements)
        {
            Indent();
            Print("</");
            Print(tag);
            PrintLn(">");
        }
        else
        {
            PrintLn("/>");
            _childElements = true;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.escape(String)</c>.</remarks>
    public string Escape(string s)
    {
        return s; // TODO
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.empty(String, Object...)</c>.</remarks>
    public void Empty(string elem, params object?[] args)
    {
        if (!_childElements)
        {
            PrintLn(">");
            _childElements = true;
        }
        Indent();
        Print("<");
        Format(elem, args);
        PrintLn("/>");
    }

}

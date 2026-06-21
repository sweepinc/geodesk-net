/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.IO;
using System.Text;

using GeoDesk.Common.Util;

namespace GeoDesk.Common.Xml;

// TODO: fix or scrap
//
// In Java this extends PrintWriter. The straight port wraps a TextWriter and exposes
// the print/format helpers that subclasses rely on.
/// <summary>
/// Lightweight indenting XML writer. Maintains a stack of open elements so nested
/// <see cref="Begin"/>/<see cref="End"/> calls produce properly indented, well-formed XML, and
/// supports attributes and self-closing elements. Emits the XML declaration on construction.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter</c>.</remarks>
internal class XmlWriter
{

    protected readonly TextWriter Out;
    readonly string _indentString = "  ";
    readonly Stack<string> _elements;
    bool _childElements = true;

    /// <summary>
    /// Creates a writer that emits UTF-8 XML (without a byte-order mark) to the given output stream.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter(OutputStream)</c>.</remarks>
    public XmlWriter(Stream @out)
        : this(new StreamWriter(@out, new UTF8Encoding(false)) { AutoFlush = true })
    {
    }

    /// <summary>
    /// Creates a writer over the given text writer and emits the XML declaration line.
    /// </summary>
    /// <remarks>Port-only constructor wrapping a <c>TextWriter</c> (Java's XmlWriter extends PrintWriter).</remarks>
    public XmlWriter(TextWriter @out)
    {
        Out = @out;
        _elements = new Stack<string>();
        PrintLn("<?xml version='1.0' encoding='UTF-8'?>");
    }

    /// <summary>
    /// Writes a string to the output verbatim.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(String)</c>.</remarks>
    protected void Print(string s) => Out.Write(s);

    /// <summary>
    /// Writes a single character to the output verbatim.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(char)</c>.</remarks>
    protected void Print(char c) => Out.Write(c);

    /// <summary>
    /// Writes a long integer to the output.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.print(long)</c>.</remarks>
    protected void Print(long v) => Out.Write(v);

    /// <summary>
    /// Writes a string followed by a newline to the output.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.println(String)</c>.</remarks>
    protected void PrintLn(string s) => Out.Write(s + "\n");

    /// <summary>
    /// Writes a Java-style formatted string to the output.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.format(String, Object...)</c>.</remarks>
    protected void Format(string format, params object?[] args) => Out.Write(JavaFormat.Format(format, args));

    /// <summary>
    /// Flushes any buffered output to the underlying writer.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.flush()</c>.</remarks>
    public void Flush() => Out.Flush();

    /// <summary>
    /// Closes the underlying writer.
    /// </summary>
    /// <remarks>Port-only helper for Java's inherited <c>PrintWriter.close()</c>.</remarks>
    public void Close() => Out.Dispose();

    /// <summary>
    /// Writes indentation whitespace matching the current element nesting depth.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.indent()</c>.</remarks>
    protected void Indent()
    {
        for (var i = 0; i < _elements.Count; i++) Print(_indentString);
    }

    /// <summary>
    /// Opens a new element with the given tag, closing the parent's start tag if needed and pushing
    /// the element onto the open-element stack. Attributes may follow before the first child or
    /// <see cref="End"/>.
    /// </summary>
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

    /// <summary>
    /// Writes an attribute with the given name and an object value (escaped) on the currently open
    /// start tag.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.attr(String, Object)</c>.</remarks>
    public void Attr(string a, object v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(Escape(v.ToString() ?? ""));
        Print('\"');
    }

    /// <summary>
    /// Writes an attribute with the given name and a long-integer value on the currently open start tag.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.attr(String, long)</c>.</remarks>
    public void Attr(string a, long v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(v);
        Print('\"');
    }

    /// <summary>
    /// Closes the innermost open element, writing either a full closing tag (if it had children) or a
    /// self-closing <c>/&gt;</c> (if it was empty).
    /// </summary>
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

    /// <summary>
    /// Escapes a string for inclusion in XML. Currently a pass-through (escaping is not yet implemented).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.xml.XmlWriter.escape(String)</c>.</remarks>
    public string Escape(string s)
    {
        return s; // TODO
    }

    /// <summary>
    /// Writes a self-closing element whose tag is produced from the given format string and arguments,
    /// closing the parent's start tag first if needed.
    /// </summary>
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

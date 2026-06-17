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
public class XmlWriter
{
    protected readonly TextWriter Out;
    private readonly string indentString = "  ";
    private readonly Stack<string> elements;
    private bool childElements = true;

    public XmlWriter(Stream @out)
        : this(new StreamWriter(@out, new UTF8Encoding(false)) { AutoFlush = true })
    {
    }

    public XmlWriter(TextWriter @out)
    {
        Out = @out;
        elements = new Stack<string>();
        PrintLn("<?xml version='1.0' encoding='UTF-8'?>");
    }

    protected void Print(string s) => Out.Write(s);

    protected void Print(char c) => Out.Write(c);

    protected void Print(long v) => Out.Write(v);

    protected void PrintLn(string s) => Out.Write(s + "\n");

    protected void Format(string format, params object?[] args) => Out.Write(JavaFormat.Format(format, args));

    public void Flush() => Out.Flush();

    public void Close() => Out.Dispose();

    protected void Indent()
    {
        for (int i = 0; i < elements.Count; i++) Print(indentString);
    }

    public void Begin(string tag)
    {
        if (!childElements)
        {
            PrintLn(">");
        }
        Indent();
        Print("<");
        Print(tag);
        elements.Push(tag);
        childElements = false;
    }

    public void Attr(string a, object v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(Escape(v.ToString() ?? ""));
        Print('\"');
    }

    public void Attr(string a, long v)
    {
        Print(' ');
        Print(a);
        Print("=\"");
        Print(v);
        Print('\"');
    }

    public void End()
    {
        string tag = elements.Pop();
        if (childElements)
        {
            Indent();
            Print("</");
            Print(tag);
            PrintLn(">");
        }
        else
        {
            PrintLn("/>");
            childElements = true;
        }
    }

    public string Escape(string s)
    {
        return s; // TODO
    }

    public void Empty(string elem, params object?[] args)
    {
        if (!childElements)
        {
            PrintLn(">");
            childElements = true;
        }
        Indent();
        Print("<");
        Format(elem, args);
        PrintLn("/>");
    }
}

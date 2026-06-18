/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.IO;
using Clarisma.Common.Text;

namespace GeoDesk.Util;

// PORT: Java's Appendable sink is represented as a .NET TextWriter.
/// <summary>
/// Methods to generate JavaScript.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.util.JavaScript</c>.</remarks>
internal class JavaScript
{

    /// <remarks>Ported from Java <c>com.geodesk.util.JavaScript.writeMap(Appendable, Map)</c>.</remarks>
    public static void WriteMap(TextWriter outp, IDictionary v)
    {
        var first = true;
        outp.Write('{');
        foreach (DictionaryEntry entry in v)
        {
            if (first)
                first = false;
            else
                outp.Write(',');
            outp.Write(entry.Key.ToString());
            outp.Write(':');
            WriteValue(outp, entry.Value);
        }
        outp.Write('}');
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.JavaScript.writeArray(Appendable, Object)</c>.</remarks>
    public static void WriteArray(TextWriter outp, object v)
    {
        outp.Write('[');
        var arr = (Array)v;
        var len = arr.Length;
        for (var i = 0; i < len; i++)
        {
            if (i > 0) outp.Write(',');
            WriteValue(outp, arr.GetValue(i));
        }
        outp.Write(']');
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.JavaScript.writeString(Appendable, String)</c>.</remarks>
    public static void WriteString(TextWriter outp, string s)
    {
        outp.Write('"');
        outp.Write(Strings.Escape(s));
        outp.Write('"');
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.JavaScript.writeValue(Appendable, Object)</c>.</remarks>
    public static void WriteValue(TextWriter outp, object? v)
    {
        if (v == null)
            outp.Write("null");
        else if (v is string s)
            WriteString(outp, s);
        else if (v is IDictionary map)
            WriteMap(outp, map);
        else if (v.GetType().IsArray)
            WriteArray(outp, v);
        else
            outp.Write(v.ToString());
    }

}

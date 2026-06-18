/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Util;

namespace GeoDesk.Common.Fab;

/// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader</c>.</remarks>
internal class EchoFabReader : FabReader
{

    int _level;

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.indent()</c>.</remarks>
    protected void Indent()
    {
        for (var i = 0; i < _level; i++)
        {
            Console.Out.Write("   ");
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.beginKey(String, String)</c>.</remarks>
    protected override void BeginKey(string key, string value)
    {
        KeyValue(key, "");
        _level++;
        KeyValue("value", value);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.beginKey(String)</c>.</remarks>
    protected override void BeginKey(string key)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s:\n", key));
        _level++;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.keyValue(String, String)</c>.</remarks>
    protected override void KeyValue(string key, string value)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s: %s\n", key, value));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.endKey()</c>.</remarks>
    protected override void EndKey()
    {
        _level--;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.error(String)</c>.</remarks>
    protected override void Error(string msg)
    {
        Console.Out.Write(JavaFormat.Format("ERROR: %s:%d: %s\n",
            fileName == null ? "<none>" : fileName,
            lineNumber, msg));
    }

}

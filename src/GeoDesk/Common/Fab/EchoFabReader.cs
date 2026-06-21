/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Util;

namespace GeoDesk.Common.Fab;

/// <summary>
/// A <see cref="FabReader"/> that echoes the parsed FAB document back to the console as indented
/// text, primarily for debugging and verifying the parser.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader</c>.</remarks>
internal class EchoFabReader : FabReader
{

    int _level;

    /// <summary>
    /// Writes leading whitespace to the console matching the current nesting level.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.indent()</c>.</remarks>
    protected void Indent()
    {
        for (var i = 0; i < _level; i++)
        {
            Console.Out.Write("   ");
        }
    }

    /// <summary>
    /// Echoes a key that opens a block and also carries a value, increasing the indent level and
    /// printing the value as a nested <c>value</c> entry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.beginKey(String, String)</c>.</remarks>
    protected override void BeginKey(string key, string value)
    {
        KeyValue(key, "");
        _level++;
        KeyValue("value", value);
    }

    /// <summary>
    /// Echoes a key that opens a block, then increases the indent level for its children.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.beginKey(String)</c>.</remarks>
    protected override void BeginKey(string key)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s:\n", key));
        _level++;
    }

    /// <summary>
    /// Echoes a leaf key/value pair at the current indent level.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.keyValue(String, String)</c>.</remarks>
    protected override void KeyValue(string key, string value)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s: %s\n", key, value));
    }

    /// <summary>
    /// Decreases the indent level when a block closes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.endKey()</c>.</remarks>
    protected override void EndKey()
    {
        _level--;
    }

    /// <summary>
    /// Prints a parse error including the file name and line number to the console instead of throwing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader.error(String)</c>.</remarks>
    protected override void Error(string msg)
    {
        Console.Out.Write(JavaFormat.Format("ERROR: %s:%d: %s\n",
            fileName == null ? "<none>" : fileName,
            lineNumber, msg));
    }

}

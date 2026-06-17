/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Clarisma.Common.Util;

namespace Clarisma.Common.Fab;

/// <remarks>Ported from Java <c>com.clarisma.common.fab.EchoFabReader</c>.</remarks>
public class EchoFabReader : FabReader
{
    private int level;

    protected void Indent()
    {
        for (int i = 0; i < level; i++)
        {
            Console.Out.Write("   ");
        }
    }

    protected override void BeginKey(string key, string value)
    {
        KeyValue(key, "");
        level++;
        KeyValue("value", value);
    }

    protected override void BeginKey(string key)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s:\n", key));
        level++;
    }

    protected override void KeyValue(string key, string value)
    {
        Indent();
        Console.Out.Write(JavaFormat.Format("%s: %s\n", key, value));
    }

    protected override void EndKey()
    {
        level--;
    }

    protected override void Error(string msg)
    {
        Console.Out.Write(JavaFormat.Format("ERROR: %s:%d: %s\n",
            fileName == null ? "<none>" : fileName,
            lineNumber, msg));
    }
}

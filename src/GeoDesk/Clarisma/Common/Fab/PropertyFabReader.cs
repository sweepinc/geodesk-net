/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Diagnostics;

namespace Clarisma.Common.Fab;

/// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader</c>.</remarks>
public class PropertyFabReader : FabReader
{
    private readonly Dictionary<string, string> properties = new Dictionary<string, string>();
    private string? prefix;

    protected override void BeginKey(string key, string value)
    {
        if (prefix != null) key = prefix + key;
        properties[key] = value;
        prefix = key + ".";
    }

    protected override void BeginKey(string key)
    {
        if (prefix != null) key = prefix + key;
        prefix = key + ".";
    }

    protected override void KeyValue(string key, string value)
    {
        if (prefix != null) key = prefix + key;
        properties[key] = value;
    }

    protected override void EndKey()
    {
        Debug.Assert(prefix != null);
        int n = prefix!.LastIndexOf('.', prefix.Length - 2);
        Debug.Assert(n > 0);
        prefix = prefix.Substring(0, n + 1);
    }

    public Dictionary<string, string> Properties => properties;

    public Dictionary<string, string> ReadProperties(string filename)
    {
        ReadFile(filename);
        return properties;
    }
}

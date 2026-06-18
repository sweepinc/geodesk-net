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
internal class PropertyFabReader : FabReader
{

    readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
    string? _prefix;

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.beginKey(String, String)</c>.</remarks>
    protected override void BeginKey(string key, string value)
    {
        if (_prefix != null) key = _prefix + key;
        _properties[key] = value;
        _prefix = key + ".";
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.beginKey(String)</c>.</remarks>
    protected override void BeginKey(string key)
    {
        if (_prefix != null) key = _prefix + key;
        _prefix = key + ".";
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.keyValue(String, String)</c>.</remarks>
    protected override void KeyValue(string key, string value)
    {
        if (_prefix != null) key = _prefix + key;
        _properties[key] = value;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.endKey()</c>.</remarks>
    protected override void EndKey()
    {
        Debug.Assert(_prefix != null);
        var n = _prefix!.LastIndexOf('.', _prefix.Length - 2);
        Debug.Assert(n > 0);
        _prefix = _prefix.Substring(0, n + 1);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.properties()</c>.</remarks>
    public Dictionary<string, string> Properties => _properties;

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.readProperties(String)</c>.</remarks>
    public Dictionary<string, string> ReadProperties(string filename)
    {
        ReadFile(filename);
        return _properties;
    }

}

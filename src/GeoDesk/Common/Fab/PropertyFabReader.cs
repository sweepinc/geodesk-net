/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Diagnostics;

namespace GeoDesk.Common.Fab;

/// <summary>
/// A <see cref="FabReader"/> that flattens a nested FAB document into a flat property dictionary,
/// joining nested keys with dots (so <c>a { b: c }</c> becomes the property <c>a.b = c</c>).
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader</c>.</remarks>
internal class PropertyFabReader : FabReader
{

    readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
    string? _prefix;

    /// <summary>
    /// Records a property for a block-opening key that also carries a value, then pushes the key onto
    /// the dotted prefix for its children.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.beginKey(String, String)</c>.</remarks>
    protected override void BeginKey(string key, string value)
    {
        if (_prefix != null) key = _prefix + key;
        _properties[key] = value;
        _prefix = key + ".";
    }

    /// <summary>
    /// Pushes a block-opening key onto the dotted prefix used to qualify its child property names.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.beginKey(String)</c>.</remarks>
    protected override void BeginKey(string key)
    {
        if (_prefix != null) key = _prefix + key;
        _prefix = key + ".";
    }

    /// <summary>
    /// Records a leaf key/value pair as a property, qualifying the key with the current dotted prefix.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.keyValue(String, String)</c>.</remarks>
    protected override void KeyValue(string key, string value)
    {
        if (_prefix != null) key = _prefix + key;
        _properties[key] = value;
    }

    /// <summary>
    /// Pops the last segment off the dotted prefix as a block closes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.endKey()</c>.</remarks>
    protected override void EndKey()
    {
        Debug.Assert(_prefix != null);
        var n = _prefix!.LastIndexOf('.', _prefix.Length - 2);
        Debug.Assert(n > 0);
        _prefix = _prefix.Substring(0, n + 1);
    }

    /// <summary>
    /// The flat dictionary of dotted property names to values accumulated so far.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.properties()</c>.</remarks>
    public Dictionary<string, string> Properties => _properties;

    /// <summary>
    /// Reads the named FAB file and returns the resulting flat property dictionary.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.fab.PropertyFabReader.readProperties(String)</c>.</remarks>
    public Dictionary<string, string> ReadProperties(string filename)
    {
        ReadFile(filename);
        return _properties;
    }

}

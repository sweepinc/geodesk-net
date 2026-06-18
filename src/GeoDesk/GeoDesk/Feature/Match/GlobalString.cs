/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString</c>.</remarks>
public class GlobalString : IComparable<GlobalString>
{

    readonly string _stringValue;
    readonly int _value;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString(String, int)</c>.</remarks>
    public GlobalString(string stringValue, int value)
    {
        _stringValue = stringValue;
        _value = value;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.value()</c>.</remarks>
    public int Value => _value;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.stringValue()</c>.</remarks>
    public string StringValue => _stringValue;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.toString()</c>.</remarks>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" (#{1})", _stringValue, _value);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.compareTo(GlobalString)</c>.</remarks>
    public int CompareTo(GlobalString? other)
    {
        return _value.CompareTo(other!._value);
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Pairs a string with its numeric code in the store's global string table. Comparable by code,
/// allowing global strings to be sorted or searched by their integer index.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString</c>.</remarks>
internal class GlobalString : IComparable<GlobalString>
{

    readonly string _stringValue;
    readonly int _value;

    /// <summary>
    /// Creates a global string pairing the text with its numeric code in the global string table.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString(String, int)</c>.</remarks>
    public GlobalString(string stringValue, int value)
    {
        _stringValue = stringValue;
        _value = value;
    }

    /// <summary>
    /// The numeric code of this string in the global string table.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.value()</c>.</remarks>
    public int Value => _value;

    /// <summary>
    /// The text of this global string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.stringValue()</c>.</remarks>
    public string StringValue => _stringValue;

    /// <summary>
    /// Returns a debug rendering of the form <c>"text" (#code)</c>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.toString()</c>.</remarks>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" (#{1})", _stringValue, _value);
    }

    /// <summary>
    /// Orders global strings by their numeric code.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.GlobalString.compareTo(GlobalString)</c>.</remarks>
    public int CompareTo(GlobalString? other)
    {
        return _value.CompareTo(other!._value);
    }

}

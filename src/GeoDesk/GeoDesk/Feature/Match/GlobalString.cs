/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature.Match;

public class GlobalString : IComparable<GlobalString>
{
    private readonly string stringValue;
    private readonly int value;

    public GlobalString(string stringValue, int value)
    {
        this.stringValue = stringValue;
        this.value = value;
    }

    public int Value => value;

    public string StringValue => stringValue;

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "\"{0}\" (#{1})", stringValue, value);
    }

    public int CompareTo(GlobalString? other)
    {
        return value.CompareTo(other!.value);
    }
}

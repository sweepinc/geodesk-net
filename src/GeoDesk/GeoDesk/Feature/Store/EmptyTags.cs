/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature;

namespace GeoDesk.Feature.Store;

public class EmptyTags : Tags
{
    public static readonly EmptyTags SINGLETON = new EmptyTags();

    public bool IsEmpty()
    {
        return true;
    }

    public int Size()
    {
        return 0;
    }

    public bool Next()
    {
        return false;
    }

    public string? Key()
    {
        return null;
    }

    public object? Value()
    {
        return null;
    }

    public string? StringValue()
    {
        return null;
    }

    public int IntValue()
    {
        return 0;
    }

    public long LongValue()
    {
        return 0;
    }

    public double DoubleValue()
    {
        return 0;
    }

    public IDictionary<string, object?> ToMap()
    {
        return new Dictionary<string, object?>();
    }
}

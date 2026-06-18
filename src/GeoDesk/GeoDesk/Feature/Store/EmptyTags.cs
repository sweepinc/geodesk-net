/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature;

namespace GeoDesk.Feature.Store;

/// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags</c>.</remarks>
public class EmptyTags : Tags
{

    public static readonly EmptyTags SINGLETON = new EmptyTags();

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.isEmpty()</c>.</remarks>
    public bool IsEmpty()
    {
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.size()</c>.</remarks>
    public int Size()
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.next()</c>.</remarks>
    public bool Next()
    {
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.key()</c>.</remarks>
    public string? Key()
    {
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.value()</c>.</remarks>
    public object? Value()
    {
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.stringValue()</c>.</remarks>
    public string? StringValue()
    {
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.intValue()</c>.</remarks>
    public int IntValue()
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.longValue()</c>.</remarks>
    public long LongValue()
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.doubleValue()</c>.</remarks>
    public double DoubleValue()
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.EmptyTags.toMap()</c>.</remarks>
    public IDictionary<string, object?> ToMap()
    {
        return new Dictionary<string, object?>();
    }

}

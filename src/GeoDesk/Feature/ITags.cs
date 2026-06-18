/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature;

/// <summary>
/// A once-traversable collection that can be used to iterate a feature's tags.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Tags</c> (which extends
/// <c>com.clarisma.common.util.Consumable</c>; <see cref="IsEmpty"/> is declared here directly
/// so the public API does not expose the internal Consumable type).</remarks>
public interface ITags
{

    /// <summary>Returns true if there are no tags to iterate.</summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Consumable.isEmpty()</c>.</remarks>
    bool IsEmpty();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.size()</c>.</remarks>
    int Size();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.next()</c>.</remarks>
    bool Next();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.key()</c>.</remarks>
    string? Key();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.value()</c>.</remarks>
    object? Value();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.stringValue()</c>.</remarks>
    string? StringValue();

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.intValue()</c>.</remarks>
    int IntValue() => TagValues.ToInt(StringValue()!);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.longValue()</c>.</remarks>
    long LongValue() => TagValues.ToLong(StringValue()!);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.doubleValue()</c>.</remarks>
    double DoubleValue() => TagValues.ToDouble(StringValue()!);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Tags.toMap()</c>.</remarks>
    IDictionary<string, object?> ToMap();

}

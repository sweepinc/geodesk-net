/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Clarisma.Common.Util;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature;

/// <summary>
/// A <see cref="IConsumable"/> that can be used to iterate a feature's tags.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Tags</c>.</remarks>
public interface Tags : IConsumable
{
    int Size();
    bool Next();
    string? Key();
    object? Value();
    string? StringValue();
    int IntValue() => TagValues.ToInt(StringValue()!);
    long LongValue() => TagValues.ToLong(StringValue()!);
    double DoubleValue() => TagValues.ToDouble(StringValue()!);

    IDictionary<string, object?> ToMap();
}

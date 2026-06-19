/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <summary>
/// A code into the store's Global String Table (GST): the small integer that stands in for a common
/// tag key or value instead of an inline string. <see cref="Nil"/> (−1) means "not in the GST".
/// </summary>
internal readonly record struct StringCode(int Value)
{

    /// <summary>The "not found in the GST" sentinel, as returned by <c>FeatureStore.CodeFromString</c>.</summary>
    public static readonly StringCode Nil = new(-1);

    public bool IsValid => Value >= 0;

    public override string ToString() => $"str#{Value}";

}

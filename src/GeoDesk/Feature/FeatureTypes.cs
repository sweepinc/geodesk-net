/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature;

/// <summary>
/// Helpers for <see cref="FeatureType"/>. (Java declares these as static methods on the
/// enum, which C# enums cannot have.)
/// </summary>
/// <remarks>Port-only helper hosting the static members of the Java <c>com.geodesk.feature.FeatureType</c> enum.</remarks>
internal static class FeatureTypes
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureType.from(String)</c>.</remarks>
    public static FeatureType From(string s)
    {
        return s switch
        {
            "node" => FeatureType.Node,
            "way" => FeatureType.Way,
            "relation" => FeatureType.Relation,
            _ => throw new InvalidOperationException(s + " is not a valid feature type"),
        };
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureType.toString()</c>.</remarks>
    public static string ToString(FeatureType type)
    {
        return type switch
        {
            FeatureType.Node => "node",
            FeatureType.Way => "way",
            FeatureType.Relation => "relation",
            _ => "",
        };
    }

}

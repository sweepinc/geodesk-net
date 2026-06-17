/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature;

/// <summary>
/// An enum representing the three feature types: <c>Node</c>, <c>Way</c> and <c>Relation</c>.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureType</c>.</remarks>
public enum FeatureType
{
    Node,
    Way,
    Relation
}

/// <summary>
/// Helpers for <see cref="FeatureType"/>. (Java declares these as static methods on the
/// enum, which C# enums cannot have.)
/// </summary>
public static class FeatureTypes
{
    public static FeatureType From(string s)
    {
        switch (s)
        {
            case "node": return FeatureType.Node;
            case "way": return FeatureType.Way;
            case "relation": return FeatureType.Relation;
            default:
                throw new InvalidOperationException(s + " is not a valid feature type");
        }
    }

    public static string ToString(FeatureType type)
    {
        switch (type)
        {
            case FeatureType.Node: return "node";
            case FeatureType.Way: return "way";
            case FeatureType.Relation: return "relation";
        }
        return "";
    }
}

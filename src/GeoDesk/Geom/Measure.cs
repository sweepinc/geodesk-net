/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <summary>
/// Convenience helpers that measure the size of a feature's geometry, delegating to the feature's
/// own length and area calculations.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.Measure</c>.</remarks>
internal class Measure
{

    /// <summary>
    /// Returns the length of the given feature in meters (0 for non-lineal features).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Measure.length(Feature)</c>.</remarks>
    public static double Length(GeoDesk.Feature.IFeature f)
    {
        return f.Length;
    }

    /// <summary>
    /// Returns the area of the given feature in square meters (0 for non-polygonal features).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Measure.area(Feature)</c>.</remarks>
    public static double Area(GeoDesk.Feature.IFeature f)
    {
        return f.Area;
    }

}

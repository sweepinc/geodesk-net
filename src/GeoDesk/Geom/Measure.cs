/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.Measure</c>.</remarks>
internal class Measure
{

    /// <remarks>Ported from Java <c>com.geodesk.geom.Measure.length(Feature)</c>.</remarks>
    public static double Length(GeoDesk.Feature.IFeature f)
    {
        return f.Length();
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Measure.area(Feature)</c>.</remarks>
    public static double Area(GeoDesk.Feature.IFeature f)
    {
        return f.Area();
    }

}

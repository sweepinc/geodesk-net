/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter</c>.</remarks>
public abstract class SlowSpatialFilter : Filter
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected abstract bool AcceptGeometry(Geometry geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter.accept(Feature)</c>.</remarks>
    public bool Accept(Feature feature)
    {
        return AcceptGeometry(feature.ToGeometry());
    }

}

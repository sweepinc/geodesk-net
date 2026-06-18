/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter</c>.</remarks>
internal class SlowIntersectsFilter : SlowSpatialFilter
{

    readonly IPreparedGeometry _prepared;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter(PreparedGeometry)</c>.</remarks>
    public SlowIntersectsFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter(Geometry)</c>.</remarks>
    public SlowIntersectsFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected override bool AcceptGeometry(Geometry geom)
    {
        return geom != null && _prepared.Intersects(geom);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter.bounds()</c>.</remarks>
    public Bounds Bounds()
    {
        // TODO: if using Feature, get the bbox of feature, but Envelope will be calculated anyway,
        //  so only minor savings
        return Box.FromEnvelope(_prepared.Geometry.EnvelopeInternal);
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Geom;
using GeoDesk.Util;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter for the <c>crosses</c> spatial predicate.
/// <para>
/// Tile acceleration: if tile is disjoint, reject all; if tile is contained properly: a feature
/// that lies entirely within the tile is rejected (a crossing feature must lie partially outside
/// of the test geometry), a multi-tile feature must be tested.
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter</c>.</remarks>
internal class SlowCrossesFilter : SlowSpatialFilter
{

    readonly IPreparedGeometry _prepared;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter(PreparedGeometry)</c>.</remarks>
    public SlowCrossesFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter(Geometry)</c>.</remarks>
    public SlowCrossesFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter(Feature)</c>.</remarks>
    public SlowCrossesFilter(IFeature f)
    {
        Geometry g;
        if (f is IRelation rel)
            g = GeometryBuilder.Instance.CreateMultiLineString(rel);
        else
            g = f.ToGeometry();
        _prepared = PreparedGeometryFactory.Prepare(g);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected override bool AcceptGeometry(Geometry geom)
    {
        return geom != null && _prepared.Crosses(geom);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowCrossesFilter.bounds()</c>.</remarks>
    public IBounds Bounds()
    {
        // TODO: if using Feature, get the bbox of feature, but Envelope will be calculated anyway,
        //  so only minor savings
        return Box.FromEnvelope(_prepared.Geometry.EnvelopeInternal);
    }

}

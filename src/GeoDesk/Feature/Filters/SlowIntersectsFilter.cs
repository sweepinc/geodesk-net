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

/// <summary>
/// A spatial filter that accepts features whose geometry intersects a fixed
/// reference geometry. Uses a prepared geometry for efficient repeated tests.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter</c>.</remarks>
internal class SlowIntersectsFilter : SlowSpatialFilter
{

    readonly IPreparedGeometry _prepared;

    /// <summary>
    /// Creates a filter from an already-prepared reference geometry against which
    /// candidate features are tested for intersection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter(PreparedGeometry)</c>.</remarks>
    public SlowIntersectsFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
    }

    /// <summary>
    /// Creates a filter from a reference geometry, preparing it for repeated
    /// intersection tests.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter(Geometry)</c>.</remarks>
    public SlowIntersectsFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>
    /// Returns true if the candidate geometry is non-null and intersects the
    /// reference geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected override bool AcceptGeometry(Geometry geom)
    {
        return geom != null && _prepared.Intersects(geom);
    }

    /// <summary>
    /// Returns the bounding box of the reference geometry, used to pre-select
    /// candidate features before the precise intersection test.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowIntersectsFilter.bounds()</c>.</remarks>
    public IBounds Bounds()
    {
        // TODO: if using Feature, get the bbox of feature, but Envelope will be calculated anyway,
        //  so only minor savings
        return Box.FromEnvelope(_prepared.Geometry.EnvelopeInternal);
    }

}

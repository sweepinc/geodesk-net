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
/// A spatial filter that accepts features whose geometry lies entirely within a
/// fixed reference geometry. Uses a prepared geometry for efficient repeated tests.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowWithinFilter</c>.</remarks>
internal class SlowWithinFilter : SlowSpatialFilter
{

    readonly IPreparedGeometry _prepared;

    /// <summary>
    /// Creates a filter from an already-prepared reference geometry that candidate
    /// features must be contained within.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowWithinFilter(PreparedGeometry)</c>.</remarks>
    public SlowWithinFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
    }

    /// <summary>
    /// Creates a filter from a reference geometry, preparing it for repeated
    /// containment tests.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowWithinFilter(Geometry)</c>.</remarks>
    public SlowWithinFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>
    /// Returns true if the candidate geometry is non-null and is contained within
    /// the reference geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowWithinFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected override bool AcceptGeometry(Geometry geom)
    {
        return geom != null && _prepared.Contains(geom);
    }

    /// <summary>
    /// Returns the bounding box of the reference geometry, used to pre-select
    /// candidate features before the precise containment test.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowWithinFilter.bounds()</c>.</remarks>
    public IBounds Bounds()
    {
        // TODO: if using Feature, get the bbox of feature, but Envelope will be calculated anyway,
        //  so only minor savings
        return Box.FromEnvelope(_prepared.Geometry.EnvelopeInternal);
    }

}

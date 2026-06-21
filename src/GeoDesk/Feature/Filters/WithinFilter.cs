/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Match;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A filter that only accepts features whose geometry lies entirely within the test geometry.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter</c>.</remarks>
internal class WithinFilter : AbstractRelateFilter
{

    /// <summary>
    /// Creates a within-filter testing against the geometry of the given feature.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(Feature)</c>.</remarks>
    public WithinFilter(IFeature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <summary>
    /// Creates a within-filter testing against the given geometry, preparing it for fast repeated tests.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(Geometry)</c>.</remarks>
    public WithinFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>
    /// Creates a within-filter testing against an already-prepared geometry, restricting accepted
    /// feature types to those compatible with the geometry's dimension.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(PreparedGeometry)</c>.</remarks>
    public WithinFilter(IPreparedGeometry prepared)
        : base(prepared, AcceptedType(prepared))
    {
    }

    /// <summary>
    /// Determines which feature types can possibly lie within the prepared geometry, based on whether
    /// it is polygonal, lineal, or puntal.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.acceptedType(PreparedGeometry)</c>.</remarks>
    static int AcceptedType(IPreparedGeometry prepared)
    {
        var geom = prepared.Geometry;
        if (geom is IPolygonal) return TypeBits.ALL;
        if (geom is ILineal) return TypeBits.ALL & ~TypeBits.AREAS;
        if (geom is IPuntal) return TypeBits.NODES | TypeBits.NONAREA_RELATIONS;
        return 0;   // don't accept generic GeometryCollection
    }

    /// <summary>
    /// The strategy flags describing how this filter can be optimized: it supports fast tile
    /// filtering, needs feature geometry, uses a strict bounding box, and restricts feature types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.strategy()</c>.</remarks>
    public override int Strategy => FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry | FilterStrategy.UsesBbox |
            FilterStrategy.StrictBbox | FilterStrategy.RestrictsTypes;

    /// <summary>
    /// Specializes this filter for a single tile: returns a false filter if the tile is disjoint from
    /// the test geometry, a fast accept-all filter if the tile is fully contained, or this filter
    /// otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.filterForTile(int, Polygon)</c>.</remarks>
    public override IFilter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (prepared.Disjoint(tileGeometry)) return FalseFilter.Instance;
        if (testDimension == 2 && prepared.ContainsProperly(tileGeometry)) return new FastTileFilter(tile, true, this);
        return this;
    }

    /// <summary>
    /// Accepts a feature when its geometry is contained within the test geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(IFeature feature, Geometry geom)
    {
        return prepared.Contains(geom);
    }

}

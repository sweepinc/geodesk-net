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
/// A Filter that only accepts features whose geometry intersects the test geometry.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter</c>.</remarks>
internal class IntersectsFilter : AbstractRelateFilter
{

    /// <summary>Creates a filter using the given feature's geometry as the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter(Feature)</c>.</remarks>
    public IntersectsFilter(IFeature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <summary>Creates a filter that accepts features intersecting the given geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter(Geometry)</c>.</remarks>
    public IntersectsFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>Creates a filter from an already-prepared reference geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter(PreparedGeometry)</c>.</remarks>
    public IntersectsFilter(IPreparedGeometry prepared)
        : base(prepared, TypeBits.ALL)
    {
    }

    /// <summary>The filter strategy flags: tile acceleration, needs geometry, uses a bounding box.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter.strategy()</c>.</remarks>
    public override int Strategy => FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry | FilterStrategy.UsesBbox;

    /// <summary>
    /// Specializes the filter per tile: rejects disjoint tiles and waives the test for
    /// tiles properly contained in a 2-D reference geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter.filterForTile(int, Polygon)</c>.</remarks>
    public override IFilter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (prepared.Disjoint(tileGeometry)) return FalseFilter.Instance;
        if (testDimension == 2 && prepared.ContainsProperly(tileGeometry)) return null;
        return this;
    }

    /// <summary>Returns true if the feature's geometry intersects the reference geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IntersectsFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(IFeature feature, Geometry geom)
    {
        return prepared.Intersects(geom);
    }

}

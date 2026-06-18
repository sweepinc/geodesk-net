/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter that only accepts features whose geometry is disjoint from the test geometry.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter</c>.</remarks>
internal class DisjointFilter : Filter
{

    readonly IPreparedGeometry _prepared;
    readonly int _testDimension;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(Feature)</c>.</remarks>
    public DisjointFilter(Feature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(Geometry)</c>.</remarks>
    public DisjointFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(PreparedGeometry)</c>.</remarks>
    public DisjointFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
        var geom = prepared.Geometry;
        _testDimension = (geom.GetType() == typeof(GeometryCollection)) ?
            AbstractRelateFilter.MixedDimension : (int)geom.Dimension;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.strategy()</c>.</remarks>
    public int Strategy()
    {
        return FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.filterForTile(int, Polygon)</c>.</remarks>
    public Filter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (_prepared.Disjoint(tileGeometry)) return new FastTileFilter(tile, true, this);
        if (_testDimension == 2 && _prepared.ContainsProperly(tileGeometry)) return FalseFilter.Instance;
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(Feature feature, Geometry geom)
    {
        return _prepared.Disjoint(geom);
    }

}

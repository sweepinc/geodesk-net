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
internal class DisjointFilter : IFilter
{

    readonly IPreparedGeometry _prepared;
    readonly int _testDimension;

    /// <summary>Creates a filter using the given feature's geometry as the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(Feature)</c>.</remarks>
    public DisjointFilter(IFeature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <summary>Creates a filter that accepts features disjoint from the given geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(Geometry)</c>.</remarks>
    public DisjointFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>Creates a filter from an already-prepared reference geometry, caching its dimension.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter(PreparedGeometry)</c>.</remarks>
    public DisjointFilter(IPreparedGeometry prepared)
    {
        _prepared = prepared;
        var geom = prepared.Geometry;
        _testDimension = (geom.GetType() == typeof(GeometryCollection)) ?
            AbstractRelateFilter.MixedDimension : (int)geom.Dimension;
    }

    /// <summary>The filter strategy flags: tile acceleration and needs geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.strategy()</c>.</remarks>
    public int Strategy => FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry;

    /// <summary>
    /// Specializes the filter per tile: waives the test for tiles disjoint from the
    /// reference geometry and rejects tiles it properly contains.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.filterForTile(int, Polygon)</c>.</remarks>
    public IFilter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (_prepared.Disjoint(tileGeometry)) return new FastTileFilter(tile, true, this);
        if (_testDimension == 2 && _prepared.ContainsProperly(tileGeometry)) return FalseFilter.Instance;
        return this;
    }

    /// <summary>Returns true if the feature's geometry is disjoint from the reference geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.DisjointFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(IFeature feature, Geometry geom)
    {
        return _prepared.Disjoint(geom);
    }

}

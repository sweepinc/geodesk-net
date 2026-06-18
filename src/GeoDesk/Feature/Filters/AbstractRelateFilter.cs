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
/// A base class for Filter classes that implement DE9-IM predicates.
/// <para>
/// This base class provides the most common presets:
/// - strategy: tile acceleration, uses (non-strict) bbox, needs geometry, restricts types
/// - tile acceleration: disjoint tile rejects all; properly-contained tile rejects single-tile
///   features, tests multi-tile.
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter</c>.</remarks>
internal abstract class AbstractRelateFilter : IFilter
{

    internal const int MixedDimension = -3;

    protected readonly IPreparedGeometry prepared;
    protected readonly Box bounds;
    protected readonly int acceptedTypes;
    protected readonly int testDimension;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter(PreparedGeometry, int)</c>.</remarks>
    public AbstractRelateFilter(IPreparedGeometry prepared, int acceptedTypes)
    {
        this.prepared = prepared;
        var geom = prepared.Geometry;
        bounds = Box.FromEnvelope(geom.EnvelopeInternal);
        testDimension = (geom.GetType() == typeof(GeometryCollection)) ? MixedDimension : (int)geom.Dimension;
        this.acceptedTypes = acceptedTypes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter.strategy()</c>.</remarks>
    public virtual int Strategy()
    {
        return FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry | FilterStrategy.UsesBbox |
            FilterStrategy.RestrictsTypes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter.acceptedTypes()</c>.</remarks>
    public int AcceptedTypes()
    {
        return acceptedTypes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter.filterForTile(int, Polygon)</c>.</remarks>
    public virtual IFilter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (prepared.Disjoint(tileGeometry)) return FalseFilter.Instance;
        if (testDimension == 2 && prepared.ContainsProperly(tileGeometry)) return new FastTileFilter(tile, false, this);
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter.accept(Feature, Geometry)</c>.</remarks>
    public abstract bool Accept(IFeature feature, Geometry geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AbstractRelateFilter.bounds()</c>.</remarks>
    public IBounds Bounds()
    {
        return bounds;
    }

}

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

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter</c>.</remarks>
public class WithinFilter : AbstractRelateFilter
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(Feature)</c>.</remarks>
    public WithinFilter(Feature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(Geometry)</c>.</remarks>
    public WithinFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter(PreparedGeometry)</c>.</remarks>
    public WithinFilter(IPreparedGeometry prepared)
        : base(prepared, AcceptedType(prepared))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.acceptedType(PreparedGeometry)</c>.</remarks>
    static int AcceptedType(IPreparedGeometry prepared)
    {
        var geom = prepared.Geometry;
        if (geom is IPolygonal) return TypeBits.ALL;
        if (geom is ILineal) return TypeBits.ALL & ~TypeBits.AREAS;
        if (geom is IPuntal) return TypeBits.NODES | TypeBits.NONAREA_RELATIONS;
        return 0;   // don't accept generic GeometryCollection
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.strategy()</c>.</remarks>
    public override int Strategy()
    {
        return FilterStrategy.FastTileFilter | FilterStrategy.NeedsGeometry | FilterStrategy.UsesBbox |
            FilterStrategy.StrictBbox | FilterStrategy.RestrictsTypes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.filterForTile(int, Polygon)</c>.</remarks>
    public override Filter? FilterForTile(int tile, Polygon tileGeometry)
    {
        if (prepared.Disjoint(tileGeometry)) return FalseFilter.Instance;
        if (testDimension == 2 && prepared.ContainsProperly(tileGeometry)) return new FastTileFilter(tile, true, this);
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.WithinFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(Feature feature, Geometry geom)
    {
        return prepared.Contains(geom);
    }

}

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
/// A Filter that only accepts features whose geometry crosses the test geometry.
/// <para>
/// Dimension of intersection must be less than maximum dimension of candidate and test: if test is
/// polygonal, don't accept areas; if test is puntal, don't accept nodes. This Filter does not
/// accept generic GeometryCollections, neither as test nor as candidate (result is always false).
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter</c>.</remarks>
internal class CrossesFilter : AbstractRelateFilter
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter(Feature)</c>.</remarks>
    public CrossesFilter(Feature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter(Geometry)</c>.</remarks>
    public CrossesFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter(PreparedGeometry)</c>.</remarks>
    public CrossesFilter(IPreparedGeometry prepared)
        : base(prepared, AcceptedType(prepared))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter.acceptedType(PreparedGeometry)</c>.</remarks>
    static int AcceptedType(IPreparedGeometry prepared)
    {
        var geom = prepared.Geometry;
        if (geom is IPolygonal) return TypeBits.ALL & ~TypeBits.AREAS;
        if (geom is IPuntal) return TypeBits.ALL & ~TypeBits.NODES;
        if (geom.GetType() == typeof(GeometryCollection)) return 0;
        return TypeBits.ALL;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.CrossesFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(Feature feature, Geometry geom)
    {
        if (geom.GetType() == typeof(GeometryCollection)) return false;
        return prepared.Crosses(geom);
    }

}

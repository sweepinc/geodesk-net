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
/// A Filter that only accepts features whose geometry touches the test geometry.
/// <para>
/// Test and candidate must have at least one point in common, but their interiors do not intersect.
/// If test is puntal, do not accept nodes. This Filter does not accept generic GeometryCollections,
/// neither as test nor as candidate (result is always false).
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter</c>.</remarks>
internal class TouchesFilter : AbstractRelateFilter
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter(Feature)</c>.</remarks>
    public TouchesFilter(Feature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter(Geometry)</c>.</remarks>
    public TouchesFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter(PreparedGeometry)</c>.</remarks>
    public TouchesFilter(IPreparedGeometry prepared)
        : base(prepared, AcceptedType(prepared))
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter.acceptedType(PreparedGeometry)</c>.</remarks>
    static int AcceptedType(IPreparedGeometry prepared)
    {
        var geom = prepared.Geometry;
        if (geom is IPuntal) return TypeBits.ALL & ~TypeBits.NODES;
        if (geom.GetType() == typeof(GeometryCollection)) return 0;
        return TypeBits.ALL;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.TouchesFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(Feature feature, Geometry geom)
    {
        if (geom.GetType() == typeof(GeometryCollection)) return false;
        return prepared.Touches(geom);
    }

}

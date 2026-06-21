/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Match;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter that only accepts features whose geometry overlaps the test geometry.
/// <para>
/// The geometries of test and candidate must have the same dimension. Test and candidate each have
/// at least one point not shared by the other. The intersection of their interiors has the same
/// dimension. This Filter does not accept generic GeometryCollections, neither as test nor as
/// candidate (result is always false).
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter</c>.</remarks>
internal class OverlapsFilter : AbstractRelateFilter
{

    /// <summary>Creates a filter using the given feature's geometry as the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter(Feature)</c>.</remarks>
    public OverlapsFilter(IFeature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <summary>Creates a filter that accepts features overlapping the given geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter(Geometry)</c>.</remarks>
    public OverlapsFilter(Geometry geom)
        : this(PreparedGeometryFactory.Prepare(geom))
    {
    }

    /// <summary>Creates a filter from an already-prepared reference geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter(PreparedGeometry)</c>.</remarks>
    public OverlapsFilter(IPreparedGeometry prepared)
        : base(prepared, AcceptedType(prepared))
    {
    }

    /// <summary>
    /// Determines which feature types can possibly overlap the given test geometry,
    /// based on its dimension (overlap requires equal dimensions).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter.acceptedType(PreparedGeometry)</c>.</remarks>
    static int AcceptedType(IPreparedGeometry prepared)
    {
        var geom = prepared.Geometry;
        if (geom is IPolygonal) return TypeBits.AREAS | TypeBits.NONAREA_RELATIONS;
        if (geom is ILineal) return TypeBits.NONAREA_WAYS | TypeBits.NONAREA_RELATIONS;
        if (geom is IPuntal) return TypeBits.NODES | TypeBits.NONAREA_RELATIONS;
        return 0;   // don't accept generic GeometryCollection
    }

    /// <summary>Returns true if the feature's geometry overlaps the reference geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.OverlapsFilter.accept(Feature, Geometry)</c>.</remarks>
    public override bool Accept(IFeature feature, Geometry geom)
    {
        if (geom.GetType() == typeof(GeometryCollection)) return false;
        return prepared.Overlaps(geom);
    }

}

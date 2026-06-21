/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Util;

/// <summary>
/// A <see cref="GeometryFactory"/> extended with helpers for building NTS geometry
/// from GeoDesk features and longitude/latitude input, producing geometry in the
/// library's Web Mercator coordinate space.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder</c>.</remarks>
internal class GeometryBuilder : GeometryFactory
{

    public static readonly GeometryBuilder Instance = new GeometryBuilder();

    /// <summary>
    /// Creates a point at the given longitude/latitude, projecting it into Web
    /// Mercator coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createPointFromLonLat(double, double)</c>.</remarks>
    public Point CreatePointFromLonLat(double lon, double lat)
    {
        return CreatePoint(new Coordinate(Mercator.XFromLon(lon), Mercator.YFromLat(lat)));
    }

    /// <summary>
    /// Creates a line string from the coordinate sequence of the given way feature.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createLineString(Feature)</c>.</remarks>
    public LineString CreateLineString(GeoDesk.Feature.IFeature way)
    {
        return CreateLineString(new WayCoordinateSequence(way.ToXY()));
    }

    /// <summary>
    /// Creates a multi-line string from the way members of the given relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createMultiLineString(Relation)</c>.</remarks>
    public MultiLineString CreateMultiLineString(IRelation rel)
    {
        var lines = new List<LineString>();
        foreach (var way in rel.Members().Ways())
        {
            lines.Add(Instance.CreateLineString(way));
        }
        return CreateMultiLineString(lines.ToArray());
    }

}

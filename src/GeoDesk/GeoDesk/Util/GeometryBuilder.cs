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

/// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder</c>.</remarks>
public class GeometryBuilder : GeometryFactory
{

    public static readonly GeometryBuilder Instance = new GeometryBuilder();

    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createPointFromLonLat(double, double)</c>.</remarks>
    public Point CreatePointFromLonLat(double lon, double lat)
    {
        return CreatePoint(new Coordinate(Mercator.XFromLon(lon), Mercator.YFromLat(lat)));
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createLineString(Feature)</c>.</remarks>
    public LineString CreateLineString(GeoDesk.Feature.Feature way)
    {
        return CreateLineString(new WayCoordinateSequence(way.ToXY()));
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.GeometryBuilder.createMultiLineString(Relation)</c>.</remarks>
    public MultiLineString CreateMultiLineString(Relation rel)
    {
        var lines = new List<LineString>();
        foreach (var way in rel.Members().Ways())
        {
            lines.Add(Instance.CreateLineString(way));
        }
        return CreateMultiLineString(lines.ToArray());
    }

}

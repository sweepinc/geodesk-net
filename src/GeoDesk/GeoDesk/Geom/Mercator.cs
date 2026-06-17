/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using static System.Math;

namespace GeoDesk.Geom;

/// <summary>
/// Methods for working with Mercator-projected coordinates.
/// GeoDesk uses a Pseudo-Mercator projection that projects
/// coordinates onto a square Cartesian plane 2^32 units wide/tall.
/// This projection is compatible with Web Mercator EPSG:3857,
/// except that instead of meters at the Equator, it uses a made-up
/// unit called "imp" ("integer, Mercator-projected").
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.Mercator</c>.</remarks>
public static class Mercator
{
    private const double MapWidth = 4_294_967_294.9999d;

    // the width and height of the coordinate space (1 << 32)
    private const double EarthCircumference = 40_075_016.68558;
        // in meters, at the equator

    public const double MinLat = -85.05112878;
    public const double MaxLat = 85.051128776;

    // Java Math.round(double): returns floor(a + 0.5). .NET Math.Round uses
    // banker's rounding by default, so we replicate Java's semantics explicitly.
    private static long JavaRound(double a) => (long)Floor(a + 0.5);

    /// <summary>
    /// Converts a longitude to Mercator imps.
    /// </summary>
    /// <param name="lon">longitude (in degrees)</param>
    /// <returns>equivalent imps</returns>
    public static int XFromLon(double lon)
    {
        if (lon < -180 || lon > 180)
        {
            throw new ArgumentException("Longitude must be in range -180 to 180");
        }
        return (int)JavaRound(MapWidth * lon / 360);
    }

    /// <summary>
    /// Converts a longitude to Mercator imps.
    /// </summary>
    /// <param name="lon">longitude (in 100-nanodegree increments)</param>
    /// <returns>equivalent imps</returns>
    public static int XFromLon100nd(int lon)
    {
        return XFromLon((double)lon / 10_000_000);
    }

    /// <summary>
    /// Converts a latitude to Mercator imps.
    /// </summary>
    /// <param name="lat">latitude (in degrees)</param>
    /// <returns>equivalent imps</returns>
    public static int YFromLat(double lat)
    {
        if (lat < MinLat)
        {
            if (lat < -90)
            {
                throw new ArgumentException("Latitude must be in range -90 to 90");
            }
            lat = MinLat;
        }
        if (lat > MaxLat)
        {
            if (lat > 90)
            {
                throw new ArgumentException("Latitude must be in range -90 to 90");
            }
            lat = MaxLat;
        }
        return (int)JavaRound(Log(Tan((lat + 90) * PI / 360)) *
            (MapWidth / 2 / PI));
    }

    /// <summary>
    /// Converts a latitude to Mercator imps.
    /// </summary>
    /// <param name="lat">latitude (in 100-nanodegree increments)</param>
    /// <returns>equivalent imps</returns>
    public static int YFromLat100nd(int lat)
    {
        return YFromLat((double)lat / 10_000_000);
    }

    public static double Scale(double y)
    {
        return Cosh(y * 2 * PI / MapWidth);
    }

    /// <summary>
    /// Converts a projected longitude to WGS84.
    /// </summary>
    /// <param name="x">projected longitude (in imps)</param>
    /// <returns>equivalent WGS-84 longitude in degrees</returns>
    public static double LonFromX(double x)
    {
        return x * 360 / MapWidth;
    }

    /// <summary>
    /// Converts a projected longitude to WGS84, rounded to 7 decimal points.
    /// </summary>
    public static double LonPrecision7FromX(double x)
    {
        double lon = LonFromX(x);
        return (double)JavaRound(lon * 10000000) / 10000000;
    }

    /// <summary>
    /// Converts a projected latitude to WGS84.
    /// </summary>
    /// <param name="y">projected latitude (in imps)</param>
    /// <returns>equivalent WGS-84 latitude in degrees</returns>
    public static double LatFromY(double y)
    {
        return Atan(Exp(y * PI * 2 / MapWidth))
            * 360 / PI - 90;
    }

    /// <summary>
    /// Converts a projected latitude to WGS84, rounded to 7 decimal points.
    /// </summary>
    public static double LatPrecision7FromY(double y)
    {
        double lat = LatFromY(y);
        return (double)JavaRound(lat * 10000000) / 10000000;
    }

    public static double MetersAtY(int y)
    {
        return EarthCircumference / MapWidth / Scale(y);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two projected
    /// points. A simple method that is sufficiently accurate only
    /// for short distances.
    /// </summary>
    /// <returns>distance in meters</returns>
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        double xDelta = Abs(x1 - x2);
        double yDelta = Abs(y1 - y2);
        double d = Sqrt(xDelta * xDelta + yDelta * yDelta);
        return d * EarthCircumference / MapWidth / Scale(
            (y1 + y2) / 2);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two projected points.
    /// </summary>
    /// <returns>distance in meters</returns>
    public static double Distance(Coordinate c1, Coordinate c2)
    {
        return Distance(c1.X, c1.Y, c2.X, c2.Y);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two Geometry objects
    /// (with Mercator-projected coordinates).
    /// </summary>
    /// <returns>distance in meters</returns>
    public static double Distance(Geometry a, Geometry b)
    {
        Coordinate[] nearestPoints = DistanceOp.NearestPoints(a, b);
        return Distance(nearestPoints[0], nearestPoints[1]);
    }

    /// <summary>
    /// Calculates the equivalent number of imps that
    /// are equal to the given distance in meters at a
    /// planar-projected latitude.
    /// </summary>
    /// <param name="meters">distance in meters</param>
    /// <param name="atY">the projected latitude (i.e. in imps, not degrees)</param>
    /// <returns>the distance in imps</returns>
    public static double DeltaFromMeters(double meters, double atY)
    {
        return meters * MapWidth / EarthCircumference *
            Scale(atY);
    }

    /// <summary>
    /// Calculates the area of the given geometry (in square meters).
    /// A simple method that is sufficiently accurate only for small areas.
    /// </summary>
    // TODO: check
    public static double Area(Geometry geom)
    {
        double area = geom.Area;
        System.Diagnostics.Debug.Assert(area >= 0, "Negative area for " + geom);
        if (area == 0) return 0;
        double scale = EarthCircumference / MapWidth / Scale(
            geom.Centroid.Y);
        return area * scale * scale;
    }

    public static Envelope ExpandEnvelope(Envelope env, double meters)
    {
        env.ExpandBy(DeltaFromMeters(meters,
            (env.MaxY + env.MinY) / 2));
        return env;
    }

    public static Envelope Envelope(double lon1, double lat1, double lon2, double lat2)
    {
        return new Envelope(XFromLon(lon1), XFromLon(lon2),
            YFromLat(lat1), YFromLat(lat2));
    }

    /// <summary>
    /// Converts the WGS84 (longitude/latitude) coordinates of a
    /// Geometry into Mercator projection. The Geometry is modified in-place.
    /// </summary>
    /// <param name="geom">the Geometry whose coordinates to project</param>
    public static void Project(Geometry geom)
    {
        geom.Apply(new ProjectFilter());
    }

    private sealed class ProjectFilter : ICoordinateSequenceFilter
    {
        public void Filter(CoordinateSequence seq, int i)
        {
            seq.SetOrdinate(i, 0, XFromLon(seq.GetX(i)));
            seq.SetOrdinate(i, 1, YFromLat(seq.GetY(i)));
        }

        public bool Done => false;

        public bool GeometryChanged => true;
    }
}

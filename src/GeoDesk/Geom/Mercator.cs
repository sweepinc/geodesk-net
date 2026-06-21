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
internal static class Mercator
{

    const double MapWidth = 4_294_967_294.9999d;

    // the width and height of the coordinate space (1 << 32)
    const double EarthCircumference = 40_075_016.68558;
    // in meters, at the equator

    public const double MinLat = -85.05112878;
    public const double MaxLat = 85.051128776;

    // Java Math.round(double): returns floor(a + 0.5). .NET Math.Round uses banker's rounding by
    // default, so we replicate Java's semantics explicitly. (Port-only helper, no Java counterpart.)
    static long JavaRound(double a) => (long)Floor(a + 0.5);

    /// <summary>
    /// Converts a longitude (in degrees) to a Mercator X coordinate in imps, throwing if the
    /// longitude is outside the valid -180 to 180 range.
    /// </summary>
    /// <param name="lon">longitude (in degrees)</param>
    /// <returns>equivalent imps</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.xFromLon(double)</c>.</remarks>
    public static int XFromLon(double lon)
    {
        if (lon < -180 || lon > 180)
            throw new ArgumentException("Longitude must be in range -180 to 180");

        return (int)JavaRound(MapWidth * lon / 360);
    }

    /// <summary>
    /// Converts a longitude expressed in 100-nanodegree increments to a Mercator X coordinate in imps.
    /// </summary>
    /// <param name="lon">longitude (in 100-nanodegree increments)</param>
    /// <returns>equivalent imps</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.xFromLon100nd(int)</c>.</remarks>
    public static int XFromLon100nd(int lon)
    {
        return XFromLon((double)lon / 10_000_000);
    }

    /// <summary>
    /// Converts a latitude (in degrees) to a Mercator Y coordinate in imps. Latitudes are clamped to
    /// the Web Mercator limits (<see cref="MinLat"/>/<see cref="MaxLat"/>); values outside -90 to 90
    /// throw.
    /// </summary>
    /// <param name="lat">latitude (in degrees)</param>
    /// <returns>equivalent imps</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.yFromLat(double)</c>.</remarks>
    public static int YFromLat(double lat)
    {
        if (lat < MinLat)
        {
            if (lat < -90)
                throw new ArgumentException("Latitude must be in range -90 to 90");

            lat = MinLat;
        }

        if (lat > MaxLat)
        {
            if (lat > 90)
                throw new ArgumentException("Latitude must be in range -90 to 90");

            lat = MaxLat;
        }

        return (int)JavaRound(Log(Tan((lat + 90) * PI / 360)) *
            (MapWidth / 2 / PI));
    }

    /// <summary>
    /// Converts a latitude expressed in 100-nanodegree increments to a Mercator Y coordinate in imps.
    /// </summary>
    /// <param name="lat">latitude (in 100-nanodegree increments)</param>
    /// <returns>equivalent imps</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.yFromLat100nd(int)</c>.</remarks>
    public static int YFromLat100nd(int lat)
    {
        return YFromLat((double)lat / 10_000_000);
    }

    /// <summary>
    /// Returns the Mercator distortion factor at the given projected Y coordinate, i.e. how much the
    /// projection stretches relative to true ground distance at that latitude (1.0 at the equator).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.scale(double)</c>.</remarks>
    public static double Scale(double y)
    {
        return Cosh(y * 2 * PI / MapWidth);
    }

    /// <summary>
    /// Converts a projected X coordinate (in imps) back to a WGS-84 longitude in degrees.
    /// </summary>
    /// <param name="x">projected longitude (in imps)</param>
    /// <returns>equivalent WGS-84 longitude in degrees</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.lonFromX(double)</c>.</remarks>
    public static double LonFromX(double x)
    {
        return x * 360 / MapWidth;
    }

    /// <summary>
    /// Converts a projected X coordinate (in imps) to a WGS-84 longitude in degrees, rounded to
    /// 7 decimal places (the precision OSM uses).
    /// </summary>
    /// <param name="x">projected longitude (in imps)</param>
    /// <returns>equivalent WGS-84 longitude in degrees</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.lonPrecision7fromX(double)</c>.</remarks>
    public static double LonPrecision7FromX(double x)
    {
        return (double)JavaRound(LonFromX(x) * 10000000) / 10000000;
    }

    /// <summary>
    /// Converts a projected Y coordinate (in imps) back to a WGS-84 latitude in degrees.
    /// </summary>
    /// <param name="y">projected latitude (in imps)</param>
    /// <returns>equivalent WGS-84 latitude in degrees</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.latFromY(double)</c>.</remarks>
    public static double LatFromY(double y)
    {
        return Atan(Exp(y * PI * 2 / MapWidth)) * 360 / PI - 90;
    }

    /// <summary>
    /// Converts a projected Y coordinate (in imps) to a WGS-84 latitude in degrees, rounded to
    /// 7 decimal places (the precision OSM uses).
    /// </summary>
    /// <param name="y">projected latitude (in imps)</param>
    /// <returns>equivalent WGS-84 latitude in degrees</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.latPrecision7fromY(double)</c>.</remarks>
    public static double LatPrecision7FromY(double y)
    {
        return (double)JavaRound(LatFromY(y) * 10000000) / 10000000;
    }

    /// <summary>
    /// Returns the number of ground meters represented by one imp at the given projected Y coordinate,
    /// accounting for Mercator distortion.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.metersAtY(int)</c>.</remarks>
    public static double MetersAtY(int y)
    {
        return EarthCircumference / MapWidth / Scale(y);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two projected points. A simple method that is
    /// sufficiently accurate only for short distances.
    /// </summary>
    /// <returns>distance in meters</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.distance(double, double, double, double)</c>.</remarks>
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        var xDelta = Abs(x1 - x2);
        var yDelta = Abs(y1 - y2);
        var d = Sqrt(xDelta * xDelta + yDelta * yDelta);
        return d * EarthCircumference / MapWidth / Scale((y1 + y2) / 2);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two projected points. A simple method that is
    /// sufficiently accurate only for short distances.
    /// </summary>
    /// <returns>distance in meters</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.distance(Coordinate, Coordinate)</c>.</remarks>
    public static double Distance(Coordinate c1, Coordinate c2)
    {
        return Distance(c1.X, c1.Y, c2.X, c2.Y);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two Geometry objects (with Mercator-projected
    /// coordinates). A simple method that is sufficiently accurate only for short distances.
    /// </summary>
    /// <returns>distance in meters</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.distance(Geometry, Geometry)</c>.</remarks>
    public static double Distance(Geometry a, Geometry b)
    {
        var nearestPoints = DistanceOp.NearestPoints(a, b);
        return Distance(nearestPoints[0], nearestPoints[1]);
    }

    /// <summary>
    /// Calculates the equivalent number of imps that are equal to the given distance in meters at a
    /// planar-projected latitude.
    /// </summary>
    /// <param name="meters">distance in meters</param>
    /// <param name="atY">the projected latitude (i.e. in imps, not degrees)</param>
    /// <returns>the distance in imps</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.deltaFromMeters(double, double)</c>.</remarks>
    public static double DeltaFromMeters(double meters, double atY)
    {
        return meters * MapWidth / EarthCircumference * Scale(atY);
    }

    /// <summary>
    /// Calculates the area of the given geometry (in square meters). A simple method that is
    /// sufficiently accurate only for small areas.
    /// </summary>
    /// <param name="geom">the geometry</param>
    /// <returns>area in square meters</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.area(Geometry)</c>.</remarks>
    // TODO: check
    public static double Area(Geometry geom)
    {
        var area = geom.Area;
        System.Diagnostics.Debug.Assert(area >= 0, "Negative area for " + geom);

        if (area == 0)
            return 0;

        var scale = EarthCircumference / MapWidth / Scale(geom.Centroid.Y);
        return area * scale * scale;
    }

    /// <summary>
    /// Expands the given envelope outward by the imp-equivalent of the given distance in meters,
    /// computed at the envelope's mid-latitude. Mutates and returns the same envelope.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.expandEnvelope(Envelope, double)</c>.</remarks>
    public static Envelope ExpandEnvelope(Envelope env, double meters)
    {
        env.ExpandBy(DeltaFromMeters(meters, (env.MaxY + env.MinY) / 2));

        return env;
    }

    /// <summary>
    /// Builds a JTS <see cref="Envelope"/> in Mercator imps from two longitude/latitude corner points.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.envelope(double, double, double, double)</c>.</remarks>
    public static Envelope Envelope(double lon1, double lat1, double lon2, double lat2)
    {
        return new Envelope(XFromLon(lon1), XFromLon(lon2), YFromLat(lat1), YFromLat(lat2));
    }

    /// <summary>
    /// Converts the WGS84 (longitude/latitude) coordinates of a Geometry into Mercator projection.
    /// The Geometry is modified in-place.
    /// </summary>
    /// <param name="geom">the Geometry whose coordinates to project</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Mercator.project(Geometry)</c>.</remarks>
    public static void Project(Geometry geom)
    {
        geom.Apply(new ProjectFilter());
    }

    // Port of the anonymous CoordinateSequenceFilter used by Mercator.project(Geometry) in Java.
    /// <summary>
    /// A JTS coordinate-sequence filter that projects each coordinate from WGS-84 longitude/latitude
    /// into Mercator imps in place, used by <see cref="Project"/>.
    /// </summary>
    sealed class ProjectFilter : ICoordinateSequenceFilter
    {

        /// <summary>
        /// Projects the coordinate at index <paramref name="i"/> of the sequence in place, replacing
        /// its longitude/latitude ordinates with Mercator imps.
        /// </summary>
        public void Filter(CoordinateSequence seq, int i)
        {
            seq.SetOrdinate(i, 0, XFromLon(seq.GetX(i)));
            seq.SetOrdinate(i, 1, YFromLat(seq.GetY(i)));
        }

        /// <summary>
        /// Always false; every coordinate in the sequence is visited.
        /// </summary>
        public bool Done => false;

        /// <summary>
        /// Always true; the filter mutates coordinates, so the geometry is marked as changed.
        /// </summary>
        public bool GeometryChanged => true;

    }

}

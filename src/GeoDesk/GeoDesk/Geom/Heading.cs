/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NetTopologySuite.Geometries;
using static System.Math;

namespace GeoDesk.Geom;

/// <summary>
/// Compass headings.
///
/// Keep in mind that unlike screen coordinates, planar coordinate values
/// *increase* as one moves "up" (north).
/// </summary>
/// <remarks>
/// Ported from Java <c>com.geodesk.geom.Heading</c> (a Java <c>enum</c>; modelled here as a sealed
/// class with static instances, preserving the Java enum's <c>ordinal()</c>/<c>values()</c> surface).
/// </remarks>
internal sealed class Heading
{

    public static readonly Heading North = new Heading(0, "N", 1, 0, 0);
    public static readonly Heading Northeast = new Heading(1, "NE", 1, 1, 45);
    public static readonly Heading East = new Heading(2, "E", 0, 1, 90);
    public static readonly Heading Southeast = new Heading(3, "SE", -1, 1, 135);
    public static readonly Heading South = new Heading(4, "S", -1, 0, 180);
    public static readonly Heading Southwest = new Heading(5, "SW", -1, -1, 225);
    public static readonly Heading West = new Heading(6, "W", 0, -1, 270);
    public static readonly Heading Northwest = new Heading(7, "NW", 1, -1, 315);

    static readonly Heading[] ValuesArray =
    {
        North, Northeast, East, Southeast, South, Southwest, West, Northwest
    };

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.values()</c>.</remarks>
    public static Heading[] Values() => ValuesArray;

    /// <summary>
    /// Returns the Heading closest to the given compass heading in degrees (0 = north, 90 = east, etc.)
    /// </summary>
    /// <param name="degrees">must be 0 &lt;= degrees &lt; 360</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.fromDegrees(double)</c>.</remarks>
    public static Heading FromDegrees(double degrees)
    {
        return ValuesArray[(int)(((degrees % 360) + 22.5) / 45)];
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.turnedBy(double, double)</c>.</remarks>
    public static double TurnedBy(double fromDegrees, double byDegrees)
    {
        return (fromDegrees + byDegrees) % 360;
    }

    /// <summary>
    /// Determines the Coordinate that lies a given distance from the center of the plane, in a given
    /// Heading.
    /// </summary>
    /// <param name="angle">heading in degrees (0 = north, 90 = east)</param>
    /// <param name="distance">distance</param>
    /// <returns>the JTS Coordinate</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.project(double, double)</c>.</remarks>
    public static Coordinate Project(double angle, double distance)
    {
        var radians = angle * PI / 180;
        var x = Sin(radians) * distance;
        var y = Cos(radians) * distance;
        return new Coordinate(x, y);
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.project(double, double, Coordinate)</c>.</remarks>
    public static LineSegment Project(double angle, double distance, Coordinate from)
    {
        var to = Project(angle, distance);
        to.X += from.X;
        to.Y += from.Y;
        return new LineSegment(from, to);
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.projectedLine(GeometryFactory, double, double, Coordinate)</c>.</remarks>
    public static LineString ProjectedLine(GeometryFactory factory, double angle, double distance, Coordinate from)
    {
        var to = Project(angle, distance);
        to.X += from.X;
        to.Y += from.Y;
        return factory.CreateLineString(new Coordinate[] { from, to });
    }

    readonly int _ordinal;
    readonly string _id;
    readonly int _northFactor;
    readonly int _eastFactor;
    readonly int _degrees;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading</c> enum constructor.</remarks>
    Heading(int ordinal, string id, int northFactor, int eastFactor, int degrees)
    {
        _ordinal = ordinal;
        _id = id;
        _northFactor = northFactor;
        _eastFactor = eastFactor;
        _degrees = degrees;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.ordinal()</c>.</remarks>
    public int Ordinal => _ordinal;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.toString()</c>.</remarks>
    public override string ToString()
    {
        return _id;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.id()</c>.</remarks>
    public string Id => _id;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.northFactor()</c>.</remarks>
    public int NorthFactor => _northFactor;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.eastFactor()</c>.</remarks>
    public int EastFactor => _eastFactor;

    /// <summary>Heading in degrees.</summary>
    /// <returns>0 = north, 90 = east, etc.</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.toDegrees()</c>.</remarks>
    public int ToDegrees()
    {
        return _degrees;
    }

    /// <summary>Returns the opposite Heading (the heading that lies 180 degrees opposite).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.reversed()</c>.</remarks>
    public Heading Reversed()
    {
        return ValuesArray[(_ordinal + 4) % 8];
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Heading.turnedBy(double)</c>.</remarks>
    public Heading TurnedBy(double degrees)
    {
        return FromDegrees((degrees + _degrees) % 360);
    }

}

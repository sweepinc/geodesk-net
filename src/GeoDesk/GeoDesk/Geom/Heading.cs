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
public sealed class Heading
{
    public static readonly Heading North = new Heading(0, "N", 1, 0, 0);
    public static readonly Heading Northeast = new Heading(1, "NE", 1, 1, 45);
    public static readonly Heading East = new Heading(2, "E", 0, 1, 90);
    public static readonly Heading Southeast = new Heading(3, "SE", -1, 1, 135);
    public static readonly Heading South = new Heading(4, "S", -1, 0, 180);
    public static readonly Heading Southwest = new Heading(5, "SW", -1, -1, 225);
    public static readonly Heading West = new Heading(6, "W", 0, -1, 270);
    public static readonly Heading Northwest = new Heading(7, "NW", 1, -1, 315);

    private static readonly Heading[] ValuesArray =
    {
        North, Northeast, East, Southeast, South, Southwest, West, Northwest
    };

    private readonly int ordinal;
    private readonly string id;
    private readonly int northFactor;
    private readonly int eastFactor;
    private readonly int degrees;

    private Heading(int ordinal, string id, int northFactor, int eastFactor, int degrees)
    {
        this.ordinal = ordinal;
        this.id = id;
        this.northFactor = northFactor;
        this.eastFactor = eastFactor;
        this.degrees = degrees;
    }

    public static Heading[] Values() => ValuesArray;

    public int Ordinal => ordinal;

    public override string ToString()
    {
        return id;
    }

    public string Id => id;

    public int NorthFactor => northFactor;

    public int EastFactor => eastFactor;

    /// <summary>
    /// Heading in degrees.
    /// </summary>
    /// <returns>0 = north, 90 = east, etc.</returns>
    public int ToDegrees()
    {
        return degrees;
    }

    /// <summary>
    /// Returns the opposite Heading (180 degrees opposite).
    /// </summary>
    public Heading Reversed()
    {
        return ValuesArray[(ordinal + 4) % 8];
    }

    public Heading TurnedBy(double degrees)
    {
        return FromDegrees((degrees + this.degrees) % 360);
    }

    /// <summary>
    /// Returns the Heading closest to the given compass heading
    /// in degrees (0 = north, 90 = east, etc.)
    /// </summary>
    /// <param name="degrees">must be 0 &lt;= degrees &lt; 360</param>
    public static Heading FromDegrees(double degrees)
    {
        return ValuesArray[(int)(((degrees % 360) + 22.5) / 45)];
    }

    public static double TurnedBy(double fromDegrees, double byDegrees)
    {
        return (fromDegrees + byDegrees) % 360;
    }

    /// <summary>
    /// Determines the Coordinate that lies a given distance from
    /// the center of the plane, in a given Heading.
    /// </summary>
    /// <param name="angle">heading in degrees (0 = north, 90 = east)</param>
    /// <param name="distance">distance</param>
    public static Coordinate Project(double angle, double distance)
    {
        double radians = angle * PI / 180;
        double x = Sin(radians) * distance;
        double y = Cos(radians) * distance;
        return new Coordinate(x, y);
    }

    public static LineSegment Project(double angle, double distance, Coordinate from)
    {
        Coordinate to = Project(angle, distance);
        to.X += from.X;
        to.Y += from.Y;
        return new LineSegment(from, to);
    }

    public static LineString ProjectedLine(
        GeometryFactory factory,
        double angle, double distance, Coordinate from)
    {
        Coordinate to = Project(angle, distance);
        to.X += from.X;
        to.Y += from.Y;
        return factory.CreateLineString(new Coordinate[] { from, to });
    }
}

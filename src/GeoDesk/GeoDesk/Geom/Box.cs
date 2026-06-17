/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

/// <summary>
/// An axis-aligned bounding box. A <c>Box</c> represents minimum and maximum
/// X and Y coordinates in a Mercator-projected plane. It can straddle the
/// Antimeridian (in which case <c>minX</c> is *larger* than <c>maxX</c>). A <c>Box</c> can
/// also be empty (in which case <c>minY</c> is *larger* than <c>maxY</c>)
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.Box</c>.</remarks>
public class Box : Bounds
{
    private int minX;
    private int minY;
    private int maxX;
    private int maxY;

    /// <summary>Creates a null Box.</summary>
    public Box()
    {
        SetNull();
    }

    public Box(int minX, int minY, int maxX, int maxY)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
    }

    public Box(Bounds b)
    {
        minX = b.MinX;
        minY = b.MinY;
        maxX = b.MaxX;
        maxY = b.MaxY;
    }

    public Box(int x, int y)
        : this(x, y, x, y)
    {
    }

    public void SetNull()
    {
        minX = int.MaxValue;
        minY = int.MaxValue;
        maxX = int.MinValue;
        maxY = int.MinValue;
    }

    /// <summary>
    /// Returns <c>true</c> if this bounding box straddles the Antimeridian.
    /// </summary>
    public bool CrossesAntimeridian()
    {
        return maxX < minX;
    }

    /// <summary>
    /// Returns <c>true</c> if this bounding box is empty (<c>maxY</c> is less than <c>minY</c>).
    /// </summary>
    public bool IsNull()
    {
        return maxY < minY;
    }

    public static bool IsNullBounds(Bounds b)
    {
        return b.MaxY < b.MinY;
    }

    public int MinX => minX;

    public int MinY => minY;

    public int MaxX => maxX;

    public int MaxY => maxY;

    // Concrete implementations of the Bounds computed members. These both satisfy
    // the interface and remain callable directly on a Box reference (as in Java,
    // where they are interface default methods accessible on the instance).
    // NOTE: do NOT forward to ((Bounds)this) — because these methods implement the
    // interface members, that would re-dispatch back here and recurse infinitely.
    public bool Intersects(Bounds other)
    {
        return !(other.MinX > maxX ||
            other.MaxX < minX ||
            other.MinY > maxY ||
            other.MaxY < minY);
    }

    public bool Contains(int x, int y)
    {
        if (maxX < minX) return (x >= minX || x <= maxX) && y >= minY && y <= maxY;
        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    public bool Contains(Bounds other)
    {
        return other.MinX >= minX && other.MaxX <= maxX &&
            other.MinY >= minY && other.MaxY <= maxY;
    }

    public long Width => (maxY < minY) ? 0 : ((((long)maxX - minX) & 0xffff_ffffL) + 1);

    public long Height => maxY < minY ? 0 : ((long)maxY - minY + 1);

    public long Area => Width * Height;

    public int CenterX => minX + (maxX - minX) / 2;

    public int CenterY => minY + (maxY - minY) / 2;

    /// <summary>
    /// Checks if this bounding box includes the given coordinate, and expands
    /// it if necessary.
    /// </summary>
    // TODO: turns 180-crossing box into regular box
    public void ExpandToInclude(int x, int y)
    {
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
    }

    /// <summary>
    /// Checks if this bounding box includes another bounding box, and expands
    /// it if necessary.
    /// </summary>
    public void ExpandToInclude(Bounds b)
    {
        int otherMinX = b.MinX;
        int otherMinY = b.MinY;
        int otherMaxX = b.MaxX;
        int otherMaxY = b.MaxY;
        if (otherMinX < minX) minX = otherMinX;
        if (otherMinY < minY) minY = otherMinY;
        if (otherMaxX > maxX) maxX = otherMaxX;
        if (otherMaxY > maxY) maxY = otherMaxY;
    }

    /// <summary>
    /// Checks if this bounding box includes another bounding box, and expands
    /// it if necessary.
    /// </summary>
    public void ExpandToInclude(int otherMinX, int otherMinY, int otherMaxX, int otherMaxY)
    {
        if (otherMinX < minX) minX = otherMinX;
        if (otherMinY < minY) minY = otherMinY;
        if (otherMaxX > maxX) maxX = otherMaxX;
        if (otherMaxY > maxY) maxY = otherMaxY;
    }

    public void ExpandToInclude(int[] coords)
    {
        for (int i = 0; i < coords.Length; i += 2) ExpandToInclude(coords[i], coords[i + 1]);
    }

    /// <summary>
    /// Creates a JTS Envelope with the same dimensions as this bounding box.
    /// </summary>
    public Envelope ToEnvelope()
    {
        return new Envelope(minX, maxX, minY, maxY); // Envelope specifies both x first, then both y
    }

    public override bool Equals(object? other)
    {
        if (other is not Bounds o) return false;
        return minX == o.MinX && maxX == o.MaxX && minY == o.MinY && maxY == o.MaxY;
    }

    public override int GetHashCode()
    {
        return minX.GetHashCode() ^ minY.GetHashCode() ^ maxX.GetHashCode() ^ maxY.GetHashCode();
    }

    /// <summary>
    /// Creates a new bounding box that is the result of the intersection between
    /// this bounding box and another.
    /// </summary>
    // TODO: fix: what happens if boxes are empty?
    public Box Intersection(Bounds o)
    {
        int x1 = minX > o.MinX ? minX : o.MinX;
        int y1 = minY > o.MinY ? minY : o.MinY;
        int x2 = maxX < o.MaxX ? maxX : o.MaxX;
        int y2 = maxY < o.MaxY ? maxY : o.MaxY;
        if (x2 < x1 || y2 < y1) return new Box(); // no intersection
        return new Box(x1, y1, x2, y2);
    }

    public static Box Intersection(Bounds a, Bounds b)
    {
        int x1 = System.Math.Max(a.MinX, b.MinX);
        int y1 = System.Math.Max(a.MinY, b.MinY);
        int x2 = System.Math.Min(a.MaxX, b.MaxX);
        int y2 = System.Math.Min(a.MaxY, b.MaxY);
        if (x2 < x1 || y2 < y1) return new Box(); // no intersection
        return new Box(x1, y1, x2, y2);
    }

    public static Bounds Smaller(Bounds a, Bounds b)
    {
        double areaA = (double)a.Width * a.Height;
        double areaB = (double)b.Width * b.Height;
        return (areaA < areaB) ? a : b;
    }

    /// <summary>
    /// Overflow-safe subtraction
    /// </summary>
    /// <returns>the result of the subtraction; or the lowest negative value in case of an overflow</returns>
    private static int TrimmedSubtract(int x, int y)
    {
        int r = x - y;
        if (((x ^ y) & (x ^ r)) < 0) return int.MinValue;
        return r;
    }

    /// <summary>
    /// Overflow-safe addition
    /// </summary>
    /// <returns>the result of the addition; or the highest positive value in case of an overflow</returns>
    private static int TrimmedAdd(int x, int y)
    {
        int r = x + y;
        if (((x ^ r) & (y ^ r)) < 0) return int.MaxValue;
        return r;
    }

    /// <summary>
    /// Expands or contracts all sides of this bounding box by a specified
    /// number of imps. If the bounding box is empty, the result is undefined.
    /// </summary>
    /// <param name="b">the buffer (in imps)</param>
    // TODO: define and test Antimeridian behaviour
    public void Buffer(int b)
    {
        minX -= b;
        maxX += b;
        if (b >= 0)
        {
            minY = TrimmedSubtract(minY, b);
            maxY = TrimmedAdd(maxY, b);
        }
        else
        {
            minY = TrimmedAdd(minY, -b);
            maxY = TrimmedSubtract(maxY, -b);
            if (maxY < minY) SetNull();
            // TODO: check if width flipped
        }
    }

    /// <summary>
    /// Expands or contracts all sides of this bounding box by a specified
    /// number of meters. If the bounding box is empty, the result is undefined.
    /// </summary>
    /// <param name="m">the buffer (in meters)</param>
    public void BufferMeters(double m)
    {
        Buffer((int)Mercator.DeltaFromMeters(m, CenterY));
    }

    /// <summary>
    /// Moves the bounding box horizontally and vertically by the specified
    /// number of units.
    /// </summary>
    public void Translate(int deltaX, int deltaY)
    {
        minX += deltaX;
        maxX += deltaX;
        if (deltaY > 0)
        {
            minY = TrimmedAdd(minY, deltaY);
            maxY = TrimmedAdd(maxY, deltaY);
        }
        else
        {
            minY = TrimmedSubtract(minY, deltaY);
            maxY = TrimmedSubtract(maxY, deltaY);
        }
    }

    public override string ToString()
    {
        return IsNull() ? "[empty]" : string.Format(CultureInfo.InvariantCulture, "[{0},{1} -> {2},{3}]", minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Creates a <c>Box</c> with the given bounds (west, south, east, north).
    /// </summary>
    public static Box OfWSEN(double west, double south, double east, double north)
    {
        return new Box(Mercator.XFromLon(west), Mercator.YFromLat(south),
            Mercator.XFromLon(east), Mercator.YFromLat(north));
    }

    /// <summary>
    /// Creates a <c>Box</c> that covers a single point.
    /// </summary>
    public static Box AtLonLat(double lon, double lat)
    {
        int x = Mercator.XFromLon(lon);
        int y = Mercator.YFromLat(lat);
        return new Box(x, y, x, y);
    }

    /// <summary>
    /// Creates a <c>Box</c> that covers a single point.
    /// </summary>
    public static Box AtXY(int x, int y)
    {
        return new Box(x, y, x, y);
    }

    public static Box OfXYXY(int x1, int y1, int x2, int y2)
    {
        return new Box(x1, y1, x2, y2);
    }

    // TODO: decide what width/height mean
    public static Box OfXYWidthHeight(int x, int y, int w, int h)
    {
        return new Box(x, y, x + w - 1, y + h - 1);
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative
    /// to a coordinate pair.
    /// </summary>
    public static Box MetersAroundXY(double meters, int x, int y)
    {
        int b = (int)Mercator.DeltaFromMeters(meters, y);
        return new Box(x - b, TrimmedSubtract(y, b), x + b, TrimmedAdd(y, b));
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative
    /// to a coordinate pair.
    /// </summary>
    public static Box ImpsAroundXY(int d, int x, int y)
    {
        return new Box(x - d, TrimmedSubtract(y, d), x + d, TrimmedAdd(y, d));
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative
    /// to a coordinate pair.
    /// </summary>
    public static Box MetersAroundLonLat(double meters, double lon, double lat)
    {
        return MetersAroundXY(meters, Mercator.XFromLon(lon), Mercator.YFromLat(lat));
    }

    /// <summary>
    /// Creates a bounding box whose sides are extended by a specific distance relative
    /// to another bounding box.
    /// </summary>
    public static Box MetersAround(double meters, Bounds other)
    {
        int b = (int)Mercator.DeltaFromMeters(meters, other.CenterY);
        return new Box(other.MinX - b, TrimmedSubtract(other.MinY, b),
            other.MaxX + b, TrimmedAdd(other.MaxY, b));
    }

    // TODO: rename of()?
    public static Box FromEnvelope(Envelope env)
    {
        return new Box(
            (int)System.Math.Floor(env.MinX),
            (int)System.Math.Floor(env.MinY + 0.5), // TODO: why not floor?
            (int)System.Math.Ceiling(env.MaxX),
            (int)System.Math.Floor(env.MaxY + 0.5)); // TODO: why not ceil?
    }

    /// <summary>
    /// Creates the tightest <c>Box</c> that encloses the given JTS Geometry.
    /// </summary>
    public static Box Of(Geometry geom)
    {
        return FromEnvelope(geom.EnvelopeInternal);
    }

    /// <summary>
    /// Creates the tightest <c>Box</c> that encloses the given LineSegment.
    /// </summary>
    public static Box Of(LineSegment seg)
    {
        double x1 = seg.P0.X;
        double y1 = seg.P0.Y;
        double x2 = seg.P1.X;
        double y2 = seg.P1.Y;
        return new Box(
            (int)System.Math.Floor(x1 < x2 ? x1 : x2),
            (int)System.Math.Floor(y1 < y2 ? y1 : y2),
            (int)System.Math.Ceiling(x1 > x2 ? x1 : x2),
            (int)System.Math.Ceiling(y1 > y2 ? y1 : y2));
    }

    private static double ParseCoordinate(string s, string name, double max)
    {
        try
        {
            double val = double.Parse(s, CultureInfo.InvariantCulture);
            if (val > max)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Coordinate value for {0} must not exceed {1}",
                    name, max));
            }
            if (val < -max)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Coordinate value for {0} must be at least {1}",
                    name, -max));
            }
            return val;
        }
        catch (FormatException)
        {
            throw new ArgumentException(s + " is not a valid coordinate value");
        }
    }

    /// <summary>
    /// Creates a Box from a string that specifies four coordinates (west, south,
    /// east, north), in degrees longitude/latitude.
    /// </summary>
    public static Box FromWSEN(string s)
    {
        // TODO: fix this, make more lenient
        string[] coords = s.Split(',');
        if (coords.Length != 4)
        {
            throw new ArgumentException("Must specify 4 coordinate values (W,S,E,N)");
        }
        double west = ParseCoordinate(coords[0], "W", 180);
        double south = ParseCoordinate(coords[1], "S", 90);
        double east = ParseCoordinate(coords[2], "E", 180);
        double north = ParseCoordinate(coords[3], "N", 90);
        return OfWSEN(west, south, east, north);
    }

    /// <summary>
    /// Creates a bounding box that covers the entire world.
    /// </summary>
    public static Box OfWorld()
    {
        return new Box(
            int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
    }

    public Geometry ToGeometry(GeometryFactory factory)
    {
        int[] coords = new int[10];
        coords[0] = minX;
        coords[1] = minY;
        coords[2] = maxX;
        coords[3] = minY;
        coords[4] = maxX;
        coords[5] = maxY;
        coords[6] = minX;
        coords[7] = maxY;
        coords[8] = minX;
        coords[9] = minY;
        return factory.CreatePolygon(new GeoDesk.Feature.Store.WayCoordinateSequence(coords));
    }
}

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
public class Box : IBounds
{

    int _minX;
    int _minY;
    int _maxX;
    int _maxY;

    /// <summary>
    /// Creates an empty (null) box, ready to be expanded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box()</c>.</remarks>
    public Box()
    {
        SetNull();
    }

    /// <summary>
    /// Creates a box with the given minimum and maximum X/Y coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box(int, int, int, int)</c>.</remarks>
    public Box(int minX, int minY, int maxX, int maxY)
    {
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
    }

    /// <summary>
    /// Creates a box that copies the edges of the given bounds.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box(Bounds)</c>.</remarks>
    public Box(IBounds b)
    {
        _minX = b.MinX;
        _minY = b.MinY;
        _maxX = b.MaxX;
        _maxY = b.MaxY;
    }

    /// <summary>
    /// Creates a degenerate box covering the single point (x, y).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box(int, int)</c>.</remarks>
    public Box(int x, int y)
        : this(x, y, x, y)
    {
    }

    /// <summary>
    /// Resets this box to the empty (null) state.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.setNull()</c>.</remarks>
    public void SetNull()
    {
        _minX = int.MaxValue;
        _minY = int.MaxValue;
        _maxX = int.MinValue;
        _maxY = int.MinValue;
    }

    /// <summary>
    /// Returns true if this box straddles the antimeridian, indicated by <c>maxX</c> being less than
    /// <c>minX</c>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.crossesAntimeridian()</c>.</remarks>
    public bool CrossesAntimeridian()
    {
        return _maxX < _minX;
    }

    /// <summary>
    /// Returns true if this box is empty, indicated by <c>maxY</c> being less than <c>minY</c>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.isNull()</c>.</remarks>
    public bool IsNull()
    {
        return _maxY < _minY;
    }

    /// <summary>
    /// Returns true if the given bounds are empty (its <c>maxY</c> is less than its <c>minY</c>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.isNull(Bounds)</c>.</remarks>
    public static bool IsNullBounds(IBounds b)
    {
        return b.MaxY < b.MinY;
    }

    /// <summary>
    /// The minimum (western) X coordinate of this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <summary>
    /// The minimum (southern) Y coordinate of this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <summary>
    /// The maximum (eastern) X coordinate of this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.maxX()</c>.</remarks>
    public int MaxX => _maxX;

    /// <summary>
    /// The maximum (northern) Y coordinate of this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.maxY()</c>.</remarks>
    public int MaxY => _maxY;

    // Concrete implementations of the Bounds computed members. These both satisfy the interface and
    // remain callable directly on a Box reference (as in Java, where they are interface default
    // methods accessible on the instance). NOTE: do NOT forward to ((Bounds)this) — because these
    // methods implement the interface members, that would re-dispatch back here and recurse infinitely.

    /// <summary>
    /// Returns true if this box and the given box overlap.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.intersects(Bounds)</c>.</remarks>
    public bool Intersects(IBounds other)
    {
        return !(other.MinX > _maxX ||
            other.MaxX < _minX ||
            other.MinY > _maxY ||
            other.MaxY < _minY);
    }

    /// <summary>
    /// Returns true if this box contains the given point, handling antimeridian-crossing boxes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.contains(int, int)</c>.</remarks>
    public bool Contains(int x, int y)
    {
        if (_maxX < _minX)
            return (x >= _minX || x <= _maxX) && y >= _minY && y <= _maxY;
        return x >= _minX && x <= _maxX && y >= _minY && y <= _maxY;
    }

    /// <summary>
    /// Returns true if this box fully encloses the given box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.contains(Bounds)</c>.</remarks>
    public bool Contains(IBounds other)
    {
        return other.MinX >= _minX && other.MaxX <= _maxX &&
            other.MinY >= _minY && other.MaxY <= _maxY;
    }

    /// <summary>
    /// The width of this box in coordinate units (inclusive), or 0 if the box is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.width()</c>.</remarks>
    public long Width => (_maxY < _minY) ? 0 : ((((long)_maxX - _minX) & 0xffff_ffffL) + 1);

    /// <summary>
    /// The height of this box in coordinate units (inclusive), or 0 if the box is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.height()</c>.</remarks>
    public long Height => _maxY < _minY ? 0 : ((long)_maxY - _minY + 1);

    /// <summary>
    /// The area of this box in square coordinate units (<see cref="Width"/> times <see cref="Height"/>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.area()</c>.</remarks>
    public long Area => Width * Height;

    /// <summary>
    /// The X coordinate of this box's center.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerX()</c>.</remarks>
    public int CenterX => _minX + (_maxX - _minX) / 2;

    /// <summary>
    /// The Y coordinate of this box's center.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerY()</c>.</remarks>
    public int CenterY => _minY + (_maxY - _minY) / 2;

    /// <summary>
    /// Expands this box if necessary to include the given point. May convert an antimeridian-crossing
    /// box into a regular box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.expandToInclude(int, int)</c>.</remarks>
    // TODO: turns 180-crossing box into regular box
    public void ExpandToInclude(int x, int y)
    {
        if (x < _minX)
            _minX = x;
        if (x > _maxX)
            _maxX = x;
        if (y < _minY)
            _minY = y;
        if (y > _maxY)
            _maxY = y;
    }

    /// <summary>
    /// Expands this box if necessary to fully include the given box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.expandToInclude(Bounds)</c>.</remarks>
    public void ExpandToInclude(IBounds b)
    {
        var otherMinX = b.MinX;
        var otherMinY = b.MinY;
        var otherMaxX = b.MaxX;
        var otherMaxY = b.MaxY;
        if (otherMinX < _minX)
            _minX = otherMinX;
        if (otherMinY < _minY)
            _minY = otherMinY;
        if (otherMaxX > _maxX)
            _maxX = otherMaxX;
        if (otherMaxY > _maxY)
            _maxY = otherMaxY;
    }

    /// <summary>
    /// Expands this box if necessary to fully include the box described by the given edges.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.expandToInclude(int, int, int, int)</c>.</remarks>
    public void ExpandToInclude(int otherMinX, int otherMinY, int otherMaxX, int otherMaxY)
    {
        if (otherMinX < _minX)
            _minX = otherMinX;
        if (otherMinY < _minY)
            _minY = otherMinY;
        if (otherMaxX > _maxX)
            _maxX = otherMaxX;
        if (otherMaxY > _maxY)
            _maxY = otherMaxY;
    }

    /// <summary>
    /// Expands this box to include every point in the given flat X/Y coordinate array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.expandToInclude(int[])</c>.</remarks>
    public void ExpandToInclude(int[] coords)
    {
        for (var i = 0; i < coords.Length; i += 2)
            ExpandToInclude(coords[i], coords[i + 1]);
    }

    /// <summary>
    /// Converts this box into an equivalent JTS <see cref="Envelope"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.toEnvelope()</c>.</remarks>
    public Envelope ToEnvelope()
    {
        return new Envelope(_minX, _maxX, _minY, _maxY); // Envelope specifies both x first, then both y
    }

    /// <summary>
    /// Returns true if the other object is bounds with the same four edges as this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.equals(Object)</c>.</remarks>
    public override bool Equals(object? other)
    {
        if (other is not IBounds o)
            return false;
        return _minX == o.MinX && _maxX == o.MaxX && _minY == o.MinY && _maxY == o.MaxY;
    }

    /// <summary>
    /// Returns a hash code derived from this box's four edges.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return _minX.GetHashCode() ^ _minY.GetHashCode() ^ _maxX.GetHashCode() ^ _maxY.GetHashCode();
    }

    /// <summary>
    /// Creates a new bounding box that is the result of the intersection between this bounding box and
    /// another.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.intersection(Bounds)</c>.</remarks>
    // TODO: fix: what happens if boxes are empty?
    public Box Intersection(IBounds o)
    {
        var x1 = _minX > o.MinX ? _minX : o.MinX;
        var y1 = _minY > o.MinY ? _minY : o.MinY;
        var x2 = _maxX < o.MaxX ? _maxX : o.MaxX;
        var y2 = _maxY < o.MaxY ? _maxY : o.MaxY;
        if (x2 < x1 || y2 < y1)
            return new Box(); // no intersection
        return new Box(x1, y1, x2, y2);
    }

    /// <summary>
    /// Returns a new box representing the overlap of the two given boxes, or an empty box if they do
    /// not overlap.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.intersection(Bounds, Bounds)</c>.</remarks>
    public static Box Intersection(IBounds a, IBounds b)
    {
        var x1 = System.Math.Max(a.MinX, b.MinX);
        var y1 = System.Math.Max(a.MinY, b.MinY);
        var x2 = System.Math.Min(a.MaxX, b.MaxX);
        var y2 = System.Math.Min(a.MaxY, b.MaxY);
        if (x2 < x1 || y2 < y1)
            return new Box(); // no intersection
        return new Box(x1, y1, x2, y2);
    }

    /// <summary>
    /// Returns whichever of the two given boxes has the smaller area.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.smaller(Bounds, Bounds)</c>.</remarks>
    public static IBounds Smaller(IBounds a, IBounds b)
    {
        var areaA = (double)a.Width * a.Height;
        var areaB = (double)b.Width * b.Height;
        return (areaA < areaB) ? a : b;
    }

    /// <summary>Overflow-safe subtraction.</summary>
    /// <returns>the result of the subtraction; or the lowest negative value in case of an overflow</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.trimmedSubtract(int, int)</c>.</remarks>
    static int TrimmedSubtract(int x, int y)
    {
        var r = x - y;
        if (((x ^ y) & (x ^ r)) < 0)
            return int.MinValue;
        return r;
    }

    /// <summary>Overflow-safe addition.</summary>
    /// <returns>the result of the addition; or the highest positive value in case of an overflow</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.trimmedAdd(int, int)</c>.</remarks>
    static int TrimmedAdd(int x, int y)
    {
        var r = x + y;
        if (((x ^ r) & (y ^ r)) < 0)
            return int.MaxValue;
        return r;
    }

    /// <summary>
    /// Expands or contracts all sides of this bounding box by a specified number of imps. If the
    /// bounding box is empty, the result is undefined.
    /// </summary>
    /// <param name="b">the buffer (in imps)</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.buffer(int)</c>.</remarks>
    // TODO: define and test Antimeridian behaviour
    public void Buffer(int b)
    {
        _minX -= b;
        _maxX += b;
        if (b >= 0)
        {
            _minY = TrimmedSubtract(_minY, b);
            _maxY = TrimmedAdd(_maxY, b);
        }
        else
        {
            _minY = TrimmedAdd(_minY, -b);
            _maxY = TrimmedSubtract(_maxY, -b);
            if (_maxY < _minY)
                SetNull();
            // TODO: check if width flipped
        }
    }

    /// <summary>
    /// Expands or contracts all sides of this bounding box by a specified number of meters. If the
    /// bounding box is empty, the result is undefined.
    /// </summary>
    /// <param name="m">the buffer (in meters)</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.bufferMeters(double)</c>.</remarks>
    public void BufferMeters(double m)
    {
        Buffer((int)Mercator.DeltaFromMeters(m, CenterY));
    }

    /// <summary>
    /// Moves this box horizontally and vertically by the given deltas, using overflow-safe arithmetic
    /// on the Y axis.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.translate(int, int)</c>.</remarks>
    public void Translate(int deltaX, int deltaY)
    {
        _minX += deltaX;
        _maxX += deltaX;
        if (deltaY > 0)
        {
            _minY = TrimmedAdd(_minY, deltaY);
            _maxY = TrimmedAdd(_maxY, deltaY);
        }
        else
        {
            _minY = TrimmedSubtract(_minY, deltaY);
            _maxY = TrimmedSubtract(_maxY, deltaY);
        }
    }

    /// <summary>
    /// Returns a readable string form of this box (<c>[empty]</c> when null, otherwise its corner
    /// coordinates).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.toString()</c>.</remarks>
    public override string ToString()
    {
        return IsNull() ? "[empty]" : string.Format(CultureInfo.InvariantCulture, "[{0},{1} -> {2},{3}]", _minX, _minY, _maxX, _maxY);
    }

    /// <summary>
    /// Creates a box from west, south, east, and north edges given in degrees longitude/latitude,
    /// projecting them to Mercator coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.ofWSEN(double, double, double, double)</c>.</remarks>
    public static Box OfWSEN(double west, double south, double east, double north)
    {
        return new Box(Mercator.XFromLon(west), Mercator.YFromLat(south), Mercator.XFromLon(east), Mercator.YFromLat(north));
    }

    /// <summary>
    /// Creates a degenerate box covering the single longitude/latitude point, projected to Mercator
    /// coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.atLonLat(double, double)</c>.</remarks>
    public static Box AtLonLat(double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return new Box(x, y, x, y);
    }

    /// <summary>
    /// Creates a degenerate box covering the single Mercator point (x, y).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.atXY(int, int)</c>.</remarks>
    public static Box AtXY(int x, int y)
    {
        return new Box(x, y, x, y);
    }

    /// <summary>
    /// Creates a box from two corner coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.ofXYXY(int, int, int, int)</c>.</remarks>
    public static Box OfXYXY(int x1, int y1, int x2, int y2)
    {
        return new Box(x1, y1, x2, y2);
    }

    // TODO: decide what width/height mean
    /// <summary>
    /// Creates a box from a corner coordinate and a width and height in coordinate units.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.ofXYWidthHeight(int, int, int, int)</c>.</remarks>
    public static Box OfXYWidthHeight(int x, int y, int w, int h)
    {
        return new Box(x, y, x + w - 1, y + h - 1);
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative to a coordinate pair.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.metersAroundXY(double, int, int)</c>.</remarks>
    public static Box MetersAroundXY(double meters, int x, int y)
    {
        var b = (int)Mercator.DeltaFromMeters(meters, y);
        return new Box(x - b, TrimmedSubtract(y, b), x + b, TrimmedAdd(y, b));
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative to a coordinate pair.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.impsAroundXY(int, int, int)</c>.</remarks>
    public static Box ImpsAroundXY(int d, int x, int y)
    {
        return new Box(x - d, TrimmedSubtract(y, d), x + d, TrimmedAdd(y, d));
    }

    /// <summary>
    /// Creates a bounding box whose sides extend by a specific distance relative to a coordinate pair.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.metersAroundLonLat(double, double, double)</c>.</remarks>
    public static Box MetersAroundLonLat(double meters, double lon, double lat)
    {
        return MetersAroundXY(meters, Mercator.XFromLon(lon), Mercator.YFromLat(lat));
    }

    /// <summary>
    /// Creates a bounding box whose sides are extended by a specific distance relative to another
    /// bounding box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.metersAround(double, Bounds)</c>.</remarks>
    public static Box MetersAround(double meters, IBounds other)
    {
        var b = (int)Mercator.DeltaFromMeters(meters, other.CenterY);
        return new Box(other.MinX - b, TrimmedSubtract(other.MinY, b),
            other.MaxX + b, TrimmedAdd(other.MaxY, b));
    }

    // TODO: rename of()?
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.fromEnvelope(Envelope)</c>.</remarks>
    public static Box FromEnvelope(Envelope env)
    {
        return new Box(
            (int)System.Math.Floor(env.MinX),
            (int)System.Math.Floor(env.MinY + 0.5), // TODO: why not floor?
            (int)System.Math.Ceiling(env.MaxX),
            (int)System.Math.Floor(env.MaxY + 0.5)); // TODO: why not ceil?
    }

    /// <summary>Creates the tightest <c>Box</c> that encloses the given JTS Geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.of(Geometry)</c>.</remarks>
    public static Box Of(Geometry geom)
    {
        return FromEnvelope(geom.EnvelopeInternal);
    }

    /// <summary>Creates the tightest <c>Box</c> that encloses the given LineSegment.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.of(LineSegment)</c>.</remarks>
    public static Box Of(LineSegment seg)
    {
        var x1 = seg.P0.X;
        var y1 = seg.P0.Y;
        var x2 = seg.P1.X;
        var y2 = seg.P1.Y;
        return new Box(
            (int)System.Math.Floor(x1 < x2 ? x1 : x2),
            (int)System.Math.Floor(y1 < y2 ? y1 : y2),
            (int)System.Math.Ceiling(x1 > x2 ? x1 : x2),
            (int)System.Math.Ceiling(y1 > y2 ? y1 : y2));
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.parseCoordinate(String, String, double)</c>.</remarks>
    static double ParseCoordinate(string s, string name, double max)
    {
        try
        {
            var val = double.Parse(s, CultureInfo.InvariantCulture);
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
    /// Creates a Box from a string that specifies four coordinates (west, south, east, north), in
    /// degrees longitude/latitude.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.fromWSEN(String)</c>.</remarks>
    public static Box FromWSEN(string s)
    {
        // TODO: fix this, make more lenient
        var coords = s.Split(',');
        if (coords.Length != 4)
        {
            throw new ArgumentException("Must specify 4 coordinate values (W,S,E,N)");
        }
        var west = ParseCoordinate(coords[0], "W", 180);
        var south = ParseCoordinate(coords[1], "S", 90);
        var east = ParseCoordinate(coords[2], "E", 180);
        var north = ParseCoordinate(coords[3], "N", 90);
        return OfWSEN(west, south, east, north);
    }

    /// <summary>Creates a bounding box that covers the entire world.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.ofWorld()</c>.</remarks>
    public static Box OfWorld()
    {
        return new Box(
            int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Box.toGeometry(GeometryFactory)</c>.</remarks>
    public Geometry ToGeometry(GeometryFactory factory)
    {
        var coords = new int[10];
        coords[0] = _minX;
        coords[1] = _minY;
        coords[2] = _maxX;
        coords[3] = _minY;
        coords[4] = _maxX;
        coords[5] = _maxY;
        coords[6] = _minX;
        coords[7] = _maxY;
        coords[8] = _minX;
        coords[9] = _minY;
        return factory.CreatePolygon(new GeoDesk.Feature.Store.WayCoordinateSequence(coords));
    }

}

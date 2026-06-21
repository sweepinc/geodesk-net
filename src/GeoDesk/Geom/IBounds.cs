/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

// TODO: maybe at minLon, maxLat, etc.
// TODO: We need a meridian-safe bbox for queries that cross lon 180
//
// In Java the computed members are interface default methods; here they are C#
// default interface methods. They are callable on a reference typed as Bounds.
/// <summary>
/// An axis-aligned bounding box in Mercator-projected integer coordinates, exposed as its four
/// edges. Default members compute derived values (intersection, containment, size, center) from
/// those edges, so any implementer only needs to supply <see cref="MinX"/>, <see cref="MinY"/>,
/// <see cref="MaxX"/>, and <see cref="MaxY"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.Bounds</c>.</remarks>
public interface IBounds
{

    /// <summary>
    /// The minimum (western) X coordinate of the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.minX()</c>.</remarks>
    int MinX { get; }

    /// <summary>
    /// The minimum (southern) Y coordinate of the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.minY()</c>.</remarks>
    int MinY { get; }

    /// <summary>
    /// The maximum (eastern) X coordinate of the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.maxX()</c>.</remarks>
    int MaxX { get; }

    /// <summary>
    /// The maximum (northern) Y coordinate of the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.maxY()</c>.</remarks>
    int MaxY { get; }

    // TODO: doesn't work if both cross the 180
    /// <summary>
    /// Returns true if this box and the given box overlap (share at least one point).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.intersects(Bounds)</c>.</remarks>
    bool Intersects(IBounds other)
    {
        return !(other.MinX > MaxX ||
            other.MaxX < MinX ||
            other.MinY > MaxY ||
            other.MaxY < MinY);
    }

    /// <summary>
    /// Returns true if the box contains the given point. Handles boxes that wrap across the
    /// antimeridian (where <see cref="MaxX"/> is less than <see cref="MinX"/>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.contains(int, int)</c>.</remarks>
    bool Contains(int x, int y)
    {
        var minX = MinX;
        var maxX = MaxX;
        if (maxX < minX)
            return (x >= minX || x <= maxX) && y >= MinY && y <= MaxY;
        else
            return x >= minX && x <= maxX && y >= MinY && y <= MaxY;
    }

    // TODO: assumes Bounds are non-null!
    /// <summary>
    /// Returns true if this box fully encloses the given box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.contains(Bounds)</c>.</remarks>
    bool Contains(IBounds other)
    {
        return other.MinX >= MinX && other.MaxX <= MaxX && other.MinY >= MinY && other.MaxY <= MaxY;
    }

    // TODO: check these, calculations are not consistent

    /// <summary>
    /// The width of the box in coordinate units (inclusive of both edges), or 0 if the box is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.width()</c>.</remarks>
    long Width => (MaxY < MinY) ? 0 : ((((long)MaxX - MinX) & 0xffff_ffffL) + 1);

    /// <summary>
    /// The height of the box in coordinate units (inclusive of both edges), or 0 if the box is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.height()</c>.</remarks>
    long Height => MaxY < MinY ? 0 : ((long)MaxY - MinY + 1);

    // TODO: may overflow
    /// <summary>
    /// The area of the box in square coordinate units (<see cref="Width"/> times <see cref="Height"/>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.area()</c>.</remarks>
    long Area => Width * Height;

    /// <summary>
    /// The X coordinate of the box's center.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerX()</c>.</remarks>
    int CenterX => MinX + (MaxX - MinX) / 2;

    /// <summary>
    /// The Y coordinate of the box's center.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerY()</c>.</remarks>
    int CenterY => MinY + (MaxY - MinY) / 2;

}

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
/// <remarks>Ported from Java <c>com.geodesk.geom.Bounds</c>.</remarks>
public interface IBounds
{

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.minX()</c>.</remarks>
    int MinX { get; }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.minY()</c>.</remarks>
    int MinY { get; }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.maxX()</c>.</remarks>
    int MaxX { get; }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.maxY()</c>.</remarks>
    int MaxY { get; }

    // TODO: doesn't work if both cross the 180
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.intersects(Bounds)</c>.</remarks>
    bool Intersects(IBounds other)
    {
        return !(other.MinX > MaxX ||
            other.MaxX < MinX ||
            other.MinY > MaxY ||
            other.MaxY < MinY);
    }

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
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.contains(Bounds)</c>.</remarks>
    bool Contains(IBounds other)
    {
        return other.MinX >= MinX && other.MaxX <= MaxX && other.MinY >= MinY && other.MaxY <= MaxY;
    }

    // TODO: check these, calculations are not consistent

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.width()</c>.</remarks>
    long Width => (MaxY < MinY) ? 0 : ((((long)MaxX - MinX) & 0xffff_ffffL) + 1);

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.height()</c>.</remarks>
    long Height => MaxY < MinY ? 0 : ((long)MaxY - MinY + 1);

    // TODO: may overflow
    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.area()</c>.</remarks>
    long Area => Width * Height;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerX()</c>.</remarks>
    int CenterX => MinX + (MaxX - MinX) / 2;

    /// <remarks>Ported from Java <c>com.geodesk.geom.Bounds.centerY()</c>.</remarks>
    int CenterY => MinY + (MaxY - MinY) / 2;

}

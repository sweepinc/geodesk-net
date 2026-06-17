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
public interface Bounds
{
    int MinX { get; }
    int MinY { get; }
    int MaxX { get; }
    int MaxY { get; }

    // TODO: doesn't work if both cross the 180
    bool Intersects(Bounds other)
    {
        return !(other.MinX > MaxX ||
            other.MaxX < MinX ||
            other.MinY > MaxY ||
            other.MaxY < MinY);
    }

    bool Contains(int x, int y)
    {
        int minX = MinX;
        int maxX = MaxX;
        if (maxX < minX) return (x >= minX || x <= maxX) && y >= MinY && y <= MaxY;
        return x >= minX && x <= maxX && y >= MinY && y <= MaxY;
    }

    // TODO: assumes Bounds are non-null!
    bool Contains(Bounds other)
    {
        return other.MinX >= MinX && other.MaxX <= MaxX &&
            other.MinY >= MinY && other.MaxY <= MaxY;
    }

    // TODO: check these, calculations are not consistent

    long Width => (MaxY < MinY) ? 0 : ((((long)MaxX - MinX) & 0xffff_ffffL) + 1);

    long Height => MaxY < MinY ? 0 : ((long)MaxY - MinY + 1);

    // TODO: may overflow
    long Area => Width * Height;

    int CenterX => MinX + (MaxX - MinX) / 2;

    int CenterY => MinY + (MaxY - MinY) / 2;
}

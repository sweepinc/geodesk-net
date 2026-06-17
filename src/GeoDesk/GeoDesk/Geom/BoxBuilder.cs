/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

public class BoxBuilder : Bounds
{
    private int minX;
    private int minY;
    private int maxX;
    private int maxY;

    public void Reset()
    {
        minX = int.MaxValue;
        minY = int.MaxValue;
        maxX = int.MinValue;
        maxY = int.MinValue;
    }

    public bool IsNull()
    {
        return maxY < minY;
    }

    public int MinX => minX;

    public int MinY => minY;

    public int MaxX => maxX;

    public int MaxY => maxY;

    /// <summary>
    /// Checks if this bounding box includes the given coordinate, and expands
    /// it if necessary.
    /// </summary>
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
}

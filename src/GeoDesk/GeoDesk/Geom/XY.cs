/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

/// <summary>
/// Methods for working with coordinates that are represented as a single <c>long</c>
/// value. Y coordinate is stored in the upper 32 bits, X in the lower.
/// </summary>
public static class XY
{
    /// <summary>
    /// Creates a <c>long</c> coordinate based on the given X and Y.
    /// </summary>
    public static long Of(int x, int y)
    {
        return ((long)y << 32) | ((long)x & 0xffff_ffffL);
    }

    /// <summary>
    /// Creates a <c>long</c> coordinate based on the given JTS Coordinate.
    /// Coordinates must be within the numeric range of a signed 32-bit integer
    /// and are rounded.
    /// </summary>
    public static long Of(Coordinate c)
    {
        return Of((int)System.Math.Floor(c.X + 0.5), (int)System.Math.Floor(c.Y + 0.5));
    }

    /// <summary>
    /// Turns a <c>long</c> coordinate into a JTS Coordinate.
    /// </summary>
    public static Coordinate ToCoordinate(long xy)
    {
        return new Coordinate(X(xy), Y(xy));
    }

    public static Coordinate[] ToCoordinates(long[] xy)
    {
        Coordinate[] coords = new Coordinate[xy.Length];
        for (int i = 0; i < xy.Length; i++)
        {
            coords[i] = ToCoordinate(xy[i]);
        }
        return coords;
    }

    /// <summary>
    /// Returns the X coordinate of the given <c>long</c> coordinate.
    /// </summary>
    public static int X(long coord)
    {
        return (int)coord;
    }

    /// <summary>
    /// Returns the Y coordinate of the given <c>long</c> coordinate.
    /// </summary>
    public static int Y(long coord)
    {
        return (int)(coord >> 32);
    }

    /// <summary>
    /// Returns an array of <c>long</c> coordinates as an array of x/y coordinate pairs.
    /// </summary>
    public static int[] Of(long[] coords)
    {
        int[] xy = new int[coords.Length * 2];
        for (int i = 0; i < coords.Length; i++)
        {
            xy[i * 2] = X(coords[i]);
            xy[i * 2 + 1] = Y(coords[i]);
        }
        return xy;
    }

    /// <summary>
    /// Checks whether a given set of coordinates represents a linear ring.
    /// </summary>
    public static bool IsClosed(int[] coords)
    {
        int len = coords.Length;
        if (len < 6) return false;
        return coords[0] == coords[len - 2] && coords[1] == coords[len - 1];
    }

    public static bool Contains(int[] coords, int x, int y)
    {
        int len = coords.Length;
        for (int i = 0; i < len; i += 2)
        {
            if (coords[i] == x && coords[i + 1] == y) return true;
        }
        return false;
    }

    /// <summary>
    /// Fast but non-robust method to check how many times a line from a point
    /// intersects the given segments, using the ray-casting algorithm.
    /// </summary>
    /// <param name="coords">pairs of x/y coordinates that form a polygon or segment thereof</param>
    /// <param name="cx">the X-coordinate to test</param>
    /// <param name="cy">the Y-coordinate to test</param>
    /// <returns>0 if even number of edges are crossed, 1 if odd</returns>
    public static int CastRay(int[] coords, double cx, double cy)
    {
        int odd = 0;
        int len = coords.Length - 2;
        for (int i = 0; i < len; i += 2)
        {
            double x1 = coords[i];
            double y1 = coords[i + 1];
            double x2 = coords[i + 2];
            double y2 = coords[i + 3];

            if (((y1 <= cy) && (y2 > cy))     // upward crossing
                || ((y1 > cy) && (y2 <= cy))) // downward crossing
            {
                // compute edge-ray intersect x-coordinate
                double vt = (cy - y1) / (y2 - y1);
                if (cx < x1 + vt * (x2 - x1)) // P.x < intersect
                {
                    odd ^= 1;
                }
            }
        }
        return odd;
    }
}

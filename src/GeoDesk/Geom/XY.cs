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
/// <remarks>Ported from Java <c>com.geodesk.geom.XY</c>.</remarks>
internal static class XY
{

    /// <summary>
    /// Packs the given X and Y into a single <c>long</c> coordinate (Y in the upper 32 bits, X in the
    /// lower).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.of(int, int)</c>.</remarks>
    public static long Of(int x, int y)
    {
        return ((long)y << 32) | ((long)x & 0xffff_ffffL);
    }

    /// <summary>
    /// Creates a <c>long</c> coordinate based on the given JTS Coordinate. Coordinates must be within
    /// the numeric range of a signed 32-bit integer and are rounded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.of(Coordinate)</c>.</remarks>
    public static long Of(Coordinate c)
    {
        return Of((int)System.Math.Floor(c.X + 0.5), (int)System.Math.Floor(c.Y + 0.5));
    }

    /// <summary>
    /// Unpacks a <c>long</c> coordinate into a JTS <see cref="Coordinate"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.toCoordinate(long)</c>.</remarks>
    public static Coordinate ToCoordinate(long xy)
    {
        return new Coordinate(X(xy), Y(xy));
    }

    /// <summary>
    /// Unpacks an array of <c>long</c> coordinates into an array of JTS <see cref="Coordinate"/> objects.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.toCoordinates(long[])</c>.</remarks>
    public static Coordinate[] ToCoordinates(long[] xy)
    {
        var coords = new Coordinate[xy.Length];
        for (var i = 0; i < xy.Length; i++)
        {
            coords[i] = ToCoordinate(xy[i]);
        }
        return coords;
    }

    /// <summary>
    /// Extracts the X coordinate (lower 32 bits) from the given <c>long</c> coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.x(long)</c>.</remarks>
    public static int X(long coord)
    {
        return (int)coord;
    }

    /// <summary>
    /// Extracts the Y coordinate (upper 32 bits) from the given <c>long</c> coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.y(long)</c>.</remarks>
    public static int Y(long coord)
    {
        return (int)(coord >> 32);
    }

    /// <summary>
    /// Unpacks an array of <c>long</c> coordinates into a flat <c>int</c> array of X/Y pairs (X at
    /// even indexes, Y at odd indexes).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.of(long[])</c>.</remarks>
    public static int[] Of(long[] coords)
    {
        var xy = new int[coords.Length * 2];
        for (var i = 0; i < coords.Length; i++)
        {
            xy[i * 2] = X(coords[i]);
            xy[i * 2 + 1] = Y(coords[i]);
        }
        return xy;
    }

    /// <summary>
    /// Checks whether the given flat X/Y coordinate array forms a closed linear ring (at least three
    /// points with the first and last coinciding).
    /// </summary>
    /// <param name="coords">array of X/Y coordinates</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.isClosed(int[])</c>.</remarks>
    public static bool IsClosed(int[] coords)
    {
        var len = coords.Length;
        if (len < 6) return false;
        return coords[0] == coords[len - 2] && coords[1] == coords[len - 1];
    }

    /// <summary>
    /// Returns true if the given flat X/Y coordinate array contains the exact point (x, y).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.contains(int[], int, int)</c>.</remarks>
    public static bool Contains(int[] coords, int x, int y)
    {
        var len = coords.Length;
        for (var i = 0; i < len; i += 2)
        {
            if (coords[i] == x && coords[i + 1] == y) return true;
        }
        return false;
    }

    /// <summary>
    /// Fast but non-robust method to check how many times a line from a point intersects the given
    /// segments, using the ray-casting algorithm. This is suitable for a point-in-polygon test, but
    /// be aware that points that are vertexes or are located on the edge (or very close to it) may or
    /// may not be considered "inside." This test can be applied to multiple line strings of the
    /// polygon in succession; in that case, the result of each test must be XOR'd with the previous
    /// results. The winding order is irrelevant, but the result is undefined if the segments are
    /// self-intersecting.
    /// </summary>
    /// <param name="coords">pairs of x/y coordinates that form a polygon or segment thereof</param>
    /// <param name="cx">the X-coordinate to test</param>
    /// <param name="cy">the Y-coordinate to test</param>
    /// <returns>0 if even number of edges are crossed ("not inside"), 1 if odd ("inside")</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.XY.castRay(int[], double, double)</c>.</remarks>
    public static int CastRay(int[] coords, double cx, double cy)
    {
        var odd = 0;
        var len = coords.Length - 2;
        for (var i = 0; i < len; i += 2)
        {
            double x1 = coords[i];
            double y1 = coords[i + 1];
            double x2 = coords[i + 2];
            double y2 = coords[i + 3];

            if (((y1 <= cy) && (y2 > cy))     // upward crossing
                || ((y1 > cy) && (y2 <= cy))) // downward crossing
            {
                // compute edge-ray intersect x-coordinate
                var vt = (cy - y1) / (y2 - y1);
                if (cx < x1 + vt * (x2 - x1)) // P.x < intersect
                {
                    odd ^= 1;
                }
            }
        }
        return odd;
    }

}

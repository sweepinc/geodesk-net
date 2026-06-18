/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates</c>.</remarks>
internal static class Coordinates
{

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.envelope(int[])</c>.</remarks>
    public static Envelope Envelope(int[] coords)
    {
        var env = new Envelope();
        for (var i = 0; i < coords.Length; i += 2)
            env.ExpandToInclude(coords[i], coords[i + 1]);

        return env;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.bounds(int[])</c>.</remarks>
    public static Box Bounds(int[] coords)
    {
        var bbox = new Box();
        for (var i = 0; i < coords.Length; i += 2)
            bbox.ExpandToInclude(coords[i], coords[i + 1]);

        return bbox;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.fixMissing(int[], int, int)</c>.</remarks>
    public static bool FixMissing(int[] c, int nullX, int nullY)
    {
        var success = true;

        for (var i = 0; i < c.Length; i += 2)
        {
            if (c[i] == nullX && c[i + 1] == nullY)
            {
                if (i > 0)
                {
                    c[i] = c[i - 2];
                    c[i + 1] = c[i - 1];
                }
                else
                {
                    var valid = FindValid(c, i + 2, nullX, nullY);
                    if (valid < 0)
                    {
                        success = false;
                    }
                    else
                    {
                        c[i] = c[valid];
                        c[i + 1] = c[valid + 1];
                    }
                }
            }
        }

        return success;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.findValid(int[], int, int, int)</c>.</remarks>
    static int FindValid(int[] c, int index, int nullX, int nullY)
    {
        while (index < c.Length)
        {
            if (c[index] != nullX || c[index + 1] != nullY)
                return index;
            index += 2;
        }

        return -1;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.countLongDeltas(int[])</c>.</remarks>
    public static int CountLongDeltas(int[] c)
    {
        var count = 0;
        for (var i = 2; i < c.Length; i += 2)
        {
            var xDelta = c[i] - c[i - 2];
            if (xDelta > short.MaxValue || xDelta < short.MinValue)
            {
                count++;
                continue;
            }

            var yDelta = c[i + 1] - c[i - 1];
            if (yDelta > short.MaxValue || yDelta < short.MinValue)
            {
                count++;
            }
        }

        return count;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.isClosedRing(int[])</c>.</remarks>
    public static bool IsClosedRing(int[] coords)
    {
        return coords[0] == coords[coords.Length - 2] && coords[1] == coords[coords.Length - 1];
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.fromCoordinates(Coordinate[])</c>.</remarks>
    public static double[] FromCoordinates(Coordinate[] coords)
    {
        var points = new double[coords.Length * 2];
        for (var i = 0; i < coords.Length; i++)
        {
            points[i * 2] = coords[i].X;
            points[i * 2 + 1] = coords[i].Y;
        }
        return points;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.fromCoordinateSequence(CoordinateSequence)</c>.</remarks>
    public static double[] FromCoordinateSequence(CoordinateSequence coords)
    {
        var points = new double[coords.Count * 2];
        for (var i = 0; i < coords.Count; i++)
        {
            points[i * 2] = coords.GetX(i);
            points[i * 2 + 1] = coords.GetY(i);
        }

        return points;
    }

}

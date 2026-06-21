/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

/// <summary>
/// Helpers for working with flat <c>int[]</c> coordinate arrays (X/Y pairs) and converting between
/// JTS coordinate representations and GeoDesk's compact arrays.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates</c>.</remarks>
internal static class Coordinates
{

    /// <summary>
    /// Computes the JTS <see cref="Envelope"/> enclosing all points in the given flat X/Y array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.envelope(int[])</c>.</remarks>
    public static Envelope Envelope(int[] coords)
    {
        var env = new Envelope();
        for (var i = 0; i < coords.Length; i += 2)
            env.ExpandToInclude(coords[i], coords[i + 1]);

        return env;
    }

    /// <summary>
    /// Computes the <see cref="Box"/> bounding box enclosing all points in the given flat X/Y array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.bounds(int[])</c>.</remarks>
    public static Box Bounds(int[] coords)
    {
        var bbox = new Box();
        for (var i = 0; i < coords.Length; i += 2)
            bbox.ExpandToInclude(coords[i], coords[i + 1]);

        return bbox;
    }

    /// <summary>
    /// Replaces any "null" coordinate (equal to the given <paramref name="nullX"/>/<paramref name="nullY"/>
    /// sentinel) with the nearest preceding valid coordinate, or the next valid one for a leading null.
    /// Returns false if no valid coordinate exists to substitute.
    /// </summary>
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

    /// <summary>
    /// Scans forward from the given index for the first coordinate that is not the null sentinel,
    /// returning its index or -1 if none is found.
    /// </summary>
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

    /// <summary>
    /// Counts how many consecutive coordinate deltas in the array exceed the range of a signed 16-bit
    /// value (in either X or Y), used when estimating compact encoding size.
    /// </summary>
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

    /// <summary>
    /// Returns true if the flat X/Y array's first and last points coincide, forming a closed ring.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Coordinates.isClosedRing(int[])</c>.</remarks>
    public static bool IsClosedRing(int[] coords)
    {
        return coords[0] == coords[coords.Length - 2] && coords[1] == coords[coords.Length - 1];
    }

    /// <summary>
    /// Flattens an array of JTS <see cref="Coordinate"/> objects into a <c>double[]</c> of X/Y pairs.
    /// </summary>
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

    /// <summary>
    /// Flattens a JTS <see cref="CoordinateSequence"/> into a <c>double[]</c> of X/Y pairs.
    /// </summary>
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

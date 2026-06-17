/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

public static class Coordinates
{
    public static Envelope Envelope(int[] coords)
    {
        Envelope env = new Envelope();
        for (int i = 0; i < coords.Length; i += 2)
        {
            env.ExpandToInclude(coords[i], coords[i + 1]);
        }
        return env;
    }

    public static Box Bounds(int[] coords)
    {
        Box bbox = new Box();
        for (int i = 0; i < coords.Length; i += 2)
        {
            bbox.ExpandToInclude(coords[i], coords[i + 1]);
        }
        return bbox;
    }

    public static bool FixMissing(int[] c, int nullX, int nullY)
    {
        bool success = true;

        for (int i = 0; i < c.Length; i += 2)
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
                    int valid = FindValid(c, i + 2, nullX, nullY);
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

    private static int FindValid(int[] c, int index, int nullX, int nullY)
    {
        while (index < c.Length)
        {
            if (c[index] != nullX || c[index + 1] != nullY) return index;
            index += 2;
        }
        return -1;
    }

    public static int CountLongDeltas(int[] c)
    {
        int count = 0;
        for (int i = 2; i < c.Length; i += 2)
        {
            int xDelta = c[i] - c[i - 2];
            if (xDelta > short.MaxValue || xDelta < short.MinValue)
            {
                count++;
                continue;
            }
            int yDelta = c[i + 1] - c[i - 1];
            if (yDelta > short.MaxValue || yDelta < short.MinValue)
            {
                count++;
            }
        }
        return count;
    }

    public static bool IsClosedRing(int[] coords)
    {
        return coords[0] == coords[coords.Length - 2] &&
            coords[1] == coords[coords.Length - 1];
    }

    public static double[] FromCoordinates(Coordinate[] coords)
    {
        double[] points = new double[coords.Length * 2];
        for (int i = 0; i < coords.Length; i++)
        {
            points[i * 2] = coords[i].X;
            points[i * 2 + 1] = coords[i].Y;
        }
        return points;
    }

    public static double[] FromCoordinateSequence(CoordinateSequence coords)
    {
        double[] points = new double[coords.Count * 2];
        for (int i = 0; i < coords.Count; i++)
        {
            points[i * 2] = coords.GetX(i);
            points[i * 2 + 1] = coords.GetY(i);
        }
        return points;
    }
}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

// PORT-DEFERRED (Phase 5): the testFast(StoredWay.XYIterator, double, double) overload
// is omitted until StoredWay is ported with the feature store layer.
public static class PointInPolygon
{
    // return -1 if vertex, 1 if inside, 0 if outside
    public static bool IsInside(int[] coords, double cx, double cy)
    {
        bool odd = false;
        int len = coords.Length - 2;
        for (int i = 0; i < len; i += 2)
        {
            double x1 = coords[i];
            double y1 = coords[i + 1];
            double x2 = coords[i + 2];
            double y2 = coords[i + 3];

            // Added this so vertices are always considered inside
            if (cx == x1 && cy == y1) return true;

            if (((y1 <= cy) && (y2 > cy))     // upward crossing
                || ((y1 > cy) && (y2 <= cy))) // downward crossing
            {
                // compute edge-ray intersect x-coordinate
                double vt = (cy - y1) / (y2 - y1);
                if (cx < x1 + vt * (x2 - x1)) // P.x < intersect
                {
                    odd = !odd;
                }
            }
        }
        return odd;
    }

    /// <summary>
    /// Fast but non-robust point-in-polygon test using the ray-crossing method.
    /// Points located on a polygon edge (or very close to it) may or may not
    /// be considered "inside." Vertexes, however, are always identified correctly.
    /// </summary>
    /// <param name="coords">pairs of x/y coordinates that form a polygon or part thereof</param>
    /// <param name="cx">the X-coordinate to test</param>
    /// <param name="cy">the Y-coordinate to test</param>
    /// <returns>0 if even number of edges are crossed, 1 if odd</returns>
    public static int TestFast(int[] coords, double cx, double cy)
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

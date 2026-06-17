/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Clarisma.Common.Util;
using Xunit;
using static GeoDesk.Geom.PointInPolygon;

namespace GeoDesk.Geom;

public class PointInPolygonTest
{
    private static readonly int[] P =
    {
        -400, 200,
        -200, 500,
        100, 500,
        400, 200,
        -200, -300,
        -400, -100,
        -400, 200
    };

    private static readonly int[] P2 =
    {
        -400, -100,
        -700, 0,
        -500, -600,
        -200, -300,
        -400, -100
    };

    private static readonly int[] P3 =
    {
        -400, -100,
        -200, -300,
        -500, -600,
        -700, 0,
        -400, -100
    };

    private static readonly int[] points =
    {
        -200, 200, 1,
        200, -200, 0,
        200, 500, 0,
        -300, 0, 1,
        100, 0, 1,
        300, 0, 0,
        300, 200, 1,
        350, 300, 0,
        -400, -200, 0,
        0, 400, 1,
        0, 499, 1,
        0, 501, 0,
        0, 600, 0,
        0, -100, 1,
        0, -300, 0,
        100, 300, 1,
        -400, 100, 1
    };

    private static readonly int[] R =
    {
        -200, 200,
        200, 200,
        200, -200,
        -200, -200,
        -200, 200
    };

    private static readonly int[] rpoints =
    {
        -100, 200, 1,
        100, 200, 1,
        -200, 100, 1,
        -200, -100, 1,
        100, -200, 1,
        -100, -200, 1,
        -200, -100, 1,
        -200, 100, 1
    };

    private static void TestVertices(string s, int[] p)
    {
        Log.Debug(s);
        for (int i = 0; i < p.Length; i += 2)
        {
            int x = p[i];
            int y = p[i + 1];
            Log.Debug("%d, %d: %s", x, y, IsInside(p, x, y));
        }
    }

    private static void TestPointsFast(int[] polygon, int[] points)
    {
        for (int i = 0; i < points.Length; i += 3)
        {
            int x = points[i];
            int y = points[i + 1];
            int expected = points[i + 2];
            Assert.Equal(expected, TestFast(polygon, x, y));
        }
    }

    [Fact]
    public void TestPointsFastCase()
    {
        TestPointsFast(P, points);
    }

    private static void TestPoints(int[] polygon, int[] points)
    {
        for (int i = 0; i < points.Length; i += 3)
        {
            int x = points[i];
            int y = points[i + 1];
            Log.Debug("%d, %d: %s", x, y, IsInside(polygon, x, y));
        }
    }

    [Fact]
    public void TestPointsCase()
    {
        TestPoints(R, rpoints);
    }

    [Fact]
    public void TestIsInside()
    {
        TestVertices("P1", P);
        TestVertices("P2", P2);
        TestVertices("P3", P3);
    }
}

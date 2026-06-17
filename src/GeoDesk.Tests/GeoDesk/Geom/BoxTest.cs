/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Clarisma.Common.Util;
using Xunit;

namespace GeoDesk.Geom;

public class BoxTest
{
    private static void TestContain(Box box, int x, int y, bool shouldContain)
    {
        Assert.Equal(shouldContain, box.Contains(x, y));
    }

    private static void TestIntersect(Box a, Box b, bool res)
    {
        Assert.Equal(res, a.Intersects(b));
        Assert.Equal(res, b.Intersects(a));
    }

    [Fact]
    public void Test()
    {
        Box box = Box.OfWorld();
        Log.Debug("World: %s", box);
        box.Buffer(10);
        Log.Debug("World + 10: %s", box);
        box.Buffer(-10);
        Log.Debug("World - 10: %s", box);
        box.Buffer(int.MinValue);
        Log.Debug("World - 10 - max: %s", box);

        box = new Box();
        Log.Debug("Should be empty: %s", box);
        box.ExpandToInclude(90, 100);
        Log.Debug("Should contain 90,100: %s", box);
        box.ExpandToInclude(-4000, -8000);
        Log.Debug("Added -4K,-8K: %s", box);
        box.Buffer(200);
        Log.Debug(" + 200: %s", box);
        TestContain(box, 0, 0, true);
        TestContain(box, -7000, -3000, false);

        Box box2 = Box.OfWSEN(170, -40, -160, 30);
        TestContain(box2, int.MinValue, -3000, true);
        TestContain(box2, int.MaxValue, -3000, true);
        TestContain(box2, 0, 0, false);
    }

    private readonly Box EMPTY = new Box();
    private readonly Box A = new Box(-800, 600, -100, 800);
    private readonly Box B = new Box(100, 500, 700, 800);
    private readonly Box C = new Box(-900, int.MinValue, -700, -200);
    private readonly Box D = new Box(300, -700, 800, -300);
    private readonly Box E = new Box(-300, 300, 200, 900);
    private readonly Box F = new Box(-700, 200, -200, 700);
    private readonly Box G = new Box(600, 300, int.MaxValue, 600);
    private readonly Box H = new Box(-800, -300, 500, 300);

    private readonly Box AE = new Box(-300, 600, -100, 800);
    private readonly Box MAX = new Box(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
    private readonly Box INVALID = new Box(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
    private readonly Box INVALID2 = new Box(200, 200, 100, 100);

    [Fact]
    public void TestIntersection()
    {
        CheckIntersection(A, B, EMPTY);
        CheckIntersection(A, EMPTY, EMPTY);
        CheckIntersection(EMPTY, B, EMPTY);
        CheckIntersection(A, E, AE);
        CheckIntersection(A, MAX, A);
        CheckIntersection(B, MAX, B);
        CheckIntersection(MAX, EMPTY, EMPTY);
        CheckIntersection(MAX, MAX, MAX);
        CheckIntersection(INVALID, EMPTY, EMPTY);
        CheckIntersection(INVALID, INVALID, EMPTY);
        CheckIntersection(A, INVALID, EMPTY);
        CheckIntersection(MAX, INVALID, EMPTY);
        CheckIntersection(INVALID2, INVALID, EMPTY);
        CheckIntersection(INVALID2, A, EMPTY);
        CheckIntersection(INVALID2, MAX, EMPTY);
        CheckIntersection(INVALID2, INVALID2, EMPTY);
    }

    private static void CheckIntersection(Box a, Box b, Box c)
    {
        Assert.Equal(c, a.Intersection(b));
        Assert.Equal(c, b.Intersection(a));
        Assert.Equal(c, Box.Intersection(a, b));
    }

    [Fact]
    public void TestIntersects()
    {
        TestIntersect(A, B, false);
        TestIntersect(A, C, false);
        TestIntersect(A, D, false);
        TestIntersect(B, C, false);
        TestIntersect(B, D, false);
        TestIntersect(C, D, false);

        TestIntersect(E, A, true);
        TestIntersect(E, B, true);
        TestIntersect(E, C, false);
        TestIntersect(E, D, false);

        TestIntersect(F, A, true);
        TestIntersect(F, B, false);
        TestIntersect(F, C, false);
        TestIntersect(F, D, false);
        TestIntersect(F, E, true);

        TestIntersect(G, A, false);
        TestIntersect(G, B, true);
        TestIntersect(G, C, false);
        TestIntersect(G, D, false);
        TestIntersect(G, E, false);
        TestIntersect(G, F, false);

        TestIntersect(H, A, false);
        TestIntersect(H, B, false);
        TestIntersect(H, C, true);
        TestIntersect(H, D, true);
        TestIntersect(H, E, true);
        TestIntersect(H, F, true);
        TestIntersect(H, G, false);
    }

    [Fact]
    public void TestArea()
    {
        Assert.Equal(140901L, A.Area);
        Assert.Equal(200901L, D.Area);
    }

    [Fact]
    public void TestTranslate()
    {
        Box h = new Box(H);
        Log.Debug(h);
        h.Translate(int.MinValue, 0);
        Log.Debug(h);
        h.Translate(int.MinValue, 0);
        Log.Debug(h);
    }
}

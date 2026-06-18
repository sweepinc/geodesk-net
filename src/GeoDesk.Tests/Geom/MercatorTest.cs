/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using Xunit;

using static GeoDesk.Geom.Mercator;

namespace GeoDesk.Tests.Geom;

public class MercatorTest
{
    // Java Math.round(double) == floor(a + 0.5); replicate to match test expectations.
    private static long JRound(double a) => (long)System.Math.Floor(a + 0.5);

    private static void AssertClose(double expected, double actual, double delta)
    {
        Assert.True(System.Math.Abs(expected - actual) <= delta,
            $"expected {expected}, got {actual} (delta {delta})");
    }

    [Fact]
    public void TestProjection()
    {
        int x, y;

        x = Mercator.XFromLon(-180);
        Assert.Equal(int.MinValue + 1, x);
        x = Mercator.XFromLon(180);
        Assert.Equal(int.MaxValue, x);
        y = Mercator.YFromLat(-90);
        Assert.Equal(int.MinValue, y);
        y = Mercator.YFromLat(90);
        Assert.Equal(int.MaxValue, y);

        y = Mercator.YFromLat(MinLat);
        Assert.Equal(int.MinValue, y);
        y = Mercator.YFromLat(MaxLat);
        Assert.Equal(int.MaxValue, y);

        double lon, lat;
        const double DELTA = 1e-8;

        double minLat = (double)JRound(MinLat * 10000000) / 10000000;
        double maxLat = (double)JRound(MaxLat * 10000000) / 10000000;

        lon = Mercator.LonPrecision7FromX(int.MinValue + 1);
        AssertClose(-180, lon, DELTA);
        lon = Mercator.LonPrecision7FromX(int.MaxValue);
        AssertClose(180, lon, DELTA);
        lat = Mercator.LatPrecision7FromY(int.MinValue);
        AssertClose(minLat, lat, DELTA);
        lat = Mercator.LatPrecision7FromY(int.MaxValue);
        AssertClose(maxLat, lat, DELTA);
    }

    [Fact]
    public void TestInvalidLatitude()
    {
        Assert.Throws<ArgumentException>(() => Mercator.YFromLat(999999));
        Assert.Throws<ArgumentException>(() => Mercator.YFromLat(-999999));
    }

    private static readonly string P1 = "POLYGON ((137186237 667219324, 137185189 667220565, 137187672 667222660, 137193199 667216107, 137194247 667214866, 137191764 667212771, 137186237 667219324))";

    [Fact]
    public void TestArea()
    {
        Geometry geom = new WKTReader().Read(P1);
        double area = Mercator.Area(geom);
        Assert.True(area > 0);
    }

    private static void TestReverse(double lon, double lat)
    {
        int x = Mercator.XFromLon(lon);
        int y = Mercator.YFromLat(lat);
        double lon2 = Mercator.LonPrecision7FromX(x);
        double lat2 = Mercator.LatPrecision7FromY(y);
        AssertClose(lon2, lon, 1e-7);
        AssertClose(lat2, lat, 1e-7);
    }

    [Fact]
    public void TestReverseCase()
    {
        TestReverse(0, 0);
        TestReverse(-180, 80);
        TestReverse(180, -80);
        TestReverse(0, Mercator.MaxLat);
        TestReverse(0, Mercator.MinLat);
        TestReverse(0, 85);
        TestReverse(0, -85);
    }

    private static void TestMercatorConversion(int lon100nd, int lat100nd)
    {
        double lon = (double)lon100nd / 10_000_000;
        double lat = (double)lat100nd / 10_000_000;
        int x = Mercator.XFromLon(lon);
        int y = Mercator.YFromLat(lat);
        Assert.Equal((long)lon100nd, JRound(Mercator.LonFromX(x) * 10_000_000));
        Assert.Equal((long)lat100nd, JRound(Mercator.LatFromY(y) * 10_000_000));

        int x2 = Mercator.XFromLon100nd(lon100nd);
        int y2 = Mercator.YFromLat100nd(lat100nd);
        Assert.Equal(x, x2);
        Assert.Equal(y, y2);
    }

    [Fact]
    public void TestMercatorConversionCase()
    {
        TestMercatorConversion(83704807, 500588692);
        TestMercatorConversion(-1_800_000_000, 0);
        TestMercatorConversion(1_800_000_000, 0);
        TestMercatorConversion(0, -850_500_000);
        TestMercatorConversion(0, 850_500_000);
        TestMercatorConversion(91481598, 487725903);
        TestMercatorConversion(113229885, 481728684);
    }
}

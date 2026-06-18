/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;
using System.Globalization;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using GeoDesk.Tests.Benchmark;
using GeoDesk.Util;

using NetTopologySuite.Geometries.Prepared;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest</c>.</remarks>
public class FastFilterTest : IDisposable
{

    readonly FeatureLibrary world;

    public FastFilterTest()
    {
        world = new FeatureLibrary(TestSettings.GolFile());
    }

    public void Dispose() => world.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testTileWalker()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestTileWalker()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=2][name:en=Germany]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();
        map.Add(bavariaPoly).Color("red");

        var tileCount = 0;
        var walker = new TileIndexWalker(world.Store());
        IFilter filter = new IntersectsFilter(bavariaPoly);
        walker.Start(bavaria.Bounds(), filter);
        while (walker.Next())
        {
            tileCount++;
            var marker = map.Add(Tile.Polygon(walker.CurrentTile()))
                .Tooltip(Tile.ToString(walker.CurrentTile()) + "<br>" + Tip.ToString(walker.Tip()));
            if (walker.CurrentFilter() != filter) marker.Color("green");
        }

        Log.Debug("%d tiles in query", tileCount);
        map.Save("c:\\geodesk\\fast-intersects-germany-de-from-world.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testIntersectsQueryPerformance()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestIntersectsQueryPerformance()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();

        long count = 0;
        SimpleBenchmark.Run("fast-intersects", 10, () =>
        {
            count = world.Select("a[building]").Intersecting(bavariaPoly).Count();
        });
        Log.Debug("Found %d features.", count);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testIntersectsQuery()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestIntersectsQuery()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPrepared = PreparedGeometryFactory.Prepare(bavaria!.ToGeometry());

        long count = 0;
        foreach (var f in world.Select("w[highway]").Intersecting(bavariaPrepared))
        {
            if (!bavariaPrepared.Intersects(f.ToGeometry()))
            {
                Assert.Fail(string.Format(CultureInfo.InvariantCulture, "{0} does not intersect", f));
            }
            count++;
        }
        Log.Debug("Found %d features", count);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testWithinQueryPerformance()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestWithinQueryPerformance()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();

        long count = 0;
        SimpleBenchmark.Run("fast-within", 10, () =>
        {
            count = world.Select("a[building]").Within(bavariaPoly).Count();
        });
        Log.Debug("Found %d features.", count);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testTileWalkerCrosses()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestTileWalkerCrosses()
    {
        var map = new MapMaker();
        var rhine = world.Select("r[waterway=river][name:en=Rhine]").First();
        var rhineGeom = rhine!.ToGeometry();
        map.Add(rhineGeom).Color("red");

        var tileCount = 0;
        var walker = new TileIndexWalker(world.Store());
        IFilter filter = new CrossesFilter(rhineGeom);
        walker.Start(rhine.Bounds(), filter);
        while (walker.Next())
        {
            tileCount++;
            var marker = map.Add(Tile.Polygon(walker.CurrentTile()))
                .Tooltip(Tile.ToString(walker.CurrentTile()));
            if (walker.CurrentFilter() != filter) marker.Color("green");
        }

        Log.Debug("%d tiles in query", tileCount);
        map.Save("c:\\geodesk\\fast-crosses-rhine.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testCrossesQueryPerformance()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestCrossesQueryPerformance()
    {
        var rhine = world.Select("r[waterway=river][name:en=Rhine]").First();

        const int runs = 3;
        long count = 0;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < runs; i++)
        {
            count = world.Select("w[highway][bridge]").Crossing(rhine!).Count();
        }
        var end = Stopwatch.GetElapsedTime(start);

        Log.Debug("Found %d bridges across Rhine, each in %d runs, %d ms total", count, runs,
            (long)end.TotalMilliseconds);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testTouchesQuery()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestTouchesQuery()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();

        long count = 0;
        foreach (var f in world.Select("a[boundary=administrative][admin_level=4][name]").Touching(bavaria!))
        {
            Log.Debug("- %s", f.StringValue("name"));
            count++;
        }
        Log.Debug("Found %d features", count);
    }

}

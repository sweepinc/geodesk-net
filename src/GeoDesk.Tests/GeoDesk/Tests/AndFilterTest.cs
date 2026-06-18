/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using GeoDesk.Util;

using NetTopologySuite.Geometries.Prepared;

using Xunit;

namespace GeoDesk.Tests;

// PORT: testAndFilterPerformance is omitted — it depends on com.geodesk.benchmark.BridgesBenchmark,
// which is part of the (un-ported) benchmark harness.
/// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest</c>.</remarks>
public class AndFilterTest : IDisposable
{

    readonly FeatureLibrary world;

    public AndFilterTest()
    {
        world = new FeatureLibrary(TestSettings.GolFile());
    }

    public void Dispose() => world.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest.testAndFilter()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestAndFilter()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var danube = world.Select("r[waterway=river][name:en=Danube]").First();

        var bridges = world.Select("w[highway][bridge]");

        map.Add(bavaria!).Color("red");
        map.Add(danube!).Color("blue");

        foreach (var bridge in bridges.Intersecting(bavaria!).Intersecting(danube!))
        {
            map.Add(bridge).Color("orange");
        }
        map.Save("c:\\geodesk\\bridges-danube-bavaria.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest.testAndFilterTiles()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestAndFilterTiles()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var danube = world.Select("r[waterway=river][name:en=Danube]").First();

        map.Add(bavaria!).Color("red");
        map.Add(danube!).Color("blue");

        var walker = new TileIndexWalker(world.Store());
        var filter = AndFilter.Create(new WithinFilter(bavaria!), new IntersectsFilter(danube!));

        map.Add(filter.Bounds()).Color("orange");
        walker.Start(filter.Bounds(), filter);
        while (walker.Next())
        {
            var marker = map.Add(Tile.Polygon(walker.CurrentTile()))
                .Tooltip(Tile.ToString(walker.CurrentTile()) + "<br>" + Tip.ToString(walker.Tip()));
            if (walker.CurrentFilter() != filter) marker.Color("green");
        }
        map.Save("c:\\geodesk\\tiles-danube-bavaria.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest.testAirports()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestAirports()
    {
        var map = new MapMaker();

        var runways = world.Select("w[aeroway=runway]");
        var airports = world.Select("a[aeroway=aerodrome]");
        const double minLength = 3000;

        var suitableRunways = new HashSet<GeoDesk.Feature.IFeature>();
        var suitableAirports = new HashSet<GeoDesk.Feature.IFeature>();

        foreach (var runway in runways)
        {
            var len = runway.DoubleValue("length");
            if (len != 0) Log.Debug("Got explicit length: %f", len);
            if (len == 0) len = runway.Length();
            if (len >= minLength)
            {
                var airport = airports.Intersecting(runway).First();
                if (airport == null)
                {
                    Log.Debug("Runway %s is not within an airport", runway);
                }
                else
                {
                    suitableAirports.Add(airport);
                    suitableRunways.Add(runway);
                }
            }
        }
        foreach (var f in suitableAirports) map.Add(f).Color("orange");
        foreach (var f in suitableRunways) map.Add(f).Color("red").Tooltip(f.Tags().ToString());
        map.Save("c:\\geodesk\\airports.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest.debugAirports()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void DebugAirports()
    {
        GeoDesk.Feature.IFeature? runway = null;

        var map = new MapMaker();
        var runways = world.Select("w[aeroway=runway]");
        var airports = world.Select("a[aeroway=aerodrome]");

        foreach (var f in runways)
        {
            if (f.Id() == 149993709)
            {
                runway = f;
                break;
            }
        }

        var walker = new TileIndexWalker(world.Store());
        IFilter filter = new IntersectsFilter(runway!.ToGeometry());
        walker.Start(filter.Bounds(), filter);
        map.Add(filter.Bounds()).Color("orange");
        while (walker.Next())
        {
            var marker = map.Add(Tile.Polygon(walker.CurrentTile()))
                .Tooltip(Tile.ToString(walker.CurrentTile()) + "<br>" + Tip.ToString(walker.Tip()));
            if (walker.CurrentFilter() != filter) marker.Color("green");
        }

        var airport = airports.In(filter.Bounds()).First();
        Assert.NotNull(airport);
        Assert.True(runway.ToGeometry().Intersects(airport!.ToGeometry()));
        var runwayPrepared = PreparedGeometryFactory.Prepare(runway.ToGeometry());
        Assert.True(runwayPrepared.Intersects(airport.ToGeometry()));

        airport = airports.Select(filter).First();
        Assert.NotNull(airport);

        map.Save("c:\\geodesk\\hamburg-airport.html");
    }

}

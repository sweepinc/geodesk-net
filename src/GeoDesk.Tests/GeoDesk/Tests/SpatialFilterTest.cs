/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Clarisma.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Geom;
using GeoDesk.Util;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest</c>.</remarks>
[Collection("GolFixture")]
public class SpatialFilterTest
{

    static long Millis(long start) => (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds;

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatial()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatial()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.GolFile());
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();
        var bavariaPrepared = new PreparedPolygon((IPolygonal)bavariaPoly);

        var startQuery = Stopwatch.GetTimestamp();

        var places = world.Select("a[leisure=pitch][sport=soccer]");
        var count = 0;

        var found = new HashSet<GeoDesk.Feature.IFeature>();

        foreach (var place in places.In(Box.Of(bavariaPoly)))
        {
            if (bavariaPrepared.Contains(place.ToGeometry()))
            {
                found.Add(place);
                count++;
            }
        }

        foreach (var place in places.In(Box.Of(bavariaPoly)))
        {
            if (bavariaPrepared.Intersects(place.ToGeometry()))
            {
                if (!found.Contains(place))
                {
                    Console.Write("{0} intersects, but not contained in\n", place);
                }
            }
        }
        Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count, Millis(startQuery), Millis(start));
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialBuildings()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialBuildings()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.GolFile());
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();
        var bavariaPrepared = new PreparedPolygon((IPolygonal)bavariaPoly);

        for (var i = 0; i < 10; i++)
        {
            var startQuery = Stopwatch.GetTimestamp();
            var places = world.Select("a[building]");
            long count = 0;

            foreach (var place in places.In(Box.Of(bavariaPoly)))
            {
                var candidateGeom = place.ToGeometry();
                if (candidateGeom != null && bavariaPrepared.Contains(candidateGeom)) count++;
            }

            Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count, Millis(startQuery), Millis(start));
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialBuildingsFilter()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialBuildingsFilter()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.GolFile());
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        var bavariaPoly = bavaria!.ToGeometry();
        var bavariaPrepared = new PreparedPolygon((IPolygonal)bavariaPoly);

        for (var i = 0; i < 10; i++)
        {
            try
            {
                var startQuery = Stopwatch.GetTimestamp();
                var places = world.Select("a[building]");
                long count = 0;

                foreach (var place in places.Select(new SlowWithinFilter(bavariaPrepared)))
                {
                    count++;
                }

                Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count, Millis(startQuery), Millis(start));
            }
            catch (Exception ex)
            {
                Log.Error("%s", ex);
            }
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialBuildingsBbox()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialBuildingsBbox()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.GolFile());
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();

        for (var i = 0; i < 10; i++)
        {
            var startQuery = Stopwatch.GetTimestamp();
            var places = world.Select("a[building]");
            var count = places.In(bavaria!.Bounds()).Count();

            Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count, Millis(startQuery), Millis(start));
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.State</c>.</remarks>
    class State
    {
        internal GeoDesk.Feature.IFeature feature = null!;
        internal Geometry geom = null!;
        internal IPreparedGeometry prepared = null!;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialStates()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialStates()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.GolFile());
        var germany = world
            .Select("a[boundary=administrative][admin_level=2][name:en=Germany]")
            .In(Box.AtLonLat(12.0231, 48.3310)).First();
        Log.Debug("Fetching country geometry...");
        var germanyPoly = germany!.ToGeometry();
        Log.Debug("Preparing country geometry...");
        var germanyPrepared = new PreparedPolygon((IPolygonal)germanyPoly);
        Log.Debug("Country geometries ready.");

        for (var i = 0; i < 1; i++)
        {
            var startQuery = Stopwatch.GetTimestamp();
            var states = world.Select("a[boundary=administrative][admin_level=4][name]");

            var stateList = new List<State>();

            foreach (var state in states.In(Box.Of(germanyPoly)))
            {
                var stateGeom = state.ToGeometry();
                if (stateGeom != null && germanyPrepared.Contains(stateGeom))
                {
                    var s = new State { feature = state, geom = stateGeom };
                    stateList.Add(s);
                }
            }

            Console.Write("Found {0} German states in {1} ms (Total runtime {2} ms)\n", stateList.Count, Millis(startQuery), Millis(start));

            startQuery = Stopwatch.GetTimestamp();
            var counties = world.Select("a[boundary=administrative][admin_level=6][name]");
            var countyCount = 0;
            var countySet = new HashSet<GeoDesk.Feature.IFeature>();
            var countyGeometries = new List<Geometry>();

            foreach (var county in counties.In(Box.Of(germanyPoly)))
            {
                var countyGeom = county.ToGeometry();
                if (countyGeom != null && germanyPrepared.Contains(countyGeom))
                {
                    countyCount++;
                    countySet.Add(county);
                    countyGeometries.Add(countyGeom);
                }
            }

            Console.Write("Found {0} counties in Germany in {1} ms\n", countyCount, Millis(startQuery));

            Log.Debug("Creating GeometryCollection of counties...");
            Geometry totalCountyGeom = world.GeometryFactory().CreateGeometryCollection(countyGeometries.ToArray());
            Log.Debug("Unioning the GeometryCollection...");
            totalCountyGeom = totalCountyGeom.Buffer(0);
            Log.Debug("Creating a map...");
            var map = new MapMaker();
            map.Add(totalCountyGeom);
            map.Save("c:\\geodesk\\germany-counties-total.html");
            Log.Debug("Map created.");

            foreach (var s in stateList)
            {
                s.prepared = new PreparedPolygon((IPolygonal)s.geom);
            }

            startQuery = Stopwatch.GetTimestamp();
            countyCount = 0;
            foreach (var s in stateList)
            {
                counties = world.Select("a[boundary=administrative][admin_level=6][name]");
                foreach (var county in counties.In(Box.Of(s.geom)))
                {
                    var countyGeom = county.ToGeometry();
                    if (countyGeom != null && s.prepared.Contains(countyGeom))
                    {
                        countyCount++;
                        countySet.Remove(county);
                    }
                }
            }
            Console.Write("Found {0} counties in German states in {1} ms\n", countyCount, Millis(startQuery));

            foreach (var f in countySet)
            {
                Log.Debug("%s was found by country query, but not be state query", f);
            }
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialCityBuildings()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialCityBuildings()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        for (var run = 0; run < 10; run++)
        {
            var start = Stopwatch.GetTimestamp();
            long cityCount = 0;
            long totalBuildingCount = 0;
            GeoDesk.Feature.IFeature? cityWithMostBuildings = null;
            long mostBuildings = 0;
            foreach (var city in world.Select("a[boundary=administrative][admin_level=8][name]"))
            {
                cityCount++;
                var cityBuildingCount = world
                    .Select("a[building]")
                    .Select(new SlowWithinFilter(city.ToGeometry())).Count();
                if (cityBuildingCount > mostBuildings)
                {
                    mostBuildings = cityBuildingCount;
                    cityWithMostBuildings = city;
                    Log.Debug("- %s: %s", city.StringValue("name"), cityBuildingCount);
                }
                totalBuildingCount += cityBuildingCount;
            }
            Log.Debug("Found %d cities with %d buildings in %d ms", cityCount, totalBuildingCount, Millis(start));
            Log.Debug("%s has most buildings (%d)", cityWithMostBuildings!.StringValue("name"), mostBuildings);
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialCityChurches()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialCityChurches()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        for (var run = 0; run < 10; run++)
        {
            var start = Stopwatch.GetTimestamp();
            long cityCount = 0;
            long totalChurchCount = 0;
            GeoDesk.Feature.IFeature? cityWithMostChurches = null;
            long mostChurches = 0;
            foreach (var city in world.Select("a[boundary=administrative][admin_level=8][name]"))
            {
                cityCount++;
                var cityChurchCount = world
                    .Select("na[amenity=place_of_worship][religion=christian]")
                    .Select(new SlowWithinFilter(city.ToGeometry())).Count();
                if (cityChurchCount > mostChurches)
                {
                    mostChurches = cityChurchCount;
                    cityWithMostChurches = city;
                }
                totalChurchCount += cityChurchCount;
            }
            Log.Debug("Found %d cities with %d churches in %d ms", cityCount, totalChurchCount, Millis(start));
            Log.Debug("%s has most churches (%d)", cityWithMostChurches!.StringValue("name"), mostChurches);
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialFilterTest.testSpatialCityChurchesBbox()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSpatialCityChurchesBbox()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        for (var run = 0; run < 10; run++)
        {
            var start = Stopwatch.GetTimestamp();
            long cityCount = 0;
            long totalChurchCount = 0;
            GeoDesk.Feature.IFeature? cityWithMostChurches = null;
            long mostChurches = 0;
            foreach (var city in world.Select("a[boundary=administrative][admin_level=8][name]"))
            {
                cityCount++;
                var cityChurchCount = world
                    .Select("na[amenity=place_of_worship][religion=christian]")
                    .In(city.Bounds()).Count();
                if (cityChurchCount > mostChurches)
                {
                    mostChurches = cityChurchCount;
                    cityWithMostChurches = city;
                }
                totalChurchCount += cityChurchCount;
            }
            Log.Debug("Found %d cities with %d churches in %d ms", cityCount, totalChurchCount, Millis(start));
            Log.Debug("%s has most churches (%d)", cityWithMostChurches!.StringValue("name"), mostChurches);
        }
        world.Close();
    }

}

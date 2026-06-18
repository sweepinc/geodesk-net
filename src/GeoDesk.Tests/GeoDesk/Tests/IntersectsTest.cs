/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest</c>.</remarks>
public class IntersectsTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest.getFeatures(Features)</c>.</remarks>
    static List<long> GetFeatures(IFeatures features)
    {
        var list = new List<long>();
        foreach (var f in features)
        {
            list.Add(FeatureId.Of(f.Type(), f.Id()));
        }
        list.Sort();
        return list;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest.testIntersects()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestIntersects()
    {
        var restaurants = world.Select("na[amenity=restaurant]");
        var country = world
            .Select("a[boundary=administrative][admin_level=2][name:en=Germany]")
            .First()!.ToGeometry();
        var states = world
            .Select("a[boundary=administrative][admin_level=4][name]")
            .Select(new SlowWithinFilter(country)).ToList();

        var inCountry = GetFeatures(restaurants.Select(new SlowWithinFilter(country)));
        var inStates = new List<long>();
        foreach (var state in states)
        {
            Log.Debug("- %s", state.StringValue("name"));
            var inState = GetFeatures(restaurants.Select(new SlowIntersectsFilter(state.ToGeometry())));
            inStates.AddRange(inState);
        }

        inStates.Sort();
        TestUtils.CompareSets("country", inCountry, "all_states", inStates);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest.timeQuery(String, Features)</c>.</remarks>
    static void TimeQuery(string fmt, IFeatures features)
    {
        for (var i = 0; i < 5; i++)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var count = features.Count();
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            System.Console.Write(fmt, count);
            System.Console.Write(": {0:F3} seconds\n", elapsed.TotalSeconds);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest.testBuildingsUSA()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestBuildingsUSA()
    {
        var buildings = world.Select("a[building=yes]");
        var country = world
            .Select("a[boundary=administrative][admin_level=2][name='United States']")
            .First()!.ToGeometry();
        TimeQuery("%d buildings intersect USA", buildings.Intersecting(country));
        TimeQuery("%d buildings within USA", buildings.Within(country));
    }

}

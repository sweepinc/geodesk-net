/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Globalization;

using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest</c>.</remarks>
[Collection("GolFixture")]
public class ContainsTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testShouldContain(double, double, String...)</c>.</remarks>
    void TestShouldContain(double lon, double lat, params string[] names)
    {
        var present = new Dictionary<string, bool>(names.Length);
        foreach (var name in names) present[name] = false;

        foreach (var f in world.Select("a").ContainingLonLat(lon, lat))
        {
            var name = f.StringValue("name");
            if (name.Length != 0) present[name] = true;
        }
        foreach (var entry in present)
        {
            if (!entry.Value)
            {
                Assert.Fail(string.Format(CultureInfo.InvariantCulture,
                    "Expected \"{0}\" to contain lon={1}, lat={2}", entry.Key, lon, lat));
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testContainsKnown()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestContainsKnown()
    {
        TestShouldContain(13.39662, 52.52099,
            "Pergamonmuseum", "Museumsinsel", "Mitte", "Berlin",
            "Berliner Urstromtal", "Deutschland", "Umweltzone Berlin");
        TestShouldContain(6.95825, 50.94131,
            "Kölner Dom", "Altstadt-Nord", "Innenstadt", "Köln",
            "Nordrhein-Westfalen", "Deutschland", "Verkehrsverbund Rhein-Sieg");
        TestShouldContain(8.5601, 50.0376,
            "Flughafen Frankfurt am Main", "Flughafen", "Frankfurt am Main",
            "Hessen", "Deutschland", "Regierungsbezirk Darmstadt", "Rhein-Main-Verkehrsverbund");
        TestShouldContain(9.4739, 47.6144,
            "Obersee", "Bodensee", "Baden-Württemberg", "Deutschland");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testContainsFeature()</c>.</remarks>
    [Fact]
    public void TestContainsFeature()
    {
        var parks = world.Select("a[leisure=park][name]");
        foreach (var park in parks)
        {
            Log.Debug("%s %s is located in:", park, park.StringValue("name"));
            foreach (var f in world.Containing(park).Select("nw"))
            {
                Log.Debug("- %s: %s %s", f, TestUtils.PrimaryTag(f), f.StringValue("name"));
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testContainsPerformance()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestContainsPerformance()
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        for (var i = 0; i < 1000; i++) TestContainsKnown();
        Log.Debug("Executed in %d ms", (long)System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }

}

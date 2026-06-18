/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest</c>.</remarks>
public class LocalKeyTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest.testLocalKeys()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestLocalKeys()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("s7.gol"));
        long count = 0;
        foreach (var n in world.Nodes())
        {
            if (n.BooleanValue("harbour"))
            {
                Log.Debug("%s: %s", n, n.Tags);
                count++;
            }
        }

        Log.Debug("%d found manually, %d per query", count, world.Nodes("[harbour]").Count());
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest.test2()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void Test2()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("s7.gol"));
        foreach (var n in world.Nodes())
        {
            if (n.Id == 1485039266L || n.Id == 1910059730L ||
                n.Id == 1281920924L || n.Id == 824086048)
            {
                var h = n.StringValue("harbour");
                Log.Debug("%s: %s", n, h);
            }
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest.test3()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void Test3()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("s7.gol"));
        foreach (var n in world.Nodes("[harbour]"))
        {
            Log.Debug(n);
        }
        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest.test4()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void Test4()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("s8.gol"));

        var harbours = world.Nodes("[harbour]");

        var nodeCount = world.Nodes().Count();
        var harbourCount = harbours.Count();
        var nonHarbourCount = world.Nodes("[!harbour]").Count();

        Log.Debug("Total nodes:        %d", nodeCount);
        Log.Debug("[harbour] results:  %d", harbourCount);
        Log.Debug("[!harbour] results: %d", nonHarbourCount);
        Log.Debug("Total - [harbour]:  %d", nodeCount - harbourCount);
        Log.Debug("Total - [!harbour]: %d", nodeCount - nonHarbourCount);

        var queryMatched = new HashSet<IFeature>();
        Log.Debug("");
        Log.Debug("Results returned by [harbour] query:");
        foreach (var f in harbours)
        {
            Log.Debug("%20s", f);
            queryMatched.Add(f);
        }

        Log.Debug("");
        Log.Debug("Check tag via Feature.stringValue(\"harbour\"), test harbours.contains():");

        var manuallyMatched = new List<IFeature>();
        foreach (var f in world.Nodes())
        {
            var h = f.StringValue("harbour");
            if (h.Length != 0)
            {
                Log.Debug("%20s: harbour=%s  harbours.contains(): %s", f, h, harbours.Contains(f));
                manuallyMatched.Add(f);
            }
            else
            {
                Assert.False(harbours.Contains(f));
            }
        }

        Log.Debug("");
        Log.Debug("Tags of manually-matched features:");
        foreach (var f in manuallyMatched)
        {
            Log.Debug("%s %s: %s", queryMatched.Contains(f) ? "[harbour] -->" : "", f, f.Tags);
        }

        world.Close();
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.LocalKeyTest.debug8a()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void Debug8a()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("s8.gol"));

        var harbours = world.Nodes("[harbour]");
        var count = harbours.Count();
        Log.Debug("%d results", count);

        world.Close();
    }

}

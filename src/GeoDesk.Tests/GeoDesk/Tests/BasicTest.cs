/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Feature;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest</c>.</remarks>
[Collection("GolFixture")]
public class BasicTest : IDisposable
{

    readonly Features features;
    readonly FeatureLibrary _lib;

    public BasicTest()
    {
        _lib = new FeatureLibrary(TestSettings.Resolve("liguria-libero4.gol"));
        features = _lib;
    }

    public void Dispose() => _lib.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest.testCounts()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestCounts()
    {
        Console.Write("{0} total highways\n", features.Select("w[highway]").Count());
        Console.Write("{0} total features\n", features.Count());
        Console.Write("{0} total nodes\n", features.Select("n").Count());
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest.testWayNodes()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestWayNodes()
    {
        long count = 0;
        foreach (var street in features.Select("w[highway]"))
        {
            foreach (var node in street.Nodes("n"))
            {
                Console.Write("{0}: {1}\n", street.ToString(), node.ToString());
                count++;
            }
        }
        Console.Write("{0} waynodes", count);
    }

}

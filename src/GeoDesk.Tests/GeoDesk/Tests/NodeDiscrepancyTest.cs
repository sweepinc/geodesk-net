/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.NodeDiscrepancyTest</c>.</remarks>
[Collection("GolFixture")]
public class NodeDiscrepancyTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.NodeDiscrepancyTest.investigateDiscrepancy()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void InvestigateDiscrepancy()
    {
        var de4 = new FeatureLibrary(TestSettings.Resolve("de4.gol"));
        var de5 = new FeatureLibrary(TestSettings.Resolve("de5.gol"));

        var nodes4 = TestUtils.GetSet(de4.Select("n"));
        var nodes5 = TestUtils.GetSet(de5.Select("n[!geodesk:duplicate][!geodesk:orphan]"));

        TestUtils.CompareSets("de4", nodes4, "de5", nodes5);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.NodeDiscrepancyTest.investigateRelation1958364()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void InvestigateRelation1958364()
    {
        var de5 = new FeatureLibrary(TestSettings.Resolve("de5.gol"));
        foreach (var rel in de5.Relations())
        {
            if (rel.Id() == 1958364)
            {
                Log.Debug(rel);
                foreach (var f in rel.Members())
                {
                    Log.Debug("- %s: %s", f, f.Role());
                }
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.NodeDiscrepancyTest.investigateNode98677236()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void InvestigateNode98677236()
    {
        var de5 = new FeatureLibrary(TestSettings.Resolve("de5.gol"));
        foreach (var n in de5.Nodes())
        {
            if (n.Id() == 98677236)
            {
                Log.Debug("%s: %s", n, n.Tags());
                Log.Debug("All nodes at this location:");
                foreach (var n2 in de5.Nodes().In(Box.AtXY(n.X(), n.Y())))
                {
                    Log.Debug("- %s: %s", n2, n2.Tags());
                }
            }
        }
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original opened a Liguria extract and only printed counts (no assertions). Rebased
// onto the monaco fixture and given dataset-independent structural assertions, so it exercises the
// query/way-node code paths against real data rather than memorizing one extract's numbers.
/// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest</c>.</remarks>
public class BasicTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest.testCounts()</c>.</remarks>
    [Fact]
    public void TestCounts()
    {
        if (world is null) return; // no GOL fixture → soft-skip

        var highways = world.Select("w[highway]").Count();
        var nodes = world.Select("n").Count();
        var ways = world.Select("w").Count();      // non-area ways
        var areas = world.Select("a").Count();      // areas (area ways + area relations)
        var relations = world.Select("r").Count();  // non-area relations
        var total = world.Count();

        Assert.True(highways > 0, "expected some highways in monaco");
        Assert.True(nodes > 0, "expected some feature nodes in monaco");
        Assert.True(highways <= ways, "non-area highways must be a subset of non-area ways");
        // GeoDesk partitions features into four disjoint categories: nodes, non-area ways,
        // areas, non-area relations. They must sum to the total.
        Assert.True(total == nodes + ways + areas + relations,
            $"total={total} nodes={nodes} ways={ways} areas={areas} relations={relations}");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.BasicTest.testWayNodes()</c>.</remarks>
    [Fact]
    public void TestWayNodes()
    {
        if (world is null) return; // no GOL fixture → soft-skip

        long wayNodeCount = 0;
        foreach (var street in world.Select("w[highway]"))
        {
            foreach (var node in street.Nodes("n"))
            {
                // .Nodes("n") must yield only the way's feature nodes
                Assert.Equal(FeatureType.Node, node.Type);
                wayNodeCount++;
            }
        }

        // Monaco's highways reference feature nodes (crossings, signals, shared junctions).
        Assert.True(wayNodeCount > 0, "expected highways to reference some feature nodes");
    }

}

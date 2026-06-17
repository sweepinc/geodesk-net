/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Clarisma.Common.Util;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.WayNodesTest</c>.</remarks>
[Collection("GolFixture")]
public class WayNodesTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.WayNodesTest.testNodeParentCounts()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestNodeParentCounts()
    {
var streets = world.Select("w[highway]");
        foreach (var street in streets)
        {
            var nNodes = street.Nodes().Count();
            Assert.True(nNodes >= 2);
            long nodeCount = 0;
            foreach (var node in street.Nodes())
            {
                Assert.True(node.BelongsTo(street));
                var nParents = node.Parents().Count();
                if (nParents == 0)
                {
                    Log.Debug("No parents found for %s in %s", node, street);
                }
                Assert.True(nParents > 0);
                var nParentWays = node.Parents().Ways().Count();
                var nParentRelations = node.Parents().Relations().Count();
                Assert.True(nParentWays + nParentRelations == nParents);
                nodeCount++;
            }
            Assert.True(nNodes == nodeCount);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.WayNodesTest.testNodesInRelations()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestNodesInRelations()
    {
foreach (var rel in world.Relations())
        {
            foreach (var node in rel.Members().Nodes())
            {
                Assert.True(node.Parents().Relations().Count() > 0);
                Assert.True(node.Parents().Relations().Contains(rel));
                Assert.True(node.BelongsToRelation());
                Assert.True(node.BelongsTo(rel));
            }
        }
    }

}

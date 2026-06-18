/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.NodesTest</c>.</remarks>
public class NodesTest : AbstractFeatureTest
{

    /// <summary>
    /// Checks each node tagged "geodesk:duplicate" to ensure: the node has no tags other than
    /// "geodesk:*"; there is at least one other node with the same x/y; the other nodes have at
    /// least one tag.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.NodesTest.testDuplicateNodes()</c>.</remarks>
    [Fact]
    public void TestDuplicateNodes()
    {
        long duplicateNodeCount = 0;
        foreach (var node in world.Nodes("n[geodesk:duplicate]"))
        {
            var tagCount = 0;
            var tags = node.Tags();
            while (tags.Next())
            {
                if (!tags.Key()!.StartsWith("geodesk:")) tagCount++;
            }
            Assert.Equal(0, tagCount);

            var nodeCount = 0;
            foreach (var otherNode in world.Nodes().In(Box.AtXY(node.X(), node.Y())))
            {
                if (otherNode.Equals(node)) continue;
                Assert.False(otherNode.Tags().IsEmpty());
                nodeCount++;
            }
            Assert.True(nodeCount > 0);
            duplicateNodeCount++;
        }
        Log.Debug("Checked %d duplicate nodes", duplicateNodeCount);
    }

    /// <summary>
    /// Checks each node tagged "geodesk:orphan" to ensure: the node has no tags other than
    /// "geodesk:*"; the node does not belong to any way; the node does not belong to any relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.NodesTest.testOrphanNodes()</c>.</remarks>
    [Fact]
    public void TestOrphanNodes()
    {
        long orphanNodeCount = 0;
        foreach (var node in world.Nodes("n[geodesk:orphan]"))
        {
            var tagCount = 0;
            var tags = node.Tags();
            while (tags.Next())
            {
                if (!tags.Key()!.StartsWith("geodesk:")) tagCount++;
            }
            Assert.Equal(0, tagCount);

            Assert.True(node.Parents().Ways().IsEmpty());
            Assert.True(node.Parents().Relations().IsEmpty());
            orphanNodeCount++;
        }
        Log.Debug("Checked %d orphan nodes", orphanNodeCount);
    }

}

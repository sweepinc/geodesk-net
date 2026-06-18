/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Feature.Query;

/// <summary>
/// Exercises the table-view cluster (relation members, way feature-nodes, parent
/// relations) against the monaco.gol fixture. Soft-skips if no fixture is present.
/// </summary>
[Collection("GolFixture")]
public class TableViewTest
{
    private readonly ITestOutputHelper output;

    public TableViewTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static string GolFile() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void IteratesMembersWayNodesAndParents()
    {
        string gol = GolFile();
        if (!File.Exists(gol))
        {
            output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        IFeatures world = FeatureLibrary.Open(gol);
        try
        {
            // 1) Relation members: at least one relation in Monaco has members.
            long relations = 0;
            long memberTotal = 0;
            IFeature? relWithMembers = null;
            foreach (IFeature r in world.Relations())
            {
                relations++;
                long m = r.Members().Count();
                memberTotal += m;
                if (m > 0 && relWithMembers == null) relWithMembers = r;
            }
            output.WriteLine($"relations={relations}, total members={memberTotal}");
            Assert.True(relations > 0, "expected relations in Monaco");
            Assert.True(memberTotal > 0, "expected at least one relation member");

            // Each member of a relation reports that relation as a parent.
            IFeature firstMember = relWithMembers!.Members().First()!;
            Assert.Contains(relWithMembers, world.ParentsOf(firstMember).ToList());

            // 2) Way feature-nodes: find a way that has feature nodes and round-trip the
            //    parent relationship (the node's parent ways include the way).
            IFeature? wayWithNodes = null;
            IFeature? aNode = null;
            foreach (IFeature w in world.Ways())
            {
                IFeature? n = w.Nodes().First();
                if (n != null)
                {
                    wayWithNodes = w;
                    aNode = n;
                    break;
                }
            }

            if (wayWithNodes != null)
            {
                output.WriteLine($"way {wayWithNodes.Id()} has feature node {aNode!.Id()}");
                Assert.True(aNode.IsNode());
                // The node's parent ways must include this way.
                Assert.Contains(wayWithNodes, world.Ways().ParentsOf(aNode).ToList());
            }
            else
            {
                output.WriteLine("no way with feature nodes found (only anonymous nodes)");
            }
        }
        finally
        {
            ((IDisposable)world).Dispose();
        }
    }
}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the two disabled (non-@Test) methods testSuperRelations / testMemberQueriesX are omitted.
/// <summary>A series of basic integrity tests.</summary>
/// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest</c>.</remarks>
public class ReferentialIntegrityTest : IDisposable
{

    readonly FeatureLibrary features;
    readonly BoxMaker boxes;

    public ReferentialIntegrityTest()
    {
        features = new FeatureLibrary(TestSettings.GolFile());
        boxes = new BoxMaker(Box.OfWSEN(7.6872276841, 47.707433547, 12.6844378887, 53.8412264446));
    }

    public void Dispose() => features.Close();

    /// <summary>Set-based queries must not return the same feature more than once.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testNoDuplicates()</c>.</remarks>
    [Fact]
    public void TestNoDuplicates()
    {
        var results = new HashSet<IFeature>();
        var runs = 1_000;
        long count = 0;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < runs; i++)
        {
            results.Clear();
            foreach (var f in features.In(boxes.Random(10_000, 100_000)))
            {
                Assert.False(results.Contains(f));
                results.Add(f);
                count++;
            }
        }
        Log.Debug("Ran %d queries with %d results in %d ms: No duplicates returned.",
            runs, count, (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }

    static readonly string[] CommonRoles =
        { "admin_centre", "inner", "main_stream", "outer", "stop", "via" };

    /// <summary>
    /// Checks that features("ar") with explicit type check for Relation and relations() return the
    /// same set.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testTypeConstraint()</c>.</remarks>
    [Fact]
    public void TestTypeConstraint()
    {
        var set1 = new HashSet<IFeature>();
        var set2 = new HashSet<IFeature>();

        foreach (var f in features.Select("ra"))
        {
            if (f is IRelation) set1.Add(f);
        }

        foreach (var rel in features.Relations())
        {
            set2.Add(rel);
        }

        Log.Debug("Total feature in set1: %d", set1.Count);
        Log.Debug("Total feature in set2: %d", set2.Count);

        CountAreas("set1", set1);
        CountAreas("set2", set2);
    }

    void CountAreas(string title, IEnumerable<IFeature> set)
    {
        long count = 0;
        foreach (var f in set) if (f.IsArea()) count++;
        Log.Debug("%s contains %d areas", title, count);
    }

    /// <summary>Checks the referential integrity between relations and their members.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testRelations()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestRelations()
    {
        var uniqueRoles = new HashSet<string?>();
        long relCount = 0;
        long memberCount = 0;
        var start = Stopwatch.GetTimestamp();
        foreach (var rel in features.Relations())
        {
            relCount++;
            foreach (var member in rel.Members())
            {
                memberCount++;

                // Check referential integrity relation <---> member
                Assert.True(member.BelongsToRelation());
                if (!member.BelongsTo(rel))
                {
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture,
                        "Feature.belongsTo() false, but {0} belongs to {1}", member, rel));
                }
                Assert.True(member.Parents().Relations().Contains(rel));
                uniqueRoles.Add(member.Role());
            }
        }
        Log.Debug("Checked %d relations with %d members in %d ms.", relCount, memberCount,
            (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        Log.Debug("- %d unique roles", uniqueRoles.Count);
        foreach (var role in CommonRoles) Assert.True(uniqueRoles.Contains(role));
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testMembersOf()</c>.</remarks>
    [Fact]
    public void TestMembersOf()
    {
        var routes = features.Select("r[route=bicycle]");
        var primaryRoads = features.Select("w[highway=primary]");

        foreach (var route in routes)
        {
            var members1 = route.Members("w[highway=primary]");
            var members2 = primaryRoads.MembersOf(route);
            foreach (var member in members1)
            {
                Assert.True(members2.Contains(member));
                Assert.True(member.Parents().Contains(route));
                Assert.True(routes.ParentsOf(member).Contains(route));
            }
            foreach (var member in members2)
            {
                Assert.True(members1.Contains(member));
            }
        }
    }

    /// <summary>
    /// Ensures that typed queries return the same results as querying "by hand". For simplicity we
    /// count the number of features found, rather than doing a proper set check.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testTypedQueries()</c>.</remarks>
    [Fact]
    public void TestTypedQueries()
    {
        long nodes = 0;
        long allWays = 0;
        long allRelations = 0;
        long areas = 0;
        long allHighways = 0;
        long linealHighways = 0;
        long linealRailways = 0;
        long linealRailwayHighways = 0;

        foreach (var f in features)
        {
            if (f is INode) nodes++;
            if (f is IWay) allWays++;
            if (f is IRelation) allRelations++;
            if (f.IsArea()) areas++;
            var isHighway = f.HasTag("highway") && f.StringValue("highway") != "no";
            var isRailway = f.HasTag("railway") && f.StringValue("railway") != "no";
            var isLineal = f is IWay && !f.IsArea();

            if (isHighway) allHighways++;
            if (isHighway && isLineal) linealHighways++;
            if (isRailway && isLineal) linealRailways++;
            if (isHighway && isRailway && isLineal) linealRailwayHighways++;
        }

        Assert.True(nodes > 0);
        Assert.True(allWays > 0);
        Assert.True(allRelations > 0);
        Assert.True(areas > 0);

        Assert.Equal(nodes, features.Nodes().Count());
        Assert.Equal(allWays, features.Ways().Count());
        Assert.Equal(allRelations, features.Relations().Count());
        Assert.Equal(areas, features.Select("a").Count());
        Assert.Equal(allHighways, features.Select("*[highway]").Count());
        Assert.Equal(linealHighways, features.Select("w[highway]").Count());
        Assert.Equal(linealHighways, features.Ways("w[highway]").Count());
        Assert.Equal(linealRailways, features.Ways("w[railway]").Count());
        Assert.Equal(linealRailwayHighways, features.Select("w[railway][highway]").Count());
        Assert.Equal(linealRailwayHighways, features.Select("w[railway]").Ways("*[highway]").Count());
        Assert.Equal(linealRailwayHighways, features.Select("w[highway]").Ways("*[railway]").Count());

        var linealWaysQuery = (WorldView)features.Ways()
            .Select("wa[highway]")
            .Ways("*[railway][highway]")
            .Select("w");
        Assert.Equal(TypeBits.NONAREA_WAYS, linealWaysQuery.TypesValue());

        Assert.Equal(linealRailwayHighways, features.Ways()
            .Select("wa[highway]")
            .Ways("*[railway][highway]")
            .Select("*[railway]")
            .Select("w")
            .Count());

        var empty = features.Ways()
            .Select("wa[highway]")
            .Ways("*[railway][highway]")
            .Select("*[railway]")
            .Nodes();
        Assert.True(empty is EmptyView);
    }

    /// <summary>
    /// Checks the referential integrity between relations and their members; this time from the
    /// perspective of the members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testRelationMembers()</c>.</remarks>
    [Fact]
    public void TestRelationMembers()
    {
        foreach (var f in features.In(Box.OfWorld()))
        {
            foreach (var rel in f.Parents().Relations())
            {
                Assert.True(rel.Members().Contains(f));
            }
        }
    }

    void TestContainsQueries(IFeatures feats, HashSet<IFeature> others)
    {
        CheckContains(feats.Select("a[landuse]"), others);
        CheckContains(feats.Nodes("na[shop]").Select("*[opening_hours]"), others);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testContains()</c>.</remarks>
    [Fact]
    public void TestContains()
    {
        var others = RandomSample(features, 10_000);

        TestContainsQueries(features, others);
        for (var i = 0; i < 1000; i++)
        {
            TestContainsQueries(features.In(boxes.Random(3000, 10_000)), others);
        }
    }

    /// <summary>
    /// Checks whether contains() returns true/false for features that are in / not in a collection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testContains(Features, Set)</c>.</remarks>
    void CheckContains(IFeatures feats, HashSet<IFeature> others)
    {
        var notContained = new HashSet<IFeature>(others);
        foreach (var f in feats)
        {
            Assert.True(feats.Contains(f));
            notContained.Remove(f);
        }
        foreach (var f in notContained)
        {
            Assert.False(feats.Contains(f));
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.randomSample(Features, int)</c>.</remarks>
    HashSet<IFeature> RandomSample(IFeatures feats, int sampleInterval)
    {
        var sample = new HashSet<IFeature>();
        var random = new Random();
        var skip = random.Next(sampleInterval);
        foreach (var f in feats)
        {
            skip--;
            if (skip > 0) continue;
            sample.Add(f);
            skip = random.Next(sampleInterval);
        }
        return sample;
    }

    /// <summary>
    /// Checks way invariants: bbox is the tightest box including all nodes; areas are closed with
    /// >= 4 nodes (others >= 2); coordinates match the nodes; area geometry is polygonal, non-area
    /// is lineal; filtered node queries match manual checks.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testWays()</c>.</remarks>
    [Fact]
    public void TestWays()
    {
        long wayCount = 0;
        long totalNodeCount = 0;
        long totalFeatureNodeCount = 0;
        long totalHighwayNodeCount = 0;
        long totalEntranceNodeCount = 0;
        foreach (var way in features.Ways())
        {
            Box wayBox = way.Bounds();
            var calculatedBox = new Box();

            IFeature? firstNode = null;
            IFeature? lastNode = null;
            var nodeCount = 0;
            var highwayNodeCount = 0;
            var entranceNodeCount = 0;

            var xy = way.ToXY();

            foreach (var node in way.Nodes())
            {
                if (firstNode == null) firstNode = node;
                lastNode = node;
                calculatedBox.ExpandToInclude(node.X(), node.Y());
                Assert.Equal(xy[nodeCount * 2], node.X());
                Assert.Equal(xy[nodeCount * 2 + 1], node.Y());
                nodeCount++;
                var v = node.StringValue("highway");
                if (v.Length != 0 && v != "no") highwayNodeCount++;
                v = node.StringValue("entrance");
                if (v.Length != 0 && v != "no") entranceNodeCount++;
            }

            Assert.Equal(nodeCount, way.Nodes().Count());
            Assert.Equal(nodeCount, xy.Length / 2);
            Assert.Equal(calculatedBox, wayBox);

            var geom = way.ToGeometry();
            if (way.IsArea())
            {
                Assert.Equal(firstNode, lastNode);
                Assert.True(nodeCount >= 4);
                Assert.True(geom is IPolygonal);
            }
            else
            {
                Assert.True(nodeCount >= 2);
                Assert.True(geom is ILineal);
            }

            foreach (var node in way.Nodes("*"))
            {
                Assert.True(node.Id() > 0);
                totalFeatureNodeCount++;
            }

            Assert.Equal(highwayNodeCount, way.Nodes("*[highway]").Count());
            Assert.Equal(entranceNodeCount, way.Nodes("n[entrance]").Count());

            wayCount++;
            totalNodeCount += nodeCount;
            totalHighwayNodeCount += highwayNodeCount;
            totalEntranceNodeCount += entranceNodeCount;
        }
        Log.Debug("Tested %d ways:", wayCount);
        Log.Debug("- %d total nodes", totalNodeCount);
        Log.Debug("- %d feature nodes", totalFeatureNodeCount);
        Log.Debug("- %d nodes tagged 'highway'", totalHighwayNodeCount);
        Log.Debug("- %d nodes tagged 'entrance'", totalEntranceNodeCount);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testParentWays()</c>.</remarks>
    [Fact]
    public void TestParentWays()
    {
        long nodeCount = 0;
        long parentWayCount = 0;
        long waysAtNodes = 0;
        var sample = RandomSample(features.Ways(), 1_000);
        var start = Stopwatch.GetTimestamp();
        foreach (var way in sample)
        {
            foreach (var node in way.Nodes())
            {
                var parentWays = node.Parents().Ways();
                if (!parentWays.Contains(way))
                {
                    Log.Debug("%s should have %s as a parent, but doesn't. It has %d parents.",
                        node.ToString(), way.ToString(), parentWays.Count());
                    Log.Debug("%s flags = %d, tags = %s", node.ToString(),
                        ((StoredNode)node).Flags(), node.Tags().ToString());
                    var parentWays2 = node.Parents().Ways();
                    foreach (var p in parentWays2)
                    {
                        Log.Debug(p.ToString());
                    }
                }
                Assert.True(parentWays.Contains(way));
                foreach (var parentWay in parentWays)
                {
                    Assert.True(parentWay.Nodes().Contains(node));
                    parentWayCount++;
                }
                waysAtNodes += features.Ways().In(node.Bounds()).Count();
                nodeCount++;
            }
        }
        Log.Debug("Checked %d ways with %d feature nodes (%d parent ways) in %d ms",
            sample.Count, nodeCount, parentWayCount, (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        Log.Debug("ParentWay queries consulted %d ways at node location.", waysAtNodes);
    }

    void AssertNotTagged(IFeature f, string k)
    {
        var v = f.StringValue(k);
        Assert.True(v.Length == 0 || v == "no");
        var tags = f.Tags();
        while (tags.Next())
        {
            if (tags.Key() == k)
            {
                v = tags.StringValue()!;
                Assert.True(v.Length == 0 || v == "no");
            }
        }
    }

    /// <summary>
    /// Gets the tags of all features and queries them in various ways (directly, through the
    /// iterator, via a map); all methods of lookup must return the same results.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testTags()</c>.</remarks>
    [Fact]
    public void TestTags()
    {
        var totalFeatureCount = 0;
        var totalTagCount = 0;
        foreach (var f in features)
        {
            var tags = f.Tags();
            var tagCount = 0;

            var tagMap = tags.ToMap();

            while (tags.Next())
            {
                var k = tags.Key()!;
                var v = tags.StringValue()!;

                if (!f.HasTag(k, v))
                {
                    Log.Debug("%s: hasTag() did not find %s=%s", f.ToString(), k, v);
                    var v2 = f.StringValue(k);
                    Log.Debug("  Value of %s: is %s", k, v2);
                }

                if (f.StringValue(k) != v)
                {
                    Log.Debug("%s: %s=%s is not equal to %s", f.ToString(), k, f.StringValue(k), v);
                }

                if (!f.HasTag(k))
                {
                    Log.Debug("%s should have tag %s", f.ToString(), k);
                }

                Assert.True(f.HasTag(k, v));
                Assert.Equal(f.StringValue(k), v);
                Assert.True(f.HasTag(k) || k.Length == 0);
                Assert.True(tagMap.ContainsKey(k));
                tagCount++;
            }
            Assert.Equal(tagCount, tags.Size());
            Assert.Equal(tagCount, tagMap.Count);

            totalFeatureCount++;
            totalTagCount += tagCount;
        }
        Log.Debug("Tested %d features with %d tags", totalFeatureCount, totalTagCount);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testSpecificWayTags()</c>.</remarks>
    [Fact]
    public void TestSpecificWayTags()
    {
        foreach (var way in features.Ways("w[highway]"))
        {
            foreach (var node in way.Nodes(
                "n[!highway][!railway][!barrier][!entrance][!created_by][!traffic_sign][!crossing]"))
            {
                var v = node.StringValue("traffic_sign");
                AssertNotTagged(node, "highway");
                AssertNotTagged(node, "highway");
                AssertNotTagged(node, "railway");
                AssertNotTagged(node, "barrier");
                AssertNotTagged(node, "entrance");
                AssertNotTagged(node, "crossing");
                AssertNotTagged(node, "created_by");
                AssertNotTagged(node, "traffic_sign");
            }
        }
    }

    /// <summary>
    /// Tests relation member queries based on primitive type, conceptual type, and iteration; all
    /// approaches must yield the same counts.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testSimpleMemberQueries()</c>.</remarks>
    [Fact]
    public void TestSimpleMemberQueries()
    {
        long relations = 0;
        long members = 0;
        long memberNodes = 0;
        long memberWays = 0;
        long memberRelations = 0;
        long memberAreas = 0;
        long memberWayAreas = 0;
        long memberRelationAreas = 0;
        long membersManual = 0;
        long memberNodesManual = 0;
        long memberWaysManual = 0;
        long memberRelationsManual = 0;
        long memberAreasManual = 0;
        long memberWayAreasManual = 0;
        long memberRelationAreasManual = 0;

        foreach (var rel in features.Relations())
        {
            var thisMemberRelations = rel.Members().Relations().Count();
            var thisMemberWays = rel.Members().Ways().Count();
            members += rel.Members().Count();
            memberNodes += rel.Members().Nodes().Count();
            memberWays += thisMemberWays;
            memberRelations += thisMemberRelations;
            memberAreas += rel.Members("a").Count();
            memberWayAreas += rel.Members().Ways("a").Count();
            memberRelationAreas += rel.Members().Relations("a").Count();
            Assert.Equal(thisMemberRelations, rel.Members().Relations("ar").Count());
            Assert.Equal(thisMemberWays, rel.Members().Ways("wa").Count());
            Assert.Equal(thisMemberRelations, rel.Members().Relations("nar").Count());
            Assert.Equal(thisMemberWays, rel.Members().Ways("nwa").Count());

            foreach (var f in rel)
            {
                switch (f.Type())
                {
                    case FeatureType.Node:
                        memberNodesManual++;
                        break;
                    case FeatureType.Way:
                        memberWaysManual++;
                        if (f.IsArea())
                        {
                            memberWayAreasManual++;
                            memberAreasManual++;
                        }
                        break;
                    case FeatureType.Relation:
                        memberRelationsManual++;
                        if (f.IsArea())
                        {
                            memberRelationAreasManual++;
                            memberAreasManual++;
                        }
                        break;
                }
                membersManual++;
            }
            relations++;
        }
        Log.Debug("%d relations with %d members", relations, members);
        Log.Debug("  (%d nodes, %d ways, %d relations)", memberNodes, memberWays, memberRelations);
        Assert.True(relations > 0);
        Assert.True(members > 0);
        Assert.Equal(membersManual, members);
        Assert.Equal(memberNodesManual, memberNodes);
        Assert.Equal(memberWaysManual, memberWays);
        Assert.Equal(memberRelationsManual, memberRelations);
        Assert.Equal(memberAreasManual, memberAreas);
        Assert.Equal(memberWayAreasManual, memberWayAreas);
        Assert.Equal(memberRelationAreasManual, memberRelationAreas);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testMemberRoleQueries()</c>.</remarks>
    [Fact]
    public void TestMemberRoleQueries()
    {
        Matcher matcher = new RoleMatcher(features.Store(), "admin_centre");
        for (var run = 0; run < 10; run++)
        {
            var start = Stopwatch.GetTimestamp();
            long relCount = 0;
            long memberCount = 0;
            long memberCountSlow = 0;
            foreach (var rel in features.Relations())
            {
                relCount++;
                var iter = ((StoredRelation)rel).GetEnumerator(TypeBits.NODES, matcher);
                while (iter.MoveNext())
                {
                    memberCount++;
                }

                foreach (var node in rel.Members().Nodes())
                {
                    if (node.Role() == "admin_centre") memberCountSlow++;
                }
            }
            Log.Debug("%d nodes in %d relations (%d ms)", memberCount, relCount,
                (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            Log.Debug("  (%d nodes using slow count)", memberCountSlow);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ReferentialIntegrityTest.testValueStrings()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestValueStrings()
    {
        var strings = new List<string>();

        foreach (var f in features)
        {
            var tags = f.Tags();
            while (tags.Next())
            {
                strings.Add(f + ": " + tags.StringValue());
            }
        }

        strings.Sort(StringComparer.Ordinal);

        using var writer = new StreamWriter("d:\\geodesk\\tests\\monaco-java.txt", false, Encoding.UTF8);
        foreach (var s in strings)
        {
            writer.WriteLine(s);
        }
    }

}

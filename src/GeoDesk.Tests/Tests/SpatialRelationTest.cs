/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <summary>
/// Relations are tricky for some of the JTS relate operations, because they involve
/// GeometryCollections. This test checks for exceptions thrown by the JTS library.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.tests.SpatialRelationTest</c>.</remarks>
public class SpatialRelationTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialRelationTest.spatialPredicatesInvolvingRelations()</c>.</remarks>
    [Fact]
    public void SpatialPredicatesInvolvingRelations()
    {
        var mostPlaceholders = 0;
        var mostNodes = 0;
        var mostWays = 0;
        IFeature? trickiestRelation = null;

        foreach (var rel in world.Relations("r[route=train]"))
        {
            var nodes = 0;
            var ways = 0;
            foreach (var member in rel)
            {
                if (member is INode) nodes++;
                if (member is IWay) ways++;
            }
            if (nodes > mostNodes && ways > mostWays)
            {
                mostNodes = nodes;
                mostWays = ways;
                trickiestRelation = rel;
            }
        }

        Log.Debug("Testing against %s (%d nodes, %d ways, %d placeholders)", trickiestRelation,
            mostNodes, mostWays, mostPlaceholders);

        TestSpatial("coveredBy", trickiestRelation!);
        TestSpatial("overlaps", trickiestRelation!);
        TestSpatial("within", trickiestRelation!);
        TestSpatial("intersects", trickiestRelation!);
        TestSpatial("crosses", trickiestRelation!);
        TestSpatial("touches", trickiestRelation!);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.SpatialRelationTest.testSpatial(String, Feature)</c>.</remarks>
    void TestSpatial(string name, IFeature test)
    {
        IFilter? filter = name switch
        {
            "coveredBy" => new CoveredByFilter(test),
            "crosses" => new CrossesFilter(test),
            "intersects" => new IntersectsFilter(test),
            "overlaps" => new OverlapsFilter(test),
            "touches" => new TouchesFilter(test),
            "within" => new WithinFilter(test),
            _ => null,
        };

        Log.Debug("%s %s:", name, test);
        foreach (var f in world.Select("r").Select(filter!))
        {
            Log.Debug("- %s", f);
        }
    }

}

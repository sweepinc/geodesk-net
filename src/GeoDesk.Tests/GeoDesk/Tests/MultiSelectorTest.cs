/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.MultiSelectorTest</c>.</remarks>
public class MultiSelectorTest : AbstractFeatureTest
{

    /// <summary>
    /// Verifies that multi-selector queries return same results as combinations of equivalent
    /// single-selector queries (uses only counts for now). Focus: same type, different keys.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.MultiSelectorTest.testMultiSelectorIndexing()</c>.</remarks>
    [Fact]
    public void TestMultiSelectorIndexing()
    {
long onlyHighwayCount = 0;
        long onlyRailwayCount = 0;
        long bothHighwayAndRailwayCount = 0;
        long notHighwayOrRailwayCount = 0;

        foreach (var f in world.Select("w"))
        {
            if (f.BooleanValue("highway"))
            {
                if (f.BooleanValue("railway"))
                    bothHighwayAndRailwayCount++;
                else
                    onlyHighwayCount++;
            }
            else if (f.BooleanValue("railway"))
            {
                onlyRailwayCount++;
            }
            else
            {
                notHighwayOrRailwayCount++;
            }
        }

        Assert.Equal(onlyHighwayCount + bothHighwayAndRailwayCount, world.Select("w[highway]").Count());
        Assert.Equal(onlyHighwayCount, world.Select("w[highway][!railway]").Count());
        Assert.Equal(onlyRailwayCount + bothHighwayAndRailwayCount, world.Select("w[railway]").Count());
        Assert.Equal(onlyRailwayCount, world.Select("w[!highway][railway]").Count());
        Assert.Equal(bothHighwayAndRailwayCount, world.Select("w[highway][railway]").Count());
        Assert.Equal(notHighwayOrRailwayCount, world.Select("w[!highway][!railway]").Count());
        Assert.Equal(onlyHighwayCount + onlyRailwayCount + bothHighwayAndRailwayCount,
            world.Select("w[highway], w[railway]").Count());
    }

    /// <summary>
    /// Verifies that multi-selector queries return same results as combinations of equivalent
    /// single-selector queries (uses only counts for now). Focus: polyform queries (different types).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.MultiSelectorTest.testPolyformQueries()</c>.</remarks>
    [Fact(Skip = "Polyform queries are not supported (faithful to geodesk 2.0.0: MatcherCompiler throws QueryException).")]
    public void TestPolyformQueries()
    {
var nodesCount = world.Select("n").Count();
        var waysCount = world.Select("w").Count();
        var areaCount = world.Select("a").Count();
        var relationCount = world.Select("r").Count();
        var stationCount = world.Select("na[amenity=fire_station]").Count();
        var hydrantCount = world.Select("n[emergency=fire_hydrant]").Count();
        Assert.True(stationCount > 0);
        Assert.True(hydrantCount > 0);

        Log.Debug("Nodes:     %d", nodesCount);
        Log.Debug("Ways:      %d", waysCount);
        Log.Debug("Areas:     %d", areaCount);
        Log.Debug("Relations: %d", relationCount);

        Assert.Equal(nodesCount + waysCount, world.Select("n, nw").Count());
        Assert.Equal(nodesCount + waysCount, world.Select("nw, n").Count());
        Assert.Equal(nodesCount + waysCount + areaCount, world.Select("na, wa, n").Count());
        Assert.Equal(nodesCount + waysCount + areaCount, world.Select("n, naw, an").Count());
        Assert.Equal(nodesCount + relationCount + areaCount, world.Select("ran, ar, n, na, nr").Count());
        Assert.Equal(nodesCount + waysCount, world.Select("n,w").Count());
        Assert.Equal(nodesCount + areaCount, world.Select("n,a").Count());
        Assert.Equal(nodesCount + waysCount + areaCount, world.Select("n,w,a").Count());
        Assert.Equal(nodesCount + waysCount + areaCount, world.Select("n,a,w").Count());
        Assert.Equal(nodesCount + areaCount + relationCount, world.Select("n,a,r").Count());
        Assert.Equal(nodesCount + waysCount + areaCount + relationCount, world.Select("n,w,a,r").Count());
        Assert.Equal(nodesCount + waysCount + areaCount + relationCount, world.Select("n,w,warn,a,r").Count());
        Assert.Equal(nodesCount + waysCount + areaCount + relationCount,
            world.Select("ran, ar, n, war, na, w, nr").Count());

        // some fire stations may be tagged as fire_hydrant as well!
        Assert.Equal(stationCount + hydrantCount,
            world.Select("na[amenity=fire_station], n[emergency=fire_hydrant]").Count());
        Assert.Equal(stationCount + hydrantCount,
            world.Select("n[emergency=fire_hydrant], na[amenity=fire_station]").Count());
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.NorthwestTest</c>.</remarks>
public class NorthwestTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.NorthwestTest.testSameIntersectsResults()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestSameIntersectsResults()
    {
        var world = FeatureLibrary.Open(TestSettings.GolFile());
        try
        {
            var country = world
                .Select("a[boundary=administrative][admin_level=2][name:en=Germany]")
                .In(Box.AtLonLat(12.0231, 48.3310))
                .First();
            var buildings = world.Select("wa[highway]");

            var slow = TestUtils.GetSet(buildings.Select(new SlowIntersectsFilter(country!.ToGeometry())));
            var fast = TestUtils.GetSet(buildings.Intersecting(country));

            TestUtils.CheckNoDupes("intersects-fast", fast);
            TestUtils.CompareSets("intersects-slow", slow, "intersects-fast", fast);
        }
        finally
        {
            ((System.IDisposable)world).Dispose();
        }
    }

    // Answer: broken geometry, member ways don't connect
    /// <remarks>Ported from Java <c>com.geodesk.tests.NorthwestTest.whySlowIntersectionFailsToReturnRelation9675374()</c>.</remarks>
    [Fact]
    public void WhySlowIntersectionFailsToReturnRelation9675374()
    {
        var world = FeatureLibrary.Open(TestSettings.GolFile());
        try
        {
            foreach (var rel in world.Relations("ra[name='Euskirchener Straße'][type=public_transport]"))
            {
                if (rel.Id() == 9675374)
                {
                    Log.Debug("Area = %s", rel.IsArea());
                    foreach (var member in rel)
                    {
                        Log.Debug("- %s: %s", member, member.ToGeometry());
                    }
                    Log.Debug("Geometry = %s", rel.ToGeometry());
                }
            }
        }
        finally
        {
            ((System.IDisposable)world).Dispose();
        }
    }

}

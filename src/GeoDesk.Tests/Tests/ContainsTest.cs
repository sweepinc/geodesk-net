/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original asserted that hard-coded German coordinates fall inside named German
// areas. Rebased onto monaco as dataset-independent invariants of the Contains filter.
/// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest</c>.</remarks>
public class ContainsTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testContainsKnown()</c>.</remarks>
    [Fact]
    public void TestAreaContainsItsInteriorPoint()
    {
        if (world is null) return;

        var tested = 0;
        foreach (var area in world.Select("a[name]"))
        {
            var pt = area.ToGeometry().InteriorPoint;
            var self = FeatureId.Of(area.Type, area.Id);

            var found = false;
            foreach (var f in world.Select("a").ContainingXY((int)pt.X, (int)pt.Y))
            {
                if (FeatureId.Of(f.Type, f.Id) == self) { found = true; break; }
            }
            Assert.True(found, $"area {self} must contain its own interior point");

            if (++tested >= 25) break; // sample, not exhaustive
        }
        Assert.True(tested > 0, "expected named areas in monaco");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainsTest.testContainsFeature()</c>.</remarks>
    [Fact]
    public void TestContainingImpliesIntersecting()
    {
        if (world is null) return;

        var area = world.Select("a[name]").First();
        Assert.NotNull(area);

        var containing = Ids(world.Select("a").Containing(area!));
        var intersecting = Ids(world.Select("a").Intersecting(area!));

        // anything that contains the area must also intersect it
        Assert.Subset(intersecting, containing);
    }

}

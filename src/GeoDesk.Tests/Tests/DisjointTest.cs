/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

using GeoDesk.Common.Util;
using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original visualized "rivers outside Bavaria" by writing HTML maps (no assertions).
// Rebased onto monaco as a structural test of the Disjoint filter: for any area, the disjoint and
// intersecting subsets of a feature set must partition it (no overlap, and together cover all).
/// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest</c>.</remarks>
public class DisjointTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest.testDisjoint()</c>.</remarks>
    [Fact]
    public void TestDisjointComplementsIntersecting()
    {
        if (world is null) return;

        var area = world.Select("a[leisure]").First(); // a leisure area in monaco
        Assert.NotNull(area);

        var streets = world.Select("w[highway]");
        var all = Ids(streets);
        var disjoint = Ids(streets.Disjoint(area!));
        var intersecting = Ids(streets.Intersecting(area!));

        // every highway is either disjoint from the area or intersects it — never both, never neither
        Assert.Empty(disjoint.Intersect(intersecting));
        var union = new HashSet<long>(disjoint);
        union.UnionWith(intersecting);
        Assert.Equal(all, union);
        Assert.NotEmpty(disjoint); // monaco has highways away from a single leisure area
    }

}

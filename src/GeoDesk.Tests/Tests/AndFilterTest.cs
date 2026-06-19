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

// PORT: the Java original drew HTML maps of "bridges over the Danube in Bavaria" / airports (German
// data, no assertions). Rebased onto monaco as a structural test of filter chaining: applying two
// Intersecting filters must yield exactly the intersection of their individual result sets.
/// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest</c>.</remarks>
public class AndFilterTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.AndFilterTest.testAndFilter()</c>.</remarks>
    [Fact]
    public void TestAndFilterIsIntersectionOfFilters()
    {
        if (world is null) return;

        var a = world.Select("a[leisure]").First();  // an area
        var b = world.Select("a[building]").First();  // a different area
        Assert.NotNull(a);
        Assert.NotNull(b);

        var streets = world.Select("w[highway]");
        var both = Ids(streets.Intersecting(a!).Intersecting(b!));
        var inA = Ids(streets.Intersecting(a!));
        var inB = Ids(streets.Intersecting(b!));

        // chaining two Intersecting filters is the logical AND of them
        var expected = new HashSet<long>(inA);
        expected.IntersectWith(inB);
        Assert.True(expected.SetEquals(both),
            "chained Intersecting filters must equal the intersection of their result sets");
        Assert.Subset(inA, both); // the AND result is a subset of each operand
        Assert.Subset(inB, both);
    }

}

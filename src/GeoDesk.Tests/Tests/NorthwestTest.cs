/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original compared fast vs brute-force intersects over Germany (plus a one-off debug
// probe of a specific German relation, dropped). Rebased onto monaco: the tile-index Intersecting
// filter must return exactly the same set as the brute-force filter.
/// <remarks>Ported from Java <c>com.geodesk.tests.NorthwestTest</c>.</remarks>
public class NorthwestTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.NorthwestTest.testSameIntersectsResults()</c>.</remarks>
    [Fact]
    public void TestSameIntersectsResults()
    {
        if (world is null) return;

        var box = Box.OfWSEN(7.40, 43.72, 7.44, 43.75).ToGeometry(new GeometryFactory());

        var streets = world.Select("w[highway]");
        var slow = Ids(streets.Select(new SlowIntersectsFilter(box)));
        var fast = Ids(streets.Intersecting(box));

        Assert.True(slow.SetEquals(fast),
            "the tile-index Intersecting filter must return the same set as the brute-force filter");
        Assert.NotEmpty(fast);
    }

}

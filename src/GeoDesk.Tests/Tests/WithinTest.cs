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

// PORT: the Java original compared fast vs brute-force Within over Bavaria. Rebased onto monaco: the
// tile-index Within filter must return exactly the same set as the brute-force filter.
/// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest</c>.</remarks>
public class WithinTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest.testWithin()</c>.</remarks>
    [Fact]
    public void TestWithin()
    {
        if (world is null) return;

        var box = Box.OfWSEN(7.40, 43.72, 7.44, 43.75).ToGeometry(new GeometryFactory());

        var nodes = world.Select("n"); // points are non-trivially within a box (unlike long highways)
        var slow = Ids(nodes.Select(new SlowWithinFilter(box)));
        var fast = Ids(nodes.Within(box));

        Assert.True(slow.SetEquals(fast),
            "the tile-index Within filter must return the same set as the brute-force filter");
        Assert.NotEmpty(fast);
    }

}

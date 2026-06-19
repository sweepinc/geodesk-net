/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original drew HTML maps / benchmarked the index-accelerated filters over German
// data. The behavior worth testing is correctness: the fast (tile-index) filter must agree with the
// exact NTS predicate (no false positives), and Within must be a subset of Intersecting.
/// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest</c>.</remarks>
public class FastFilterTest : AbstractFeatureTest
{

    // A box over central Monaco, where there are plenty of highways and feature nodes.
    static Geometry CentralMonaco() => Box.OfWSEN(7.40, 43.72, 7.44, 43.75).ToGeometry(new GeometryFactory());

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testIntersectsQuery()</c>.</remarks>
    [Fact]
    public void IntersectingHasNoFalsePositives()
    {
        if (world is null) return;

        var prepared = PreparedGeometryFactory.Prepare(CentralMonaco());

        var count = 0;
        foreach (var f in world.Select("w[highway]").Intersecting(prepared))
        {
            Assert.True(prepared.Intersects(f.ToGeometry()),
                $"{f} was returned by Intersecting but does not actually intersect");
            count++;
        }
        Assert.True(count > 0, "expected highways intersecting central Monaco");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.FastFilterTest.testWithinQueryPerformance()</c>.</remarks>
    [Fact]
    public void WithinIsSubsetOfIntersecting()
    {
        if (world is null) return;

        var box = CentralMonaco();

        // points: a node is "within" an area exactly when it intersects it, so the within result
        // must be a (here, equal) subset of the intersecting result — never a superset.
        var within = Ids(world.Select("n").Within(box));
        var intersecting = Ids(world.Select("n").Intersecting(box));
        Assert.Subset(intersecting, within);
        Assert.NotEmpty(within); // monaco has feature nodes in its center
    }

}

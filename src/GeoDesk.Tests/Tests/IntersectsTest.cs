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

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original checked that "restaurants within a country = the union of restaurants
// within its states" over German data, plus a USA benchmark (dropped). Rebased onto monaco as the
// same decomposition invariant using two half-boxes that tile a whole box.
/// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest</c>.</remarks>
public class IntersectsTest : AbstractFeatureTest
{

    static HashSet<long> Ids(IFeatureQuery q)
    {
        var set = new HashSet<long>();
        foreach (var f in q) set.Add(FeatureId.Of(f.Type, f.Id));
        return set;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IntersectsTest.testIntersects()</c>.</remarks>
    [Fact]
    public void TestIntersects()
    {
        if (world is null) return;

        var factory = new GeometryFactory();
        var whole = Box.OfWSEN(7.40, 43.72, 7.44, 43.75); // central Monaco
        var left = Box.OfWSEN(7.40, 43.72, 7.42, 43.75);
        var right = Box.OfWSEN(7.42, 43.72, 7.44, 43.75); // left and right tile `whole`

        var streets = world.Select("w[highway]");
        var inWhole = Ids(streets.Intersecting(whole.ToGeometry(factory)));
        var inLeft = Ids(streets.Intersecting(left.ToGeometry(factory)));
        var inRight = Ids(streets.Intersecting(right.ToGeometry(factory)));

        var union = new HashSet<long>(inLeft);
        union.UnionWith(inRight);

        // a feature intersects the whole box iff it intersects one of the two halves
        Assert.True(inWhole.SetEquals(union),
            "Intersecting(whole) must equal Intersecting(left) ∪ Intersecting(right)");
        Assert.NotEmpty(inWhole);
    }

}

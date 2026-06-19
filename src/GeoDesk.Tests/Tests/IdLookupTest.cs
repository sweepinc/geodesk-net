/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Linq;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original hard-coded OSM ids from a German extract. Rebased onto monaco by taking an
// id *from* the fixture and asserting it round-trips, plus type-mismatch and absent-id behavior.
/// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest</c>.</remarks>
public class IdLookupTest : AbstractFeatureTest
{

    const long AbsentId = long.MaxValue;

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupNode()</c>.</remarks>
    [Fact]
    public void TestLookupNode()
    {
        if (world is null) return;

        var id = world.Select("n").First().Id; // an id known to exist in monaco

        var f = world.GetNode(id);
        Assert.NotNull(f);
        Assert.Equal(id, f!.Id);

        Assert.Null(world.Select("w").GetNode(id)); // that id is not a way
        Assert.Null(world.GetNode(AbsentId));        // absent id
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupWay()</c>.</remarks>
    [Fact]
    public void TestLookupWay()
    {
        if (world is null) return;

        var id = world.Select("w").First().Id;

        var f = world.GetWay(id);
        Assert.NotNull(f);
        Assert.Equal(id, f!.Id);

        Assert.Null(world.Select("n").GetWay(id)); // that id is not a node
        Assert.Null(world.Select("r").GetWay(id)); // nor a relation
        Assert.Null(world.GetWay(AbsentId));
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupRelation()</c>.</remarks>
    [Fact]
    public void TestLookupRelation()
    {
        if (world is null) return;

        var id = world.Select("r").First().Id;

        var f = world.GetRelation(id);
        Assert.NotNull(f);
        Assert.Equal(id, f!.Id);

        Assert.Null(world.Select("n").GetRelation(id)); // that id is not a node
        Assert.Null(world.GetRelation(AbsentId));
    }

}

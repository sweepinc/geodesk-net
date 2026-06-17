/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest</c>.</remarks>
[Collection("GolFixture")]
public class IdLookupTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupNode()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestLookupNode()
    {
        long presentId = 240109189;
        long absentId = 1;

        var f = world.GetNode(presentId);
        Assert.NotNull(f);
        Assert.Equal(presentId, f!.Id());

        f = world.Select("n[place]").GetNode(presentId);
        Assert.NotNull(f);

        f = world.Select("w").GetNode(presentId);
        Assert.Null(f);

        f = world.GetNode(absentId);
        Assert.Null(f);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupWay()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestLookupWay()
    {
        long presentId = 27426031;
        long absentId = 1;

        var f = world.GetWay(presentId);
        Assert.NotNull(f);
        Assert.Equal(presentId, f!.Id());

        f = world.Select("a[building]").GetWay(presentId);
        Assert.NotNull(f);

        f = world.Select("n").GetWay(presentId);
        Assert.Null(f);

        f = world.Select("w").GetWay(presentId);
        Assert.Null(f);

        f = world.Select("r").GetWay(presentId);
        Assert.Null(f);

        f = world.GetWay(absentId);
        Assert.Null(f);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.IdLookupTest.testLookupRelation()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestLookupRelation()
    {
        long presentId = 2599004;
        long absentId = 1;

        var f = world.GetRelation(presentId);
        Assert.NotNull(f);
        Assert.Equal(presentId, f!.Id());

        f = world.Select("r[route_master]").GetRelation(presentId);
        Assert.NotNull(f);

        f = world.Select("r[restriction]").GetRelation(presentId);
        Assert.Null(f);

        f = world.GetRelation(absentId);
        Assert.Null(f);
    }

}

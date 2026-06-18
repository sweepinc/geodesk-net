/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using GeoDesk.Util;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest</c>.</remarks>
[Collection("GolFixture")]
public class DisjointTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest.testDisjoint()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestDisjoint()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310))
            .First();
        var rivers = world.Select("r[waterway=river]");

        map.Add(bavaria!).Color("red");

        var riversOutsideBavaria = rivers.Disjoint(bavaria!);

        TestUtils.CheckNoDupes("rivers-outside-bavaria", TestUtils.GetSet(riversOutsideBavaria));

        foreach (var river in riversOutsideBavaria)
        {
            map.Add(river).Color("blue");
        }
        map.Save("c:\\geodesk\\disjoint-bavaria.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest.debugMissingRiver()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void DebugMissingRiver()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310))
            .First();
        var rivers = world.Select("r[waterway=river][name=Theel]");

        map.Add(bavaria!).Color("red");

        foreach (var river in rivers.Disjoint(bavaria!))
        {
            map.Add(river).Color("blue");
        }
        map.Save("c:\\geodesk\\river-theel.html");
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.DisjointTest.testDisjointFilterTiles()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestDisjointFilterTiles()
    {
        var map = new MapMaker();
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310))
            .First();

        map.Add(bavaria!).Color("red");

        var walker = new TileIndexWalker(((FeatureLibrary)world).Store());
        IFilter filter = new DisjointFilter(bavaria!);

        map.Add(filter.Bounds()).Color("orange");
        walker.Start(Box.OfWorld(), filter);
        while (walker.Next())
        {
            var marker = map.Add(Tile.Polygon(walker.CurrentTile()))
                .Tooltip(Tile.ToString(walker.CurrentTile()) + "<br>" + Tip.ToString(walker.Tip()));
            if (walker.CurrentFilter() != filter) marker.Color("green");
        }
        map.Save("c:\\geodesk\\tiles-disjoint-bavaria.html");
    }

}

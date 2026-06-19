/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original walked tiles over Bavaria's bbox 100× and drew an HTML map (no assertions).
// Rebased onto monaco as a structural test: every tile the walker visits for a query bbox must
// actually overlap that bbox.
/// <remarks>Ported from Java <c>com.geodesk.tests.TileWalkerTest</c>.</remarks>
public class TileWalkerTest : IDisposable
{

    readonly FeatureLibrary? world;

    public TileWalkerTest()
    {
        var gol = TestSettings.GolFile();
        if (File.Exists(gol)) world = new FeatureLibrary(gol);
    }

    public void Dispose() => world?.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.TileWalkerTest.testTileWalker()</c>.</remarks>
    [Fact]
    public void TestTileWalker()
    {
        if (world is null) return;

        // Use a real feature's bounds (already in the store's coordinate system) as the query box.
        IBounds target = world.Select("w[highway]").First().Bounds;
        var walker = new TileIndexWalker(world.Store);
        walker.Start(target);

        // The walker is a do-while cursor: CurrentTile is valid right after Start(), and Next()
        // advances. (Monaco fits in a single tile, so a plain while(Next()) loop would skip it.)
        var tileCount = 0;
        do
        {
            var box = Tile.Bounds(walker.CurrentTile());
            Assert.False(
                box.MaxX < target.MinX || box.MinX > target.MaxX ||
                box.MaxY < target.MinY || box.MinY > target.MaxY,
                "the tile walker visited a tile that does not overlap the query bounds");
            tileCount++;
        }
        while (walker.Next());

        Assert.True(tileCount > 0, "expected the tile walker to visit tiles over central Monaco");
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using GeoDesk.Util;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.TileWalkerTest</c>.</remarks>
public class TileWalkerTest : IDisposable
{

    readonly FeatureLibrary world;

    public TileWalkerTest()
    {
        world = new FeatureLibrary(TestSettings.GolFile());
    }

    public void Dispose() => world.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.TileWalkerTest.testTileWalker()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestTileWalker()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310))
            .First();
        var bavariaPoly = bavaria!.ToGeometry();
        IPreparedGeometry bavariaPrepared = new PreparedPolygon((IPolygonal)bavariaPoly);

        var map = new MapMaker();
        var tileCount = 0;
        var tilesAlwaysFiltered = 0;
        var tilesOutside = 0;
        var tilesInside = 0;
        var start = Stopwatch.GetTimestamp();
        const int runs = 100;
        for (var run = 0; run < runs; run++)
        {
            var walker = new TileIndexWalker(world.Store());
            IBounds targetBounds = bavaria.Bounds;
            walker.Start(targetBounds);
            while (walker.Next())
            {
                var inside = false;
                var outside = false;
                tileCount++;
                var box = Tile.Bounds(walker.CurrentTile());
                if (box.MinX <= targetBounds.MinX && box.MinY <= targetBounds.MinY &&
                    box.MaxX >= targetBounds.MaxX && box.MaxY >= targetBounds.MaxY)
                {
                    // If tile bbox contains the target bbox, no need to check tile, we always have
                    // to apply filter
                    tilesAlwaysFiltered++;
                }
                else
                {
                    Geometry tileGeom = world.GeometryFactory().CreatePolygon(new BoxCoordinateSequence(box));

                    outside = bavariaPrepared.Disjoint(tileGeom);
                    if (outside)
                        inside = false;
                    else
                        inside = bavariaPrepared.ContainsProperly(tileGeom);
                    if (run == 0 && !inside && !outside) map.Add(tileGeom);
                    if (run == 0 && inside) map.Add(tileGeom).Color("green");
                }
                if (outside) tilesOutside++;
                if (inside) tilesInside++;
            }
        }
        var end = Stopwatch.GetElapsedTime(start);
        map.Add(bavariaPoly).Color("red");
        map.Save("c:\\geodesk\\tile-walker-test-germany.html");

        Log.Debug("%d tiles in bbox", tileCount);
        Log.Debug("  %d tiles outside", tilesOutside);
        Log.Debug("  %d tiles inside", tilesInside);
        Log.Debug("  %d tiles must be queried", tileCount - tilesOutside);
        Log.Debug("  %d tiles must be filtered", tileCount - tilesInside - tilesOutside);
        Log.Debug("    Of these, %d are always filtered, no tile geometry check needed", tilesAlwaysFiltered);
        Log.Debug("Walking performed %d times in %d ms", runs, (long)end.TotalMilliseconds);
    }

}

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
using GeoDesk.Geom;
using GeoDesk.Util;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest_old</c>.</remarks>
public class WithinTest_old
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest_old.testWithinPoint()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestWithinPoint()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        var geomBuilder = new GeometryBuilder();

        var lon = 13.38686;
        var lat = 52.50806;

        var pt = geomBuilder.CreatePointFromLonLat(lon, lat);

        var areas = world.Select("a").In(Box.AtLonLat(lon, lat));

        for (var i = 0; i < 10; i++)
        {
            var startQuery = Stopwatch.GetTimestamp();

            var count = 0;
            foreach (var f in areas)
            {
                var candidateGeom = f.ToGeometry();
                try
                {
                    if (candidateGeom != null && candidateGeom.Contains(pt))
                    {
                        Log.Debug("- %s: %s", f, f.StringValue("name"));
                        count++;
                    }
                }
                catch (TopologyException ex)
                {
                    Log.Debug("Exception while testing %s: %s", f, ex);
                    Log.Debug("Valid geometry? %s", candidateGeom!.IsValid);
                }
            }

            Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count,
                (long)Stopwatch.GetElapsedTime(startQuery).TotalMilliseconds,
                (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest_old.testWithinPointPrepared()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestWithinPointPrepared()
    {
        var start = Stopwatch.GetTimestamp();
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        var geomBuilder = new GeometryBuilder();

        var lon = 13.38686;
        var lat = 52.50806;

        var pt = geomBuilder.CreatePointFromLonLat(lon, lat);

        var areas = world.Select("a").In(Box.AtLonLat(lon, lat));

        for (var i = 0; i < 10; i++)
        {
            var startQuery = Stopwatch.GetTimestamp();

            var count = 0;
            foreach (var f in areas)
            {
                var candidateGeom = new PreparedPolygon((IPolygonal)f.ToGeometry());
                try
                {
                    if (candidateGeom != null && candidateGeom.Contains(pt))
                    {
                        Log.Debug("- %s: %s", f, f.StringValue("name"));
                        count++;
                    }
                }
                catch (TopologyException ex)
                {
                    Log.Debug("Exception while testing %s: %s", f, ex);
                    Log.Debug("Valid geometry? %s", candidateGeom.Geometry.IsValid);
                }
            }

            Console.Write("Found {0} features in {1} ms (Total runtime {2} ms)\n", count,
                (long)Stopwatch.GetElapsedTime(startQuery).TotalMilliseconds,
                (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// Exercises the spatial-predicate filters (intersecting / within / containingXY / maxMetersFrom)
/// against the monaco.gol fixture. Soft-skips if no fixture is available.
/// </summary>
[Collection("GolFixture")]
public class SpatialFilterTest
{

    readonly ITestOutputHelper _output;

    public SpatialFilterTest(ITestOutputHelper output)
    {
        _output = output;
    }

    static string GolFile() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void FiltersHighwaysSpatially()
    {
        var gol = GolFile();
        if (!File.Exists(gol))
        {
            _output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        var world = Features.Open(gol);
        try
        {
            // A box covering central Monaco, as a JTS polygon (Mercator coords).
            var box = Box.OfWSEN(7.41, 43.72, 7.43, 43.74);
            var boxGeom = box.ToGeometry(new GeometryFactory());

            var allHighways = world.Ways("w[highway]").Count();
            var intersecting = world.Ways("w[highway]").Intersecting(boxGeom).Count();
            var within = world.Ways("w[highway]").Within(boxGeom).Count();

            _output.WriteLine($"highways: all={allHighways}, intersecting box={intersecting}, within box={within}");
            Assert.True(intersecting > 0, "expected highways intersecting the box");
            Assert.True(within <= intersecting, "within must be a subset of intersecting");
            Assert.True(intersecting <= allHighways);

            // Features containing the box centre point.
            var cx = (box.MinX + box.MaxX) / 2;
            var cy = (box.MinY + box.MaxY) / 2;
            var containingCentre = world.ContainingXY(cx, cy).Count();
            _output.WriteLine($"features containing centre: {containingCentre}");

            // maxMetersFrom a point: nearby highways within 200 m of the centre.
            var near = world.Ways("w[highway]").MaxMetersFromXY(200, cx, cy).Count();
            _output.WriteLine($"highways within 200m of centre: {near}");
            Assert.True(near > 0, "expected highways within 200m of the centre");
        }
        finally
        {
            ((IDisposable)world).Dispose();
        }
    }

}

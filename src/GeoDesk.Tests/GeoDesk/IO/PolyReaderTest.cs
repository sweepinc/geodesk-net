/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using GeoDesk.Util;
using NetTopologySuite.Geometries;
using Xunit;

namespace GeoDesk.IO;

/// <summary>
/// Ported from Java <c>com.geodesk.io.PolyReaderTest</c>. Reads the <c>bremen.poly</c> and
/// <c>holes.poly</c> resources and validates polygon/ring counts.
/// </summary>
public class PolyReaderTest
{

    static string PolyFile(string name) =>
        Path.Combine(AppContext.BaseDirectory, "TestResources", "io", "poly", name);

    static Geometry ReadPoly(string name)
    {
        var factory = new GeometryFactory();
        var transformer = new CoordinateTransformer(6);
        using var input = new StreamReader(PolyFile(name));
        var reader = new PolyReader(input, factory, transformer);
        return reader.Read();
    }

    [Fact]
    public void TestRead()
    {
        var geom = ReadPoly("bremen.poly");

        // We should have read 2 polygons
        Assert.IsType<MultiPolygon>(geom);
        Assert.Equal(2, geom.NumGeometries);

        // #1 should have 85 points (including end)
        var p1 = (Polygon)geom.GetGeometryN(0);
        Assert.Equal(85, p1.ExteriorRing.CoordinateSequence.Count);

        // #2 should have 254 points (including end)
        var p2 = (Polygon)geom.GetGeometryN(1);
        Assert.Equal(254, p2.ExteriorRing.CoordinateSequence.Count);

        geom = ReadPoly("holes.poly");

        Assert.IsType<MultiPolygon>(geom);
        Assert.Equal(2, geom.NumGeometries);

        p1 = (Polygon)geom.GetGeometryN(0);
        Assert.Equal(5, p1.ExteriorRing.CoordinateSequence.Count);
        Assert.Equal(2, p1.NumInteriorRings);
        Assert.Equal(4, p1.GetInteriorRingN(0).CoordinateSequence.Count);
        Assert.Equal(4, p1.GetInteriorRingN(1).CoordinateSequence.Count);
        p2 = (Polygon)geom.GetGeometryN(1);
        Assert.Equal(5, p2.ExteriorRing.CoordinateSequence.Count);
        Assert.Equal(1, p2.NumInteriorRings);
        Assert.Equal(4, p2.GetInteriorRingN(0).CoordinateSequence.Count);
    }

}

/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using GeoDesk.Util;
using NetTopologySuite.Geometries;
using Xunit;

namespace GeoDesk.IO;

/// <summary>
/// Round-trips a polygon through the .poly reader/writer using an identity coordinate transformer.
/// </summary>
public class PolyReaderWriterTest
{

    const string SquarePoly =
        "testarea\n" +
        "area\n" +
        "   1.0   1.0\n" +
        "   2.0   1.0\n" +
        "   2.0   2.0\n" +
        "   1.0   2.0\n" +
        "END\n" +
        "END\n";

    static Geometry ReadPoly(string text)
    {
        var reader = new PolyReader(new StringReader(text), new GeometryFactory(),
            new CoordinateTransformer(7));
        return reader.Read();
    }

    [Fact]
    public void ReadsPolygon()
    {
        var g = ReadPoly(SquarePoly);
        var polygon = Assert.IsType<Polygon>(g);
        // closed unit square -> 5 coordinates, area 1.0
        Assert.Equal(5, polygon.ExteriorRing.NumPoints);
        Assert.Equal(1.0, polygon.Area, 9);
    }

    [Fact]
    public void RoundTripsThroughWriter()
    {
        var original = ReadPoly(SquarePoly);

        var sw = new StringWriter();
        var writer = new PolyWriter(sw, new CoordinateTransformer(7));
        writer.Write("testarea", original);
        var text = sw.ToString();

        Assert.Contains("area", text);
        Assert.Contains("END", text);

        var reparsed = ReadPoly(text);
        Assert.Equal(original.Area, reparsed.Area, 9);
        Assert.True(original.EqualsTopologically(reparsed));
    }

}

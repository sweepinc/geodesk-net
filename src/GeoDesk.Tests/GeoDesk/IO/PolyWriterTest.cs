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
/// Ported from Java <c>com.geodesk.io.PolyWriterTest</c>. Reads a poly file, writes it and reads it
/// again, to make sure we get back the same geometry.
/// </summary>
public class PolyWriterTest
{

    static string PolyFile(string name) =>
        Path.Combine(AppContext.BaseDirectory, "TestResources", "io", "poly", name);

    [Fact]
    public void TestWrite()
    {
        var factory = new GeometryFactory();
        var transformer = new CoordinateTransformer(6);
        Geometry g1;
        using (var input = new StreamReader(PolyFile("bremen.poly")))
        {
            g1 = new PolyReader(input, factory, transformer).Read();
        }

        var sw = new StringWriter();
        new PolyWriter(sw, transformer).Write("poly-test.poly", g1);
        var result = sw.ToString();

        var g2 = new PolyReader(new StringReader(result), factory, transformer).Read();
        Assert.True(g1.EqualsExact(g2));
    }

}

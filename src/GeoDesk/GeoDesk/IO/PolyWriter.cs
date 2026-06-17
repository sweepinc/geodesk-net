/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics;
using System.Globalization;
using System.IO;
using GeoDesk.Util;
using NetTopologySuite.Geometries;

namespace GeoDesk.IO;

// PORT: Java's Appendable sink is represented as a .NET TextWriter.
/// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter</c>.</remarks>
public class PolyWriter
{

    readonly TextWriter _out;
    readonly CoordinateTransformer _transformer;
    int _shellCount;
    int _holeCount;

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter(Appendable, CoordinateTransformer)</c>.</remarks>
    public PolyWriter(TextWriter outp, CoordinateTransformer transformer)
    {
        _out = outp;
        _transformer = transformer;
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter.write(String, Geometry)</c>.</remarks>
    public void Write(string name, Geometry geom)
    {
        _out.Write(name);
        _out.Write("\n");
        Write(geom);
        _out.Write("END");
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter.write(Geometry)</c>.</remarks>
    public void Write(Geometry geom)
    {
        for (var i = 0; i < geom.NumGeometries; i++)
        {
            if (geom.GetGeometryN(i) is Polygon polygon) Write(polygon);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter.write(Polygon)</c>.</remarks>
    public void Write(Polygon polygon)
    {
        WriteRing("area", ++_shellCount, (LinearRing)polygon.ExteriorRing);
        for (var i = 0; i < polygon.NumInteriorRings; i++)
        {
            WriteRing("!hole", ++_holeCount, (LinearRing)polygon.GetInteriorRingN(i));
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyWriter.writeRing(String, int, LinearRing)</c>.</remarks>
    void WriteRing(string name, int count, LinearRing ring)
    {
        _out.Write(name);
        Debug.Assert(count != 0);  // we use 1-based counting for nicer display
        if (count > 1) _out.Write(count.ToString(CultureInfo.InvariantCulture));
        var coords = ring.CoordinateSequence;
        for (var i = 0; i < coords.Count; i++)
        {
            _out.Write("\n\t");
            _transformer.WriteX(_out, coords.GetX(i));
            _out.Write("\t");
            _transformer.WriteY(_out, coords.GetY(i));
        }
        _out.Write("\nEND\n");
    }

}

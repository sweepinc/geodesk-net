/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using GeoDesk.Util;
using NetTopologySuite.Geometries;

namespace GeoDesk.IO;

// PORT: Java's BufferedReader source is represented as a .NET TextReader.
/// <remarks>Ported from Java <c>com.geodesk.io.PolyReader</c>.</remarks>
public class PolyReader
{

    readonly TextReader _in;
    readonly GeometryFactory _factory;
    readonly CoordinateTransformer _transformer;

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyReader(BufferedReader, GeometryFactory, CoordinateTransformer)</c>.</remarks>
    public PolyReader(TextReader input, GeometryFactory factory, CoordinateTransformer transformer)
    {
        _in = input;
        _factory = factory;
        _transformer = transformer;
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyReader.error(String, int)</c>.</remarks>
    [DoesNotReturn]
    static void Error(string msg, int line)
    {
        throw new ParseException(string.Format(CultureInfo.InvariantCulture, "Line {0}: {1}", line, msg));
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyReader.makePolygon(LinearRing, List)</c>.</remarks>
    Polygon MakePolygon(LinearRing shell, List<LinearRing> holes)
    {
        if (holes.Count == 0) return _factory.CreatePolygon(shell);
        return _factory.CreatePolygon(shell, holes.ToArray());
    }

    /// <remarks>Ported from Java <c>com.geodesk.io.PolyReader.read()</c>.</remarks>
    public Geometry Read()
    {
        var polygons = new List<Polygon>();
        var holes = new List<LinearRing>();
        var coords = new List<double>();

        var line = 1;
        var name = _in.ReadLine();
        if (name == null) Error("Expected name", line);
        LinearRing? shell = null;
        for (; ; )
        {
            line++;
            var ringName = _in.ReadLine();
            if (ringName == null) Error("Expected ring name", line);
            ringName = ringName.Trim();
            if (ringName == "END")
            {
                if (shell == null) Error("Must define at least one polygon", line);
                polygons.Add(MakePolygon(shell, holes));
                break;
            }
            if (ringName.StartsWith("!")) // defines a hole
            {
                if (shell == null) Error("Must define shell before holes", line);
            }
            else if (shell != null)
            {
                polygons.Add(MakePolygon(shell, holes));
                shell = null;
                holes.Clear();
            }

            for (; ; )
            {
                line++;
                var coordPair = _in.ReadLine();
                if (coordPair == null)
                {
                    if (coords.Count != 0) Error("Unexpected end of file", line);
                    break;
                }
                coordPair = coordPair.Trim();
                if (coordPair == "END") break;
                if (coordPair.Length == 0) continue;
                var n = coordPair.IndexOf(' ');
                if (n < 0) n = coordPair.IndexOf('\t');
                if (n > 0)
                {
                    try
                    {
                        var x = double.Parse(coordPair.Substring(0, n).Trim(), CultureInfo.InvariantCulture);
                        var y = double.Parse(coordPair.Substring(n + 1).Trim(), CultureInfo.InvariantCulture);
                        coords.Add(_transformer.TransformX(x));
                        coords.Add(_transformer.TransformY(y));
                        continue;
                    }
                    catch (FormatException)
                    {
                        break; // fall through
                    }
                }
                Error("Expected <lon> <lat>", line);
            }
            var len = coords.Count;
            if (len == 0) break;
            var coordinateCount = len / 2;
            if (coordinateCount < 3) Error("Must specify at least 3 coordinate pairs", line);
            var firstX = coords[0];
            var firstY = coords[1];
            var closed = firstX == coords[len - 2] && firstY == coords[len - 1];
            var seq = _factory.CoordinateSequenceFactory.Create(coordinateCount + (closed ? 0 : 1), 2);
            for (var i = 0; i < coordinateCount; i++)
            {
                seq.SetOrdinate(i, 0, coords[i * 2]);
                seq.SetOrdinate(i, 1, coords[i * 2 + 1]);
            }
            if (!closed)
            {
                seq.SetOrdinate(coordinateCount, 0, firstX);
                seq.SetOrdinate(coordinateCount, 1, firstY);
            }
            var ring = _factory.CreateLinearRing(seq);
            if (shell == null)
                shell = ring;
            else
                holes.Add(ring);
            coords.Clear();
        }
        if (polygons.Count == 1) return polygons[0];
        return _factory.CreateMultiPolygon(polygons.ToArray());
    }

}

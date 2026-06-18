/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Util;

// PORT: Java's Appendable sink is represented as a .NET TextWriter.
/// <summary>
/// A class for generating a Leaflet-based interactive map.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.util.MapMaker</c>.</remarks>
public class MapMaker
{

    string _id = "map";
    readonly List<Marker> _markers = new List<Marker>();
    int _minZoom = 0;
    int _maxZoom = 19;
    string _tileServerUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    string _attribution =
        "Map data © <a href=\"http://openstreetmap.org\">OpenStreetMap</a> contributors";
    string _leafletStyleSheetUrl = "https://unpkg.com/leaflet@1.8.0/dist/leaflet.css";
    string _leafletScriptUrl = "https://unpkg.com/leaflet@1.8.0/dist/leaflet.js";

    /// <summary>Sets the URL template for the source of map tiles.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.tiles(String)</c>.</remarks>
    public void Tiles(string url)
    {
        _tileServerUrl = url;
    }

    /// <summary>Sets the attribution displayed on the map.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.attribution(String)</c>.</remarks>
    public void Attribution(string attribution)
    {
        _attribution = attribution;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.writeXY(Appendable, double, double)</c>.</remarks>
    internal void WriteXY(TextWriter outp, double x, double y)
    {
        outp.Write('[');
        outp.Write(Mercator.LatFromY(y).ToString(CultureInfo.InvariantCulture));
        outp.Write(',');
        outp.Write(Mercator.LonFromX(x).ToString(CultureInfo.InvariantCulture));
        outp.Write(']');
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.add(Marker)</c>.</remarks>
    Marker Add(Marker marker)
    {
        marker.SetMap(this);
        _markers.Add(marker);
        return marker;
    }

    /// <summary>Adds a Marker for the given JTS Geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.add(Geometry)</c>.</remarks>
    public Marker Add(Geometry geom)
    {
        return Add(geom.IsEmpty ? new EmptyMarker() : new GeometryMarker(geom));
    }

    /// <summary>Adds a Marker for the given bounding box.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.add(Bounds)</c>.</remarks>
    public Marker Add(Bounds box)
    {
        return Add(new BoxMarker(box));
    }

    /// <summary>Adds a Marker for the given Feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.add(Feature)</c>.</remarks>
    public Marker Add(GeoDesk.Feature.Feature feature)
    {
        var geom = feature.ToGeometry();
        return Add(new GeometryMarker(geom));
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.add(Iterable)</c>.</remarks>
    public void Add(IEnumerable<GeoDesk.Feature.Feature> features)
    {
        foreach (var f in features) Add(f);
    }

    /// <summary>
    /// Generates a self-contained HTML file that displays the interactive map and all its markers.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.save(String)</c>.</remarks>
    public void Save(string path)
    {
        using var outp = new StreamWriter(path);
        Write(outp);
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.write(Appendable)</c>.</remarks>
    public void Write(TextWriter outp)
    {
        outp.Write("<html><head><link rel=\"stylesheet\" href=\"");
        outp.Write(_leafletStyleSheetUrl);
        outp.Write("\">\n<script src=\"");
        outp.Write(_leafletScriptUrl);
        outp.Write("\"></script>\n<style>\n#map {height: 100%;}\nbody {margin:0;}\n</style>\n");
        outp.Write("</head>\n<body>\n<div id=\"map\"> </div>\n");
        outp.Write("<script>");
        WriteScript(outp);
        outp.Write("</script></body></html>");
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.writeScript(Appendable)</c>.</remarks>
    void WriteScript(TextWriter outp)
    {
        outp.Write("var map = L.map('map');\n");
        outp.Write("var tilesUrl='");
        outp.Write(_tileServerUrl);
        outp.Write("';\nvar tilesAttrib='");
        outp.Write(_attribution);
        outp.Write("';\nvar tileLayer = new L.TileLayer(tilesUrl, {minZoom: ");
        outp.Write(_minZoom.ToString(CultureInfo.InvariantCulture));
        outp.Write(", maxZoom: ");
        outp.Write(_maxZoom.ToString(CultureInfo.InvariantCulture));
        outp.Write(", attribution: tilesAttrib});\n" +
            "map.setView([51.505, -0.09], 13);\n" +     // TODO
            "map.addLayer(tileLayer);\n" +
            "L.control.scale().addTo(map);\n");

        var bounds = new Box();
        foreach (var marker in _markers)
        {
            if (marker.IsVisible())
            {
                marker.Write(outp);
                bounds.ExpandToInclude(marker.Bounds());
            }
        }
        outp.Write("map.fitBounds([");
        WriteXY(outp, bounds.MinX, bounds.MinY);
        outp.Write(',');
        WriteXY(outp, bounds.MaxX, bounds.MaxY);
        outp.Write("]);");
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker</c>.</remarks>
    class GeometryMarker : Marker
    {

        readonly Geometry _geom;

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker(Geometry)</c>.</remarks>
        internal GeometryMarker(Geometry geom)
        {
            _geom = geom;
        }

        // TODO: winding order?
        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker.writePolygonCoordinates(Appendable, Polygon)</c>.</remarks>
        void WritePolygonCoordinates(TextWriter outp, Polygon p)
        {
            outp.Write('[');
            WriteCoordinates(outp, p.ExteriorRing.CoordinateSequence);
            for (var i = 0; i < p.NumInteriorRings; i++)
            {
                outp.Write(',');
                WriteCoordinates(outp, p.GetInteriorRingN(i).CoordinateSequence);
            }
            outp.Write(']');
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker.bounds()</c>.</remarks>
        public override Bounds Bounds()
        {
            return Box.FromEnvelope(_geom.EnvelopeInternal);
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker.writeStub(Appendable)</c>.</remarks>
        protected override void WriteStub(TextWriter outp)
        {
            WriteStub(outp, _geom);
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.GeometryMarker.writeStub(Appendable, Geometry)</c>.</remarks>
        void WriteStub(TextWriter outp, Geometry g)
        {
            if (g is IPolygonal)
            {
                outp.Write("L.polygon(");
                var geometryCount = g.NumGeometries;
                if (geometryCount == 1) // single polygon
                {
                    WritePolygonCoordinates(outp, (Polygon)g);
                }
                else // multipolygon
                {
                    outp.Write('[');
                    for (var i = 0; i < geometryCount; i++)
                    {
                        if (i > 0) outp.Write(',');
                        WritePolygonCoordinates(outp, (Polygon)g.GetGeometryN(i));
                    }
                    outp.Write(']');
                }
            }
            else if (g is ILineal)
            {
                outp.Write("L.polyline(");
                var geometryCount = g.NumGeometries;
                if (geometryCount == 1) // single polyline
                {
                    WriteCoordinates(outp, ((LineString)g.GetGeometryN(0)).CoordinateSequence);
                }
                else // multipolyline
                {
                    outp.Write('[');
                    for (var i = 0; i < geometryCount; i++)
                    {
                        if (i > 0) outp.Write(',');
                        WriteCoordinates(outp, ((LineString)g.GetGeometryN(i)).CoordinateSequence);
                    }
                    outp.Write(']');
                }
            }
            else if (g is Point pt)
            {
                WritePoint(outp, pt.X, pt.Y);
            }
            else    // GeometryCollection
            {
                outp.Write("L.featureGroup([");
                for (var i = 0; i < g.NumGeometries; i++)
                {
                    if (i > 0) outp.Write(',');
                    WriteStub(outp, g.GetGeometryN(i));
                    if (options.Count > 0)
                    {
                        outp.Write(',');
                        JavaScript.WriteMap(outp, options);
                    }
                    outp.Write(')');
                }
                outp.Write(']');
            }
        }

    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.BoxMarker</c>.</remarks>
    class BoxMarker : Marker
    {

        readonly Bounds _box;

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.BoxMarker(Bounds)</c>.</remarks>
        internal BoxMarker(Bounds box)
        {
            _box = box;
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.BoxMarker.bounds()</c>.</remarks>
        public override Bounds Bounds()
        {
            return _box;
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.BoxMarker.writeStub(Appendable)</c>.</remarks>
        protected override void WriteStub(TextWriter outp)
        {
            outp.Write("L.rectangle([");
            map!.WriteXY(outp, _box.MinX, _box.MinY);
            outp.Write(',');
            map!.WriteXY(outp, _box.MaxX, _box.MaxY);
            outp.Write(']');
        }

    }

    /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.EmptyMarker</c>.</remarks>
    class EmptyMarker : Marker
    {

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.EmptyMarker.bounds()</c>.</remarks>
        public override Bounds Bounds()
        {
            return new Box();       // TODO: cache
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.EmptyMarker.isVisible()</c>.</remarks>
        public override bool IsVisible()
        {
            return false;
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.EmptyMarker.writeStub(Appendable)</c>.</remarks>
        protected override void WriteStub(TextWriter outp)
        {
            // do nothing
        }

        /// <remarks>Ported from Java <c>com.geodesk.util.MapMaker.EmptyMarker.write(Appendable)</c>.</remarks>
        public override void Write(TextWriter outp)
        {
            // do nothing
        }

    }

}

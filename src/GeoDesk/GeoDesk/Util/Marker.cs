/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.IO;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Util;

// PORT: Java's Appendable sink is represented as a .NET TextWriter.
/// <summary>
/// A marker on a Leaflet-based interactive map.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.util.Marker</c>.</remarks>
public abstract class Marker
{

    protected Dictionary<string, object> options = new Dictionary<string, object>();
    string? _tooltip;
    string? _url;
    protected MapMaker? map;

    /// <summary>
    /// Sets the content to be displayed whenever the cursor is hovered over this Marker.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.tooltip(String)</c>.</remarks>
    public Marker Tooltip(string tooltip)
    {
        _tooltip = tooltip;
        return this;
    }

    /// <summary>
    /// Sets the URL which is navigated when the user clicks on this Marker.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.url(String)</c>.</remarks>
    public Marker Url(string url)
    {
        _url = url;
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.setMap(MapMaker)</c>.</remarks>
    internal void SetMap(MapMaker map)
    {
        this.map = map;
    }

    /// <summary>
    /// Specifies options for this Marker (see the Leaflet path documentation).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.options(Map)</c>.</remarks>
    public Marker Options(Dictionary<string, object> moreOptions)
    {
        foreach (var kv in moreOptions) options[kv.Key] = kv.Value;
        return this;
    }

    /// <summary>
    /// Specifies a single option for this Marker (see the Leaflet path documentation).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.option(String, Object)</c>.</remarks>
    public Marker Option(string key, object value)
    {
        options[key] = value;
        return this;
    }

    /// <summary>
    /// Specifies the color of this Marker.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.color(String)</c>.</remarks>
    public Marker Color(string color)
    {
        return Option("color", color);
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.defaultTooltip()</c>.</remarks>
    public virtual string? DefaultTooltip()
    {
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.isVisible()</c>.</remarks>
    public virtual bool IsVisible()
    {
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.writePoint(Appendable, double, double)</c>.</remarks>
    protected void WritePoint(TextWriter outp, double x, double y)
    {
        outp.Write("L.circle(");
        map!.WriteXY(outp, x, y);
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.writeCoordinates(Appendable, CoordinateSequence)</c>.</remarks>
    protected void WriteCoordinates(TextWriter outp, CoordinateSequence coords)
    {
        outp.Write("[");
        var len = coords.Count;
        for (var i = 0; i < len; i++)
        {
            if (i > 0) outp.Write(',');
            map!.WriteXY(outp, coords.GetX(i), coords.GetY(i));
        }
        outp.Write("]");
    }

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.bounds()</c>.</remarks>
    public abstract Bounds Bounds();

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.writeStub(Appendable)</c>.</remarks>
    protected abstract void WriteStub(TextWriter outp);

    /// <remarks>Ported from Java <c>com.geodesk.util.Marker.write(Appendable)</c>.</remarks>
    public virtual void Write(TextWriter outp)
    {
        WriteStub(outp);
        if (options.Count > 0)
        {
            outp.Write(',');
            JavaScript.WriteMap(outp, options);
        }
        outp.Write(')');
        if (_tooltip != null && _tooltip.Length > 0)
        {
            outp.Write(".bindTooltip(");
            JavaScript.WriteString(outp, _tooltip);
            outp.Write(")");
        }
        if (_url != null && _url.Length > 0)
        {
            outp.Write(".on('click', function(){window.location=");
            JavaScript.WriteString(outp, _url);
            outp.Write(";})");
        }
        outp.Write(".addTo(map);\n");
    }

}
